using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoguessrApi
    {
        static readonly Dictionary<string, GeoTournament.PlayerGame[]> _games = new();

        public static async Task<GeoTournament.PlayerGame[]> LoadGame(string gameId, Page page)
        {
            if (_games.TryGetValue(gameId, out var games))
            {
                return games;
            }

            await page.GoToAsync($"https://www.geoguessr.com/api/v3/results/scores/{gameId}/0/10000");
            var content = await page.MainFrame.GetContentAsync();
            var jsonString = new string(content.SkipWhile(x => x != '[' && x != '{').TakeWhile(x => x != '<').ToArray());
            if (WasNotAllowed(jsonString))
            {
                return Array.Empty<GeoTournament.PlayerGame>();
            }

            var playerGames = JsonSerializer.Deserialize<GeoTournament.PlayerGame[]>(jsonString) ?? Array.Empty<GeoTournament.PlayerGame>();
            _games.Add(gameId, playerGames);
            return playerGames;
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