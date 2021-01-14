using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoguessrApi
    {
        public const string ChallengeUrlPrefix = "https://www.geoguessr.com/challenge";
        const int MaxApiCallsPerHour = 100;
        const string AuthenticationFilePath = "local-authentication.json";
        static int ErrorMessageCount;
        static readonly Dictionary<string, List<GeoTournament.PlayerGame>> CachedGames = new();
        static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
        static readonly SizeAndTimeLimitedQueue ChallengeResultsCallsPerHour = new(Lifetime, MaxApiCallsPerHour);
        static readonly SizeAndTimeLimitedQueue ChallengeLinkCallsPerHour = new(Lifetime, MaxApiCallsPerHour);
        static readonly GeoTournament.PlayerGame[] Empty = Array.Empty<GeoTournament.PlayerGame>();

        public static async Task<(string? error, IReadOnlyCollection<GeoTournament.PlayerGame> playerGames)> LoadGame(string gameId, Page page, IConfiguration config)
        {
            try
            {
                var error = await VerifySignedIn(page, config);
                if (error != null)
                {
                    return (error, Empty);
                }

                if (CachedGames.TryGetValue(gameId, out var cachedGames))
                {
                    return (null, cachedGames);
                }

                var limitPerHour = ChallengeResultsCallsPerHour;
                if (limitPerHour.IsFull())
                {
                    return (RateLimitResponse(), Empty);
                }

                var playerGames = new List<GeoTournament.PlayerGame>();
                List<GeoTournament.PlayerGame> games;
                var maxItems = 50;
                do
                {
                    limitPerHour.TryEnqueue(DateTime.UtcNow);
                    games = await GetWithFetch<List<GeoTournament.PlayerGame>>(
                                    page,
                                    $"results/scores/{gameId}/{playerGames.Count}/{maxItems}") ??
                                new List<GeoTournament.PlayerGame>();
                    if (!games.Any())
                    {
                        return ("It looks like you haven't finished the game?", Empty);
                    }

                    playerGames.AddRange(games);
                } while (games.Count == maxItems && !limitPerHour.IsFull());

                CachedGames.Add(gameId, playerGames);
                return (null, playerGames);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ("Unexpected error.", Empty);
            }
        }

        public static async Task<(string? error, string? link)> GenerateChallengeLink(Page page, IConfiguration config, ushort timeLimit, GameMode gameMode, string mapId)
        {
            var error = await VerifySignedIn(page, config);
            if (error != null)
            {
                return (error, null);
            }

            var limitPerHour = ChallengeLinkCallsPerHour;
            if (limitPerHour.IsFull())
            {
                return (RateLimitResponse(), null);
            }

            try
            {
                var forbidMoving = gameMode switch
                {
                    GameMode.NoMove or GameMode.NMPZ => true,
                    _ => false
                };
                var forbidZooming = gameMode switch
                {
                    GameMode.NMPZ => true,
                    _ => false
                };
                var forbidRotating = gameMode switch
                {
                    GameMode.NMPZ => true,
                    _ => false
                };
                var requestBody = JsonSerializer.Serialize(new
                {
                    map = mapId,
                    isCountryStreak = false,
                    roundTime = 0,
                    forbidMoving = forbidMoving,
                    forbidZooming = forbidZooming,
                    forbidRotating = forbidRotating,
                    timeLimit = timeLimit
                });
                limitPerHour.TryEnqueue(DateTime.UtcNow);
                var result = await PostWithFetch<ChallengeApiResult>(page, requestBody, "challenges");
                if (result == null)
                {
                    return ("Unknown response type", null);
                }

                return (null, $"{ChallengeUrlPrefix}/{result.token}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ("Unexpected error.", null);
            }
        }

        public static string ApiCallsInfo()
        {
            static string CallsPerHour(SizeAndTimeLimitedQueue queue, string endpoint)
            {
                queue.RemoveStaleEntries();
                return $"{queue.Count} '{endpoint}' calls in the preceding {(DateTime.UtcNow - queue.Oldest()):mm\\:ss}";
            }

            return $"{CallsPerHour(ChallengeResultsCallsPerHour, "api/v3/results/scores")}. {CallsPerHour(ChallengeLinkCallsPerHour, "api/v3/challenges")}";
        }

        public static async Task<bool> TrySignInFromLocalFile(Page page)
        {
            try
            {
                if (File.Exists(AuthenticationFilePath))
                {
                    var cookieDataFromFile = JsonSerializer.Deserialize<CookieParam>(await File.ReadAllTextAsync(AuthenticationFilePath));
                    if (cookieDataFromFile != null && Extensions.UnixTimeStampToDateTime(cookieDataFromFile.Expires ?? 0) > DateTime.Now.AddDays(1))
                    {
                        await page.SetCookieAsync(cookieDataFromFile);
                        return true;
                    }
                    else
                    {
                        File.Delete(AuthenticationFilePath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed trying to auto sign in from {AuthenticationFilePath} data.");
                Console.WriteLine(e);
            }

            return false;
        }

        static async Task<T?> PostWithFetch<T>(Page page, object requestBody, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'POST', headers: {{'Content-Type': 'application/json'}}, body: JSON.stringify({requestBody})}}).then(response => response.json()).then(x => JSON.stringify(x))";
            return await CallGeoguessrApi<T>(page, script);
        }

        static async Task<T?> GetWithFetch<T>(Page page, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'GET', headers: {{'Content-Type': 'application/json'}}}}).then(response => response.json()).then(x => JSON.stringify(x))";
            return await CallGeoguessrApi<T>(page, script);
        }

        static async Task<T?> CallGeoguessrApi<T>(Page page, string script)
        {
            var result = await page.EvaluateExpressionAsync<string>(script);
            if (WasNotAllowed(result))
            {
                return default;
            }

            await SaveTokenInLocalAuthenticationFile(page);
            return JsonSerializer.Deserialize<T>(result);
        }

        static async Task SaveTokenInLocalAuthenticationFile(Page page)
        {
            try
            {
                if (File.Exists(AuthenticationFilePath))
                {
                    return;
                }

                var authCookie = await GetSignInCookie(page);
                if (authCookie != null)
                {
                    await File.WriteAllTextAsync(AuthenticationFilePath, JsonSerializer.Serialize(authCookie));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        static async Task<string?> VerifySignedIn(Page page, IConfiguration config)
        {
            var signInCookie = await GetSignInCookie(page);
            if (signInCookie == null)
            {
                if (!await TrySignInFromLocalFile(page))
                {
                    return ++ErrorMessageCount > 3 ? null : $"@{config[TwitchClient.TwitchChannelConfigKey]} You have not signed in to https://www.geoguessr.com correctly.";
                }
            }

            return null;
        }

        static async Task<CookieParam?> GetSignInCookie(Page page)
        {
            try
            {
                var cookies = await page.GetCookiesAsync("https://www.geoguessr.com");
                return cookies.FirstOrDefault(x => x.Name == "_ncfa");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        static bool WasNotAllowed(string json)
        {
            try
            {
                var errorModel = JsonSerializer.Deserialize<ErrorModel>(json);
                return !string.IsNullOrEmpty(errorModel?.message);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static string RateLimitResponse()
        {
            var timeUntilOldestIsOlderThanLifetime = (Lifetime - (DateTime.UtcNow - ChallengeResultsCallsPerHour.Oldest())).ToString(@"mm\:ss");
            return $"Rate limited for {timeUntilOldestIsOlderThanLifetime}";
        }

        record ErrorModel
        {
            public string? message { get; set; }
        }

        record ChallengeApiResult
        {
            public string? token { get; set; }
        }

        public enum GameMode
        {
            Invalid,
            Move,
            NoMove,
            NMPZ
        }
    }
}