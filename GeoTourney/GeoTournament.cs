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

        public bool PlayWithEliminations { get; private set; }

        public GameState GameState { get; set; }

        public GeoTournament Restart()
        {
            return new()
            {
                PlayWithEliminations = PlayWithEliminations
            };
        }

        public async Task<string?> ToggleEliminations(Page page, IConfiguration config)
        {
            PlayWithEliminations = !PlayWithEliminations;
            // safeguarding from crazy situations.
            if (GameState == GameState.PendingEliminations)
            {
                return await EliminateAndFinish(page, config, 0);
            }

            return null;
        }

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

        async Task<string> FinishGame(IConfiguration config, PlayerGame[] playerGames, List<string> userIdsEliminated)
        {
            var newGame = new GameObject
            {
                GameUrl = new Uri($"https://www.geoguessr.com/results/{CurrentGameId}"),
                GameNumber = Games.Count + 1,
                MapName = playerGames.First().game.mapName,
                PlayerGames = playerGames,
                UserIdsEliminated = userIdsEliminated,
                PlayedWithEliminations = PlayWithEliminations
            };
            Games.Add(newGame);

            CurrentGameId = null;
            GameState = GameState.NotActive;

            var url = await GenerateUrlToLatestTournamentInfo(Games.ToList(), config);
            return $"Game #{newGame.GameNumber} finished. {url}";
        }

        public async Task<string?> CheckIfCurrentGameFinished(Page page, IConfiguration config)
        {
            if (GameState == GameState.NotActive || GameState == GameState.PendingEliminations)
            {
                return null;
            }

            var (error, playerGames) = await GeoguessrApi.LoadGame(CurrentGameId!, page, config);
            if (string.IsNullOrEmpty(error) && playerGames.Any())
            {
                if (PlayWithEliminations)
                {
                    var eliminationPossibilities =
                        EliminationPossibilities(playerGames, Games).DistinctBy(x => x.userId).ToList();
                    var didNotPlayThisGame = eliminationPossibilities.Where(x => x.points == null).ToList();
                    var didNotPlayDescritpion = didNotPlayThisGame.Any()
                        ? $" {didNotPlayThisGame.Count} did not play this round, but played the one before."
                        : string.Empty;
                    GameState = GameState.PendingEliminations;
                    return $"{eliminationPossibilities.Count} players are still in the game.{didNotPlayDescritpion} How many do you want to eliminate?";
                }

                var messageToChat = await FinishGame(config, playerGames, new List<string>());
                return messageToChat;
            }

            return error;
        }

        public async Task<string?> EliminateAndFinish(Page page, IConfiguration config, int numberOfEliminations)
        {
            if (GameState != GameState.PendingEliminations)
            {
                return null;
            }

            var (_, playerGames) = await GeoguessrApi.LoadGame(CurrentGameId!, page, config);
            var eliminationPossibilities =
                EliminationPossibilities(playerGames, Games).DistinctBy(x => x.userId).ToList();
            var eliminees = eliminationPossibilities.Take(numberOfEliminations).ToList();
            var url = await FinishGame(config, playerGames, eliminees.Select(x => x.userId).ToList());
            return $"{numberOfEliminations} players eliminated. {url}";
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
                new {Command = "!restart", Description = "Forget current tournament and start over"},
                new {Command = "!totalscore", Description = "Get a results page with all games and points summed."},
                new {Command = "!elim", Description = "Toggle elimination mode on/off."},
            };
            Console.WriteLine(ConsoleTable.From(rows).ToMinimalString());
        }

        public async Task<string> PrintGameScore(IConfigurationRoot config)
        {
            return await GenerateUrlToLatestTournamentInfo(Games, config);
        }

        public async Task SetCurrentGame(Uri urlToChallenge, Page page, IConfiguration config)
        {
            if (GameState == GameState.PendingEliminations)
            {
                await EliminateAndFinish(page, config, 0);
            }

            var gameId = urlToChallenge.PathAndQuery.Split('/').LastOrDefault();

            if (Games.All(x => x.GameUrl?.PathAndQuery.Split('/').LastOrDefault() != gameId))
            {
                CurrentGameId = gameId;
                GameState = GameState.Running;
            }
        }

        public async Task<string> PrintTotalScore(IConfigurationRoot config)
        {
            var data = TournamentDataCreator.GenerateJsData(Games, true);
            var url = await Github.UploadTournamentData(config, data);

            Console.WriteLine(url);
            return url;
        }

        public static EliminationStatus GetEliminationStatus(IReadOnlyCollection<GameObject> games, string userId)
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
            public bool PlayedWithEliminations { get; set; }
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

    public enum EliminationStatus
    {
        DidNotPlayGame1,
        Eliminated,
        StillInTheGame
    }

    public enum GameState
    {
        NotActive,
        Running,
        PendingEliminations
    }
}