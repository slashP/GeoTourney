using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GeoTourney
{
    public class TournamentHistory
    {
        public static async Task<GeoTournament?> CreateTournamentFromUrl(string username, string id, string originalUrl)
        {
            var url = $"https://raw.githubusercontent.com/{username}/{username}.github.io/main/geoguessr/{id}.json";
            var client = new HttpClient();
            var data = await client.GetFromJsonAsync<GithubTournamentData>(url);
            if (data == null) return null;
            var tournament = new GeoTournament(data.nickname, data.startTimeUtc)
            {
                CurrentGithubResultsPageUrl = originalUrl
            };
            var playerIds = data.games.SelectMany(g => g.playerGames.Select(x => x.playerId)).Distinct().ToList();
            foreach (var game in data.games)
            {
                tournament.Games.Add(new GeoTournament.GameObject
                {
                    GameNumber = game.gameNumber,
                    GameUrl = game.gameUrl,
                    GameId = game.gameId,
                    MapName = game.mapName,
                    PlayedWithEliminations = game.playedWithEliminations,
                    PlayerGames = game.playerGames.Select((x, playerIndex) => new GeoTournament.PlayerGame
                    {
                        playerName = x.player,
                        userId = x.playerId,
                        totalScore = x.points,
                        game = new GeoTournament.Game
                        {
                            player = new GeoTournament.Player
                            {
                                id = x.playerId,
                                guesses = game.allGuesses.Select(g => new GeoTournament.Guess
                                {
                                    lat = g[playerIndex].lat,
                                    lng = g[playerIndex].lng,
                                    roundScoreInPoints = g[playerIndex].roundScoreInPoints,
                                    distanceInMeters = g[playerIndex].distanceInMeters,
                                    time = g[playerIndex].time
                                }).ToArray(),
                                totalDistanceInMeters = x.totalDistanceInMeters,
                                totalTime = x.totalTime
                            },
                            mapName = game.mapName,
                            rounds = game.answers.Select(a => new GeoTournament.Round
                            {
                                lat = a.lat,
                                lng = a.lng
                            }).ToArray(),
                            forbidMoving = game.forbidMoving,
                            forbidRotating = game.forbidRotating,
                            forbidZooming = game.forbidZooming,
                            timeLimit = game.timeLimit,
                        }
                    }).ToList(),
                    EliminationStatuses = playerIds.ToDictionary(x => x, _ => EliminationStatus.StillInTheGame)
                });
            }

            return tournament;
        }
    }
}