using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoguessrApi
    {
        const int MaxApiCallsPerHour = 100;
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

                if (!ChallengeResultsCallsPerHour.TryEnqueue(DateTime.UtcNow))
                {
                    return (RateLimitResponse(), Empty);
                }

                var playerGames = new List<GeoTournament.PlayerGame>();
                List<GeoTournament.PlayerGame> games;
                var maxItems = 50;
                do
                {
                    games = await GetWithFetch<List<GeoTournament.PlayerGame>>(
                                    page,
                                    $"results/scores/{gameId}/{playerGames.Count}/{maxItems}") ??
                                new List<GeoTournament.PlayerGame>();
                    if (!games.Any())
                    {
                        return ("It looks like you haven't finished the game?", Empty);
                    }

                    playerGames.AddRange(games);
                } while (games.Count == maxItems);

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

            if (!ChallengeLinkCallsPerHour.TryEnqueue(DateTime.UtcNow))
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
                var result = await PostWithFetch<ChallengeApiResult>(page, requestBody, "challenges");
                if (result == null)
                {
                    return ("Unknown response type", null);
                }

                return (null, $"https://www.geoguessr.com/challenge/{result.token}");
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
                return $"{queue.Count} '{endpoint}' calls in the preceding {(DateTime.UtcNow - queue.Oldest()):mm\\:ss}";
            }

            return $"{CallsPerHour(ChallengeResultsCallsPerHour, "api/v3/results/scores")}. {CallsPerHour(ChallengeLinkCallsPerHour, "api/v3/challenges")}";
        }

        static async Task<T?> PostWithFetch<T>(Page page, object requestBody, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'POST', headers: {{'Content-Type': 'application/json'}}, body: JSON.stringify({requestBody})}}).then(response => response.json()).then(x => JSON.stringify(x))";
            var result = await page.EvaluateExpressionAsync<string>(script);
            if (WasNotAllowed(result))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(result);
        }

        static async Task<T?> GetWithFetch<T>(Page page, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'GET', headers: {{'Content-Type': 'application/json'}}}}).then(response => response.json()).then(x => JSON.stringify(x))";
            var result = await page.EvaluateExpressionAsync<string>(script);
            if (WasNotAllowed(result))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(result);
        }

        static async Task<string?> VerifySignedIn(Page page, IConfiguration config)
        {
            var cookies = await page.GetCookiesAsync("https://www.geoguessr.com");
            var isSignedIn = cookies.Any(x => x.Name == "_ncfa");
            if (!isSignedIn)
            {
                var errorMessage = ++ErrorMessageCount > 3 ? null : $"@{config[TwitchClient.TwitchChannelConfigKey]} You have not signed in to https://www.geoguessr.com correctly.";
                return errorMessage;
            }

            return null;
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