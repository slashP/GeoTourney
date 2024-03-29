﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney.Core
{
    public class GeoTournament
    {
        static readonly EliminationStatus[] StatusesWhereYouCanGetEliminated = {EliminationStatus.Revived, EliminationStatus.StillInTheGame};

        static readonly EliminationStatus[] StatusesWhereYouCanGetRevived = {EliminationStatus.DidNotPlayGame1, EliminationStatus.Eliminated};

        public GeoTournament(string nickname, DateTime startTimeUtc)
        {
            Games = new List<GameObject>();
            Nickname = nickname;
            StartTimeUtc = startTimeUtc;
        }

        public List<GameObject> Games { get; }

        public string Nickname { get; }

        public string? CurrentGameId { get; set; }

        public string? CurrentMapId { get; set; }

        public string? CurrentGithubResultsPageUrl { get; set; }

        public bool PlayWithEliminations { get; private set; }

        public GameState GameState { get; set; }

        public DateTime StartTimeUtc { get; }

        public GeoTournament Restart(string nickname)
        {
            return new(nickname, DateTime.UtcNow)
            {
                PlayWithEliminations = PlayWithEliminations
            };
        }

        public async Task<string?> ToggleEliminations(IPage page, IConfiguration config)
        {
            PlayWithEliminations = !PlayWithEliminations;
            // safeguarding from crazy situations.
            if (GameState == GameState.PendingEliminations)
            {
                return await EliminateAndFinish(page, config, 0, SaveMode.Automatic);
            }

            return null;
        }

        static IEnumerable<(string userId, int? points)> EliminationPossibilities(IReadOnlyCollection<PlayerGame> currentGame, List<GameObject> games)
        {
            var allPlayerIdsInAllGames = games.SelectMany(x => x.PlayerGames.Select(y => y.userId))
                .Concat(currentGame.Select(y => y.userId)).Distinct().ToList();
            var eliminationPossibilities = allPlayerIdsInAllGames.Select(x => new
                {
                    userId = x,
                    status = GetEliminationStatus(games, x),
                    totalScore = currentGame.FirstOrDefault(y => y.userId == x)?.totalScore
                }).Where(x => x.status == EliminationStatus.StillInTheGame ||
                              x.status == EliminationStatus.Revived)
                .OrderBy(x => x.totalScore).ToList();
            foreach (var eliminationPossibility in eliminationPossibilities)
            {
                yield return (eliminationPossibility.userId, eliminationPossibility.totalScore);
            }
        }

        async Task<string> FinishGame(IConfiguration config, IReadOnlyCollection<PlayerGame> playerGames, List<string> userIdsEliminated, SaveMode saveMode)
        {
            var eliminationStatuses = PlayWithEliminations
                ? Games.SelectMany(x => x.PlayerGames.Select(y => y.userId))
                    .Concat(playerGames.Select(y => y.userId)).Distinct().ToDictionary(id => id,
                        id => NewEliminationStatus(userIdsEliminated, id))
                : new();
            var game = playerGames.First().game;
            var locations = await GeocodingClient.Locations(config, game.rounds);
            var newGame = new GameObject
            {
                GameUrl = new Uri($"https://www.geoguessr.com/results/{CurrentGameId}"),
                GameId = CurrentGameId!,
                MapId = CurrentMapId,
                GameNumber = Games.Count + 1,
                MapName = game.mapName,
                PlayerGames = playerGames,
                EliminationStatuses = eliminationStatuses,
                PlayedWithEliminations = PlayWithEliminations,
                RoundLocations = locations
            };
            Games.Add(newGame);

            CurrentGameId = null;
            GameState = GameState.NotActive;

            var url = await GenerateUrlToLatestTournamentInfo(config, saveMode);
            return $"Game #{newGame.GameNumber} finished. {url}";
        }

        EliminationStatus NewEliminationStatus(List<string> userIdsEliminated, string id)
        {
            if (userIdsEliminated.Contains(id))
                return EliminationStatus.Eliminated;
            var status = GetEliminationStatus(Games, id);
            return status == EliminationStatus.Revived ? EliminationStatus.StillInTheGame : status;
        }

        public async Task<(string? messsageToChat, string? error)> CheckIfCurrentGameFinished(IPage page, IConfiguration config, SaveMode saveMode)
        {
            if (GameState == GameState.NotActive || GameState == GameState.PendingEliminations)
            {
                return (null, null);
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
                    return ($"{eliminationPossibilities.Count} players are still in the game.{didNotPlayDescritpion} How many do you want to eliminate?", null);
                }

                var messageToChat = await FinishGame(config, playerGames, new List<string>(), saveMode);
                return (messageToChat, null);
            }

            return (null, error);
        }

        public async Task<string?> EliminateAndFinish(IPage page, IConfiguration config, int numberOfEliminations, SaveMode saveMode)
        {
            if (GameState != GameState.PendingEliminations)
            {
                return null;
            }

            var (_, playerGames) = await GeoguessrApi.LoadGame(CurrentGameId!, page, config);
            var eliminationPossibilities =
                EliminationPossibilities(playerGames, Games).DistinctBy(x => x.userId).ToList();
            var eliminees = eliminationPossibilities.Take(numberOfEliminations).ToList();
            var url = await FinishGame(config, playerGames, eliminees.Select(x => x.userId).ToList(), saveMode);
            return $"{numberOfEliminations} players eliminated. {url}";
        }

        public async Task<string?> EliminateAndFinish(IPage page, PointsDescription pointsDescription, int threshold, IConfiguration config, SaveMode saveMode)
        {
            if (GameState != GameState.PendingEliminations)
            {
                return null;
            }

            var (_, playerGames) = await GeoguessrApi.LoadGame(CurrentGameId!, page, config);
            var possibilities = EliminationPossibilities(playerGames, Games);
            var selector = PointsSelector(pointsDescription, threshold);
            var matchingEliminations = possibilities.Where(selector).ToList();
            var url = await FinishGame(config, playerGames, matchingEliminations.Select(x => x.userId).ToList(), saveMode);
            return $"{matchingEliminations.Count} players eliminated. {url}";
        }

        public async Task<string?> EliminateSpecificPlayer(string playerSearchTerm, IConfiguration config, SaveMode saveMode)
        {
            if (!PlayWithEliminations)
            {
                return "Elimination mode is off.";
            }

            var currentGame = Games.LastOrDefault();
            if (currentGame == null)
            {
                return null;
            }

            var (match, error) = MatchingPlayerOrError(playerSearchTerm);
            if (error != null || match == null)
            {
                return error;
            }

            var currentEliminationStatus = currentGame.EliminationStatuses[match.Value.userId];
            switch (currentEliminationStatus)
            {
                case EliminationStatus.DidNotPlayGame1:
                    return $"{match.Value.playerName} did not play game 1 and is therefore not considered to be in the tournament.";
                case EliminationStatus.Eliminated:
                {
                    var gameNumber = GameWhenPlayerWasEliminated(Games, match.Value.userId, currentGame.GameNumber)?.GameNumber;
                    return $"{match.Value.playerName} was already eliminated in game #{gameNumber}.";
                }
                case EliminationStatus.Revived:
                case EliminationStatus.StillInTheGame:
                {
                    currentGame.EliminationStatuses[match.Value.userId] = EliminationStatus.Eliminated;
                    var url = await GenerateUrlToLatestTournamentInfo(config, saveMode);
                    return $"{match.Value.playerName} eliminated. {url}";
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<string?> EliminatePlayers(PointsDescription pointsDescription, int threshold, IConfiguration config)
        {
            return await MassActionOnMatchingPlayers(pointsDescription, threshold, config, StatusesWhereYouCanGetEliminated, EliminationStatus.Eliminated, "eliminated");
        }

        public async Task<string?> RevivePlayers(PointsDescription pointsDescription, int threshold, IConfiguration config)
        {
            return await MassActionOnMatchingPlayers(pointsDescription, threshold, config, StatusesWhereYouCanGetRevived, EliminationStatus.Revived, "revived");
        }

        public void UpdateBans(IReadOnlyCollection<string> bannedUserIds)
        {
            foreach (var gameObject in Games)
            {
                gameObject.PlayerGames = gameObject.PlayerGames.Where(x => !bannedUserIds.Contains(x.userId)).ToList();
                gameObject.EliminationStatuses = gameObject.EliminationStatuses
                    .Where(x => !bannedUserIds.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        async Task<string?> MassActionOnMatchingPlayers(
            PointsDescription pointsDescription,
            int threshold,
            IConfiguration config,
            EliminationStatus[] eligibleStatuses,
            EliminationStatus newStatus,
            string actionVerb)
        {
            if (!PlayWithEliminations)
            {
                return "Elimination mode is off.";
            }

            var currentGame = Games.LastOrDefault();
            if (currentGame == null)
            {
                return null;
            }

            var selector = PointsDescriptionSelector(pointsDescription, threshold);
            var matchingPlayers = currentGame.PlayerGames.Where(selector).Where(x =>
                currentGame.EliminationStatuses.ContainsKey(x.userId) &&
                eligibleStatuses.Contains(currentGame.EliminationStatuses[x.userId])).ToList();
            foreach (var matchingPlayer in matchingPlayers)
            {
                currentGame.EliminationStatuses[matchingPlayer.userId] = newStatus;
            }

            var url = await GenerateUrlToLatestTournamentInfo(config, SaveMode.Automatic);
            return $"{matchingPlayers.Count} {"player".Pluralize(matchingPlayers.Count)} {actionVerb}. {url}";
        }

        static Func<PlayerGame, bool> PointsDescriptionSelector(PointsDescription pointsDescription, int threshold) =>
            pointsDescription == PointsDescription.LessThan
                ? x => x.totalScore < threshold
                : x => x.totalScore > threshold;

        static Func<(string userId, int? points), bool> PointsSelector(PointsDescription pointsDescription, int threshold) =>
            pointsDescription == PointsDescription.LessThan
                ? x => x.points == null || x.points < threshold
                : x => x.points == null || x.points > threshold;

        public async Task<string?> ReviveSpecificPlayer(string playerSearchTerm, IConfiguration config)
        {
            if (!PlayWithEliminations)
            {
                return "Elimination mode is off.";
            }

            var currentGame = Games.LastOrDefault();
            if (currentGame == null)
            {
                return null;
            }

            var (match, error) = MatchingPlayerOrError(playerSearchTerm);
            if (error != null || match == null)
            {
                return error;
            }

            var currentEliminationStatus = currentGame.EliminationStatuses[match.Value.userId];
            switch (currentEliminationStatus)
            {
                case EliminationStatus.DidNotPlayGame1:
                case EliminationStatus.Eliminated:
                {
                    currentGame.EliminationStatuses[match.Value.userId] = EliminationStatus.Revived;
                    var url = await GenerateUrlToLatestTournamentInfo(config, SaveMode.Automatic);
                    return $"{match.Value.playerName} revived. {url}";
                }
                case EliminationStatus.Revived:
                    return $"{match.Value.playerName} was already revived.";
                case EliminationStatus.StillInTheGame:
                    return $"{match.Value.playerName} was still in the game.";
                default: throw new ArgumentOutOfRangeException();
            }
        }

        ((string userId, string playerName)?, string? error) MatchingPlayerOrError(string playerSearchTerm)
        {
            var matches = MatchingPlayers(playerSearchTerm);
            if (!matches.Any())
            {
                return (null, $"No matching player found for '{playerSearchTerm}'");
            }

            if (matches.Count != 1)
            {
                return (null, $"More than one match found. Narrow down the search. '{playerSearchTerm}' |> {string.Join(", ", matches.Take(2).Select(x => x.playerName))}");
            }

            var (userId, playerName) = matches.Single();

            return ((userId, playerName), null);
        }

        public static GameObject? GameWhenPlayerWasEliminated(IEnumerable<GameObject> games, string userId, int fromGame)
        {
            return games.Where(x => x.GameNumber <= fromGame).OrderByDescending(x => x.GameNumber)
                    .SkipWhile(x => x.EliminationStatuses.TryGetValue(userId, out var status) && status != EliminationStatus.Eliminated)
                    .TakeWhile(x => x.EliminationStatuses.TryGetValue(userId, out var status) && status == EliminationStatus.Eliminated)
                .LastOrDefault();
        }

        List<(string userId, string playerName)> MatchingPlayers(string playerSearchTerm)
        {
            var searchTermIsPointsSearch = int.TryParse(playerSearchTerm, out var numberOfPointsSearch);
            var pointSearchMatches = searchTermIsPointsSearch
                ? Games.Last().PlayerGames.Where(x => x.totalScore == numberOfPointsSearch)
                : Enumerable.Empty<PlayerGame>();
            var nameSearchMatches = Games.SelectMany(x => x.PlayerGames).Where(x =>
                x.playerName.Contains(playerSearchTerm, StringComparison.InvariantCultureIgnoreCase));
            var matches = pointSearchMatches.Concat(nameSearchMatches);
            return matches.GroupBy(x => x.userId).Select(x => (x.Key, x.Last().playerName)).ToList();
        }

        async Task<string?> GenerateUrlToLatestTournamentInfo(IConfiguration configuration, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Manual)
            {
                return null;
            }

            var url = await SaveAndGetGithubTournamentUrl(configuration);
            return url;
        }

        public async Task<string> SaveAndGetGithubTournamentUrl(IConfiguration configuration)
        {
            var data = TournamentDataCreator.GenerateTournamentData(this);
            var url = await Github.UploadTournamentData(configuration, data);
            CurrentGithubResultsPageUrl = url;
            return url;
        }

        public static void PrintCommands()
        {
            var rows = new[]
            {
                new {Command = "!game [mapshortcut] [timelimit] [gamemode]", Description = "Create a challenge URL and start a game."},
                new {Command = "!restart", Description = "Forget current tournament and start over."},
                new {Command = "!totalscore", Description = "Get a results page with all games and points summed."},
                new {Command = "!elim", Description = "Toggle elimination mode on/off."},
                new {Command = "!elim slashpeek", Description = "Eliminate one specific player with name slashpeek."},
                new {Command = "!revive slashpeek", Description = "Revive one specific player with name slashpeek."},
                new {Command = "!elim less than N", Description = "Eliminate all players with less than N points in the last game."},
                new {Command = "!elim more than N", Description = "Eliminate all players with more than N points in the last game."},
                new {Command = "!revive less than N", Description = "Revive all players with less than N points in the last game."},
                new {Command = "!revive more than N", Description = "Revive all players with more than N points in the last game."},
                new {Command = "!currentgame", Description = "Get the current challenge URL."},
                new {Command = "!apiinfo", Description = "See how many Geoguessr API calls have been made since startup."},
                new {Command = "!shutdown", Description = "Completely stop the program. Will require restart and signing in again."},
            };
            Console.WriteLine(ConsoleTable.From(rows).ToMinimalString());
        }

        public async Task<string?> PrintGameScore(IConfiguration config)
        {
            return await GenerateUrlToLatestTournamentInfo(config, SaveMode.Automatic);
        }

        public async Task<string?> SetCurrentGame(string gameId, IPage page, IConfiguration config, string? mapId, SaveMode saveMode)
        {
            if (GameState == GameState.PendingEliminations)
            {
                return await EliminateAndFinish(page, config, 0, saveMode);
            }

            if (IsCurrentGameSameAs(gameId))
            {
                return null;
            }

            var hasNotBeenPlayed = Games.All(x => x.GameId != gameId);
            if (hasNotBeenPlayed)
            {
                CurrentGameId = gameId;
                CurrentMapId = mapId;
                GameState = GameState.Running;
                var currentGameNumber = CurrentGameNumber();
                var urlToChallenge = GeoguessrApi.ChallengeLink(gameId);
                return currentGameNumber == 1
                    ? $"First game of tournament \"{Nickname}\": {urlToChallenge} Eliminations are {PlayWithEliminations.ToOnOrOffString()}"
                    : $"Game #{currentGameNumber} {urlToChallenge}";
            }

            return "That game URL has already been played.";
        }

        public bool IsCurrentGameSameAs(string gameId) => gameId == CurrentGameId;

        public string? CurrentGameUrl() => CurrentGameId != null
            ? GeoguessrApi.ChallengeLink(CurrentGameId)
            : null;

        public static EliminationStatus GetEliminationStatus(IReadOnlyCollection<GameObject> games, string userId)
        {
            if (!games.Any())
            {
                return EliminationStatus.StillInTheGame;
            }

            if (games.Count == 1 && games.FirstOrDefault()?.PlayerGames.Any(x => x.userId == userId) == false)
            {
                return EliminationStatus.DidNotPlayGame1;
            }

            var previousStatus = games.Reverse().SelectMany(y => y.EliminationStatuses).FirstOrDefault(x => x.Key == userId).Value;
            return previousStatus;
        }

        public int CurrentGameNumber()
        {
            return (Games.LastOrDefault()?.GameNumber ?? 0) + 1;
        }

        public record GameObject
        {
            public int GameNumber { get; set; }
            public IReadOnlyCollection<PlayerGame> PlayerGames { get; set; } = Array.Empty<PlayerGame>();
            public Dictionary<string, EliminationStatus> EliminationStatuses { get; set; } = new();
            public string MapName { get; set; } = string.Empty;
            public Uri? GameUrl { get; set; }
            public string GameId { get; set; } = string.Empty;
            public string? MapId { get; set; }
            public bool PlayedWithEliminations { get; set; }
            public IReadOnlyCollection<RoundLocation> RoundLocations { get; set; } = Array.Empty<RoundLocation>();
        }

        public record PlayerGame
        {
            public string playerName { get; set; } = string.Empty;
            public string userId { get; set; } = string.Empty;
            public int totalScore { get; set; }
            public string pinUrl { get; set; } = string.Empty;
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
            public decimal totalDistanceInMeters { get; set; }
            public decimal totalTime { get; set; }
            public Guess[] guesses { get; set; } = Array.Empty<Guess>();
        }

        public record Guess
        {
            public decimal distanceInMeters { get; set; }
            public int roundScoreInPoints { get; set; }
            public decimal time { get; set; }
            public decimal lat { get; set; }
            public decimal lng { get; set; }
        }
    }

    public enum SaveMode
    {
        Automatic,
        Manual
    }

    public enum EliminationStatus
    {
        DidNotPlayGame1,
        Eliminated,
        Revived,
        StillInTheGame
    }

    public enum GameState
    {
        NotActive,
        Running,
        PendingEliminations
    }
}