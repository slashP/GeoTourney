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
        static int ErrorMessageCount;
        static readonly Dictionary<string, List<GeoTournament.PlayerGame>> _games = new();

        public static async Task<(string? error, IReadOnlyCollection<GeoTournament.PlayerGame> playerGames)> LoadGame(string gameId, Page page, IConfiguration config)
        {
            var cookies = await page.GetCookiesAsync("https://www.geoguessr.com");
            var isSignedIn = cookies.Any(x => x.Name == "_ncfa");
            if (!isSignedIn)
            {
                var errorMessage = ++ErrorMessageCount > 3 ? null : $"@{config[TwitchClient.TwitchChannelConfigKey]} You have not signed in to https://www.geoguessr.com correctly.";
                return (errorMessage, Array.Empty<GeoTournament.PlayerGame>());
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
                await page.GoToAsync($"https://www.geoguessr.com/api/v3/results/scores/{gameId}/{playerGames.Count}/{maxItems}");
                var content = await page.MainFrame.GetContentAsync();
                var jsonString =
                    new string(content.SkipWhile(x => x != '[' && x != '{').TakeWhile(x => x != '<').ToArray());
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
    }
}