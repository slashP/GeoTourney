using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoguessrApi
    {
        static int ErrorMessageCount;
        static readonly Dictionary<string, List<GeoTournament.PlayerGame>> _games = new();

        public static async Task<(string? error, IReadOnlyCollection<GeoTournament.PlayerGame> playerGames)> LoadGame(string gameId, Page page, IConfiguration config)
        {
            var error = await VerifySignedIn(page, config);
            if (error != null)
            {
                return (error, Array.Empty<GeoTournament.PlayerGame>());
            }

            if (_games.TryGetValue(gameId, out var cachedGames))
            {
                return (null, cachedGames);
            }

            var playerGames = new List<GeoTournament.PlayerGame>();
            List<GeoTournament.PlayerGame> games;
            var maxItems = 50;
            do
            {
                var jsonString = await GoToUrlAndGetJsonString(page, $"https://www.geoguessr.com/api/v3/results/scores/{gameId}/{playerGames.Count}/{maxItems}");
                if (WasNotAllowed(jsonString))
                {
                    return (null, Array.Empty<GeoTournament.PlayerGame>());
                }

                games = JsonSerializer.Deserialize<List<GeoTournament.PlayerGame>>(jsonString) ?? new List<GeoTournament.PlayerGame>();
                playerGames.AddRange(games);
            } while (games.Count == maxItems);

            _games.Add(gameId, playerGames);
            return (null, playerGames);
        }

        public static async Task<(string? error, string? link)> GenerateChallengeLink(Page page, IConfiguration config, ushort timeLimit, GameMode gameMode, string mapId)
        {
            var error = await VerifySignedIn(page, config);
            if (error != null)
            {
                return (error, null);
            }

            try
            {
                var challengeApiUrl = $"challenges";
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

        static async Task<T?> PostWithFetch<T>(Page page, object requestBody, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'POST', headers: {{'Content-Type': 'application/json'}}, body: JSON.stringify({requestBody})}}).then(response => response.json()).then(x => JSON.stringify(x))";
            var result = await page.EvaluateExpressionAsync<string>(script);
            return JsonSerializer.Deserialize<T>(result);
        }

        static async Task<T?> GetWithFetch<T>(Page page, string path)
        {
            var apiUrl = $"https://www.geoguessr.com/api/v3/{path}";
            var script = $@"fetch('{apiUrl}', {{method: 'GET', headers: {{'Content-Type': 'application/json'}}}}).then(response => response.json()).then(x => JSON.stringify(x))";
            var result = await page.EvaluateExpressionAsync<string>(script);
            return JsonSerializer.Deserialize<T>(result);
        }

        static async Task<string> GoToUrlAndGetJsonString(Page page, string url)
        {
            await page.GoToAsync(url);
            var content = await page.MainFrame.GetContentAsync();
            var jsonString =
                new string(content.SkipWhile(x => x != '[' && x != '{').TakeWhile(x => x != '<').ToArray());
            return jsonString;
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