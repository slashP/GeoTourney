using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GeoTourney
{
    internal class TournamentDataCreator
    {
        public static GithubTournamentData GenerateTournamentData(GeoTournament t)
        {
            var games = t.Games.Select(x =>
            {
                var game = x.PlayerGames.First().game;
                return new GameData
                {
                    allGuesses = game.rounds.Select((_, i) => x.PlayerGames.OrderByDescending(playerGame => playerGame.totalScore).Select(
                        p =>
                            new PlayerInRound
                            {
                                playerName = p.playerName,
                                playerId = p.userId,
                                roundScoreInPoints = p.game.player.guesses[i].roundScoreInPoints,
                                distanceInMeters = p.game.player.guesses[i].distanceInMeters,
                                lat = p.game.player.guesses[i].lat,
                                lng = p.game.player.guesses[i].lng
                            }).ToArray()).ToArray(),
                    playerGames = x.PlayerGames.Select(pg => new PlayerGameResult
                    {
                        player = pg.playerName,
                        playerId = pg.userId,
                        points = pg.totalScore,
                        r1 = pg.game.player.guesses.Skip(0).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r2 = pg.game.player.guesses.Skip(1).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r3 = pg.game.player.guesses.Skip(2).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r4 = pg.game.player.guesses.Skip(3).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r5 = pg.game.player.guesses.Skip(4).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        eliminatedInGame = EliminatedInGameDescription(t.Games, x, pg.userId)
                    }).ToList(),
                    answers = game.rounds.Select(r => new AnswerInGame { lat = r.lat, lng = r.lng }).ToArray(),
                    mapName = game.mapName,
                    gameNumber = x.GameNumber,
                    gameUrl = x.GameUrl,
                    forbidMoving = game.forbidMoving,
                    forbidRotating = game.forbidRotating,
                    forbidZooming = game.forbidZooming,
                    timeLimit = game.timeLimit,
                    gameDescription = GameDescription(x.PlayerGames.First().game),
                    playedWithEliminations = x.PlayedWithEliminations
                };
            }).OrderByDescending(x => x.gameNumber).ToArray();
            var tournament = new TournamentResult
            {
                players = t.Games.SelectMany(x => x.PlayerGames).GroupBy(x => x.userId).Select(x => new PlayerInTournamentResult
                {
                    playerId = x.Key,
                    playerName = x.First().playerName,
                    totalPoints = x.Sum(y => y.totalScore),
                    games = t.Games.Select(y => new GameResult
                    {
                        gamePoints = y.PlayerGames.FirstOrDefault(z => z.userId == x.Key)?.totalScore
                    }).ToArray()
                }).OrderByDescending(x => x.totalPoints).ToArray()
            };
            var result = new GithubTournamentData
            {
                games = games,
                tournament = tournament,
                nickname = t.Nickname
            };
            return result;
        }

        static string GameDescription(GeoTournament.Game game)
        {
            var restrictions = (game.forbidMoving, game.forbidZooming, game.forbidRotating) switch
            {
                (true, true, true) => "NMPZ",
                (true, true, false) => "No move, no zoom",
                (true, false, false) => "No move",
                (false, true, false) => "No zoom",
                _ => "Moving allowed"
            };
            return game.timeLimit.HasValue ? $"{game.timeLimit} sec. {restrictions}" : restrictions;
        }

        static string? EliminatedInGameDescription(
            IReadOnlyCollection<GeoTournament.GameObject> games,
            GeoTournament.GameObject thisGame,
            string userId)
        {
            var statusThisGame = thisGame.EliminationStatuses[userId];
            return statusThisGame switch
            {
                EliminationStatus.Revived => "+",
                EliminationStatus.Eliminated => GeoTournament.GameWhenPlayerWasEliminated(games, userId, thisGame.GameNumber)?.GameNumber.ToString(),
                EliminationStatus.DidNotPlayGame1 => "-",
                EliminationStatus.StillInTheGame => null,
                _ => null
            };
        }
    }

    public record PlayerGameResult
    {
        public string player { get; set; } = string.Empty;
        public string playerId { get; set; } = string.Empty;
        public int points { get; set; }
        public int r1 { get; set; }
        public int r2 { get; set; }
        public int r3 { get; set; }
        public int r4 { get; set; }
        public int r5 { get; set; }
        public string? eliminatedInGame { get; set; }
    }

    public record GameData
    {
        public PlayerInRound[][] allGuesses { get; set; } = Array.Empty<PlayerInRound[]>();
        public IList<PlayerGameResult> playerGames { get; set; } = Array.Empty<PlayerGameResult>();
        public IList<AnswerInGame> answers { get; set; } = Array.Empty<AnswerInGame>();
        public string mapName { get; set; } = string.Empty;
        public int gameNumber { get; set; }
        public Uri? gameUrl { get; set; }
        public string gameDescription { get; set; } = string.Empty;
        public bool playedWithEliminations { get; set; }
        public bool forbidMoving { get; set; }
        public bool forbidRotating { get; set; }
        public bool forbidZooming { get; set; }
        public int? timeLimit { get; set; }
    }

    public record AnswerInGame
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }

    public record GithubTournamentData
    {
        public string nickname { get; set; } = string.Empty;
        public TournamentResult tournament { get; set; } = new();
        public IList<GameData> games { get; set; } = Array.Empty<GameData>();
    }

    public record TournamentResult
    {
        public IList<PlayerInTournamentResult> players { get; set; } = Array.Empty<PlayerInTournamentResult>();
    }

    public record PlayerInTournamentResult
    {
        public string playerId { get; set; } = string.Empty;
        public string playerName { get; set; } = string.Empty;
        public int totalPoints { get; set; }
        public IList<GameResult> games { get; set; } = Array.Empty<GameResult>();
    }

    public record GameResult
    {
        public int? gamePoints { get; set; }
    }

    public class PlayerInRound
    {
        public string playerName { get; set; } = string.Empty;
        public string playerId { get; set; } = string.Empty;
        public int roundScoreInPoints { get; set; }
        public decimal distanceInMeters { get; set; }
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }
}