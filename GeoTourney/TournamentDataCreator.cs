using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GeoTourney
{
    internal class TournamentDataCreator
    {
        public static string GenerateJsData(List<GeoTournament.GameObject> g, bool includeTotalScore)
        {
            var games = g.OrderByDescending(x => x.GameNumber).Select(x =>
            {
                var game = x.PlayerGames.First().game;
                return new
                {
                    allGuesses = game.rounds.Select((_, i) => x.PlayerGames.OrderByDescending(playerGame => playerGame.totalScore).Select(
                        p =>
                            new PlayerInRound
                            {
                                playerName = p.playerName,
                                roundScoreInPoints = p.game.player.guesses[i].roundScoreInPoints,
                                distanceInMeters = p.game.player.guesses[i].distanceInMeters,
                                lat = p.game.player.guesses[i].lat,
                                lng = p.game.player.guesses[i].lng
                            }).ToArray()).ToArray(),
                    playerGames = x.PlayerGames.Select(pg => new
                    {
                        player = pg.playerName,
                        points = pg.totalScore,
                        r1 = pg.game.player.guesses.Skip(0).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r2 = pg.game.player.guesses.Skip(1).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r3 = pg.game.player.guesses.Skip(2).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r4 = pg.game.player.guesses.Skip(3).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        r5 = pg.game.player.guesses.Skip(4).FirstOrDefault()?.roundScoreInPoints ?? 0,
                        eliminatedInGame = EliminatedInGameDescription(g, pg.userId)
                    }).ToList(),
                    answers = game.rounds.Select(x => new { x.lat, x.lng }).ToArray(),
                    mapName = game.mapName,
                    gameNumber = x.GameNumber,
                    gameUrl = x.GameUrl,
                    gameDescription = GameDescription(x.PlayerGames.First().game),
                    playedWithEliminations = x.PlayedWithEliminations
                };
            });
            var tournament = includeTotalScore ? new
            {
                players = g.SelectMany(x => x.PlayerGames).GroupBy(x => x.userId).Select(x => new
                {
                    playerName = x.First().playerName,
                    totalPoints = x.Sum(y => y.totalScore),
                    games = g.Select(y => new
                    {
                        gamePoints = y.PlayerGames.FirstOrDefault(z => z.userId == x.Key)?.totalScore
                    }).ToArray()
                }).OrderByDescending(x => x.totalPoints).ToArray()
            } : null;
            var result = new
            {
                games = games,
                tournament = tournament
            };
            return JsonSerializer.Serialize(result);
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

        static string? EliminatedInGameDescription(IReadOnlyCollection<GeoTournament.GameObject> games, string userId)
        {
            return GeoTournament.GetEliminationStatus(games, userId) == EliminationStatus.DidNotPlayGame1
                ? "-"
                : games.FirstOrDefault(g => g.UserIdsEliminated.Contains(userId))?.GameNumber.ToString() ??
                  null;
        }
    }

    public class PlayerInRound
    {
        public string playerName { get; set; } = string.Empty;
        public int roundScoreInPoints { get; set; }
        public decimal distanceInMeters { get; set; }
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }
}