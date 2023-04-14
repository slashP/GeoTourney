using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;
using PuppeteerSharp;

namespace GeoTourney.Core
{
    public class GeoguessrApi
    {
        static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri("https://www.geoguessr.com/")
        };

        const int MaxApiCallsPerHour = 100;
        const string AuthenticationFilePath = "local-authentication.json";
        static int ErrorMessageCount;
        static readonly Dictionary<string, List<GeoTournament.PlayerGame>> CachedGames = new();
        static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
        static readonly SizeAndTimeLimitedQueue ChallengeResultsCallsPerHour = new(Lifetime, MaxApiCallsPerHour);
        static readonly SizeAndTimeLimitedQueue ChallengeLinkCallsPerHour = new(Lifetime, MaxApiCallsPerHour);
        static readonly GeoTournament.PlayerGame[] Empty = Array.Empty<GeoTournament.PlayerGame>();

        public static async Task<(string? error, IReadOnlyCollection<GeoTournament.PlayerGame> playerGames)> LoadGame(string gameId, IPage page, IConfiguration config)
        {
            var bannedUsersIds = await AppDataProvider.BannedUsersIds();

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
                ChallengeResult? challengeResult = null;
                var maxItems = 26;
                do
                {
                    limitPerHour.TryEnqueue(DateTime.UtcNow);
                    var paginationToken = !string.IsNullOrEmpty(challengeResult?.paginationToken)
                        ? $"&paginationToken={challengeResult.paginationToken}"
                        : string.Empty;
                    challengeResult = await GetWithFetch<ChallengeResult>(
                                              page,
                                              $"results/highscores/{gameId}?friends=false&limit={maxItems}{paginationToken}") ??
                                          new ChallengeResult();
                    playerGames.AddRange(challengeResult.items);
                    if (!playerGames.Any())
                    {
                        return ("It looks like you haven't finished the game?", Empty);
                    }

                } while (challengeResult.items.Length == maxItems && !limitPerHour.IsFull());

                var filteredGames = playerGames.Where(x => !bannedUsersIds.Contains(x.userId)).ToList();
                CachedGames.Add(gameId, filteredGames);
                return (null, filteredGames);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ("Unexpected error.", Empty);
            }
        }

        public static async Task<(string? error, string? gameId, string? mapId)> GenerateChallengeLink(IPage page, IConfiguration config, ushort timeLimit, GameMode gameMode, string mapId)
        {
            var error = await VerifySignedIn(page, config);
            if (error != null)
            {
                return (error, null, null);
            }

            var limitPerHour = ChallengeLinkCallsPerHour;
            if (limitPerHour.IsFull())
            {
                return (RateLimitResponse(), null, null);
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
                var requestBody = new
                {
                    map = mapId,
                    isCountryStreak = false,
                    roundTime = 0,
                    forbidMoving = forbidMoving,
                    forbidZooming = forbidZooming,
                    forbidRotating = forbidRotating,
                    timeLimit = timeLimit
                };
                limitPerHour.TryEnqueue(DateTime.UtcNow);
                var result = await PostWithFetch<ChallengeApiResult>(page, requestBody, "challenges");
                if (result == null)
                {
                    return ("Unknown response type", null, null);
                }

                return (null, result.token, mapId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ("Unexpected error.", null, null);
            }
        }

        public static async Task GenerateMap(IPage page, string folder)
        {
            var locations = JsonSerializer.Deserialize<List<GeoguessrMapLocation>>(await File.ReadAllTextAsync(Path.Combine(folder, "locations.json"))) ?? new();
            var metadataPath = Path.Combine(folder, "metadata.json");
            var geoguessrMap = (JsonSerializer.Deserialize<GeoguessrMap>(await File.ReadAllTextAsync(metadataPath)) ?? new()) with
            {
                customCoordinates = locations
            };
            var url = string.IsNullOrEmpty(geoguessrMap.id) ? "profiles/maps" : $"profiles/maps/{geoguessrMap.id}";
            var result = await PostWithFetch<CreateMapResult>(page, geoguessrMap, url);
            if (result != null)
            {
                var metadata = geoguessrMap with
                {
                    id = result.id,
                    customCoordinates = new()
                };
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
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

        public static async Task<bool> TrySignInFromLocalFile(IPage page)
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

        public static string ChallengeLink(string gameId) => $"https://www.geoguessr.com/challenge/{gameId}";

        public static string ResultsLink(string gameId) => $"https://www.geoguessr.com/results/{gameId}";

        static async Task<T?> PostWithFetch<T>(IPage page, object requestBody, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/v3/{path}");
            var stringContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            return await CallGeoguessrApi<T>(page, request);
        }

        static async Task<T?> GetWithFetch<T>(IPage page, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v3/{path}");
            return await CallGeoguessrApi<T>(page, request);
        }

        static async Task<T?> CallGeoguessrApi<T>(IPage page, HttpRequestMessage request)
        {
            var signInCookie = await GetSignInCookie(page);
            request.Headers.Add("Cookie", $"{signInCookie!.Name}={signInCookie.Value}");
            var response = await Client.SendAsync(request);
            if (WasNotAllowed(response))
            {
                return default;
            }

            await SaveTokenInLocalAuthenticationFile(page);
            try
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception)
            {
                Console.WriteLine($"JSON response:{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
                throw;
            }
        }

        static async Task SaveTokenInLocalAuthenticationFile(IPage page)
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


        static async Task<string?> VerifySignedIn(IPage page, IConfiguration config)
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

        static async Task<CookieParam?> GetSignInCookie(IPage page)
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

        static bool WasNotAllowed(HttpResponseMessage response) => response.StatusCode == HttpStatusCode.Unauthorized;

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

        record CreateMapResult
        {
            public string id { get; set; } = string.Empty;
        }

        record GeoguessrMap
        {
            public string? id { get; set; }
            public bool highlighted { get; set; }
            public string name { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public Avatar avatar { get; set; } = new();
            public bool published { get; set; }
            public List<GeoguessrMapLocation> customCoordinates { get; set; } = new();
        }

        record Avatar
        {
            public string background { get; set; } = string.Empty;
            public string landscape { get; set; } = string.Empty;
            public string ground { get; set; } = string.Empty;
            public string decoration { get; set; } = string.Empty;
        }


        record GeoguessrMapLocation
        {
            public float lat { get; set; }
            public float lng { get; set; }
            public float heading { get; set; }
            public int pitch { get; set; }
        }

        public enum GameMode
        {
            Invalid,
            Move,
            NoMove,
            NMPZ
        }

        record ChallengeResult
        {
            public GeoTournament.PlayerGame[] items { get; set; } = Array.Empty<GeoTournament.PlayerGame>();
            public string paginationToken { get; set; } = string.Empty;
        }
    }
}