using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoTournament
    {
        public GeoTournament()
        {
            Games = new List<GameObject>();
        }

        public List<GameObject> Games { get; }

        public string? CurrentGameId { get; set; }

        static IEnumerable<(string userId, int? points)> EliminationPossibilities(PlayerGame[] currentGame, List<GameObject> games)
        {
            var allPlayerIdsInAllGames = games.Select(x => x.PlayerGames.Select(y => y.userId)).Concat(new[] { currentGame.Select(y => y.userId) }.AsEnumerable()).ToArray();
            var userIdsThatPlayedAllGames = allPlayerIdsInAllGames
                .Aggregate((previousList, nextList) => previousList.Intersect(nextList)).ToArray();
            var userIdsThatDidntPlayAllGames = allPlayerIdsInAllGames.SelectMany(x => x).Except(userIdsThatPlayedAllGames).ToArray();
            foreach (var playerGames in games.SelectMany(x => x.PlayerGames).Concat(currentGame).Where(x => userIdsThatDidntPlayAllGames.Contains(x.userId))
                .GroupBy(x => x.userId).OrderBy(x => x.Sum(y => y.totalScore)))
            {
                var userId = playerGames.Key;
                if (GetEliminationStatus(games, userId) == EliminationStatus.StillInTheGame
                ) // Did not play all rounds, but is still in the tournament.
                {
                    yield return (userId, null);
                }
            }

            var eliminationPossibilities = currentGame.Where(x => GetEliminationStatus(games, x.userId) == EliminationStatus.StillInTheGame)
                .OrderBy(x => x.totalScore).ToList();
            foreach (var eliminationPossibility in eliminationPossibilities)
            {
                yield return (eliminationPossibility.userId, eliminationPossibility.totalScore);
            }
        }

        async Task<string> FinishGame(Uri uri, IConfiguration config, PlayerGame[] playerGames)
        {
            var eliminationPossibilities =
                EliminationPossibilities(playerGames, Games).DistinctBy(x => x.userId).ToList();
            var didNotPlayThisGame = eliminationPossibilities.Where(x => x.points == null).ToList();
            var didNotPlayDescritpion = didNotPlayThisGame.Any()
                ? $" {didNotPlayThisGame.Count} did not play this round, but played the one before."
                : string.Empty;
            var eliminate = "0";
            int.TryParse(eliminate, out var numberOfEliminations);
            var eliminees = eliminationPossibilities.Take(numberOfEliminations).ToList();
            Games.Add(new GameObject
            {
                GameUrl = uri,
                GameNumber = Games.Count + 1,
                MapName = playerGames.First().game.mapName,
                PlayerGames = playerGames,
                UserIdsEliminated = eliminees.Select(x => x.userId).ToList()
            });
            var gist = await GenerateUrlToLatestTournamentInfo(Games.ToList(), config);
            return gist;
        }

        public async Task<(bool finished, string? gistUrl)> CheckIfCurrentGameFinished(Page page, IConfigurationRoot config)
        {
            if (CurrentGameId == null)
            {
                return (false, null);
            }

            var playerGames = await GeoguessrApi.LoadGame(CurrentGameId, page);
            if (playerGames.Any())
            {
                var url = await FinishGame(new Uri($"https://www.geoguessr.com/results/{CurrentGameId}"), config, playerGames);
                CurrentGameId = null;
                return (true, url);
            }

            return (false, null);
        }

        static async Task<string> GenerateUrlToLatestTournamentInfo(List<GameObject> games, IConfiguration configuration)
        {
            var data = TournamentDataCreator.GenerateJsData(games, false);
            var url = await Github.UploadTournamentData(configuration, data);
            return url;
        }

        public static void PrintCommands()
        {
            var rows = new[]
            {
                new {Command = "[URL]", Description = "Post a Geoguessr challenge page URL to Twitch chat."},
                new {Command = "exit", Description = "Stop the program"},
                new {Command = "!restart", Description = "Forgets the current tournament and starts a new one."},
                new {Command = "!totalscore", Description = "Prints the total (summed) score for all games in the current tournament, sends it to Hastebin and copies the Hastebin URL to clipboard."}
            };
            Console.WriteLine(ConsoleTable.From(rows).ToMinimalString());
        }

        public async Task<string> PrintGameScore(IConfigurationRoot config)
        {
            return await GenerateUrlToLatestTournamentInfo(Games, config);
        }

        public void SetCurrentGame(Uri urlToChallenge)
        {
            var gameId = urlToChallenge.PathAndQuery.Split('/').LastOrDefault();

            if (Games.All(x => x.GameUrl?.PathAndQuery.Split('/').LastOrDefault() != gameId))
            {
                CurrentGameId = gameId;
            }
        }

        public async Task<string> PrintTotalScore(IConfigurationRoot config)
        {
            var data = TournamentDataCreator.GenerateJsData(Games, true);
            var url = await Github.UploadTournamentData(config, data);

            Console.WriteLine(url);
            return url;
        }

        static EliminationStatus GetEliminationStatus(IReadOnlyCollection<GameObject> games, string userId)
        {
            if (games.FirstOrDefault()?.PlayerGames.Any(x => x.userId == userId) == false)
            {
                return EliminationStatus.DidNotPlayGame1;
            }

            return games.SelectMany(y => y.UserIdsEliminated).Contains(userId)
                ? EliminationStatus.Eliminated
                : EliminationStatus.StillInTheGame;
        }

        static string EliminatedInGameDescription(IReadOnlyCollection<GameObject> games, string userId)
        {
            return GetEliminationStatus(games, userId) == EliminationStatus.DidNotPlayGame1
                ? "-"
                : games.FirstOrDefault(g => g.UserIdsEliminated.Contains(userId))?.GameNumber.ToString() ??
                  string.Empty;
        }

        public record GameObject
        {
            public int GameNumber { get; set; }
            public PlayerGame[] PlayerGames { get; set; } = Array.Empty<PlayerGame>();
            public List<string> UserIdsEliminated { get; set; } = new();
            public string MapName { get; set; } = string.Empty;
            public Uri? GameUrl { get; set; }
        }

        public record PlayerGame
        {
            public string playerName { get; set; } = string.Empty;
            public string userId { get; set; } = string.Empty;
            public int totalScore { get; set; }
            public Game game { get; set; } = new();
        }

        public record Game
        {
            public string mapName { get; set; } = string.Empty;
            public int? timeLimit { get; set; }
            public bool forbidMoving { get; set; }
            public bool forbidZooming { get; set; }
            public bool forbidRotating { get; set; }
            public Player player { get; set; } = new();
            public Round[] rounds { get; set; } = Array.Empty<Round>();
        }

        public record Round
        {
            public decimal lat { get; set; }

            public decimal lng { get; set; }
        }

        public record Player
        {
            public string id { get; set; } = string.Empty;
            public Guess[] guesses { get; set; } = Array.Empty<Guess>();
        }

        public record Guess
        {
            public decimal distanceInMeters { get; set; }
            public int roundScoreInPoints { get; set; }
            public decimal lat { get; set; }
            public decimal lng { get; set; }
        }
    }

    internal enum EliminationStatus
    {
        DidNotPlayGame1,
        Eliminated,
        StillInTheGame
    }
}