using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace GeoTourney
{
    public class GoogleSheet
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "GeoTourney";

        public static async Task<string> Create(GeoTournament tournament, string? filenameSourceForCommand)
        {
            UserCredential credential;

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileHelper.Content("credentials.json")));
            string credPath = "google-token";
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true));

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            var myNewSheet = new Spreadsheet
            {
                Properties = new SpreadsheetProperties {Title = $"Geoguessr tournament {tournament.Nickname}"}
            };

            var players = tournament.Games.SelectMany(x => x.PlayerGames).DistinctBy(x => x.userId).Select(x => new
            {
                UserId = x.userId,
                PlayerName = x.playerName
            }).ToList();
            var typesOfDataBackgroundColor = System.Drawing.Color.CornflowerBlue;
            var totalsBackground = System.Drawing.Color.Cornsilk;
            var initialTournamentRows = new[]
            {
                RowData(tournament.CurrentGithubResultsPageUrl, Array.Empty<string>()),
                new RowData()
            };
            var rows = players.SelectMany(x =>
            {
                var games = tournament.Games;
                var playerRow = RowData(x.PlayerName, x.UserId);
                playerRow.Values[0] = SetTextFormat(playerRow.Values[0], true, 16);
                return new[]
                {
                    playerRow,
                    SetBackground(RowData(string.Empty, games.Select(g => g.MapName).ToArray()), System.Drawing.Color.LightGreen),
                    SetBackground(SimpleRow("SCORE"), typesOfDataBackgroundColor),
                    RowData("Round 1", games.Select(g => ValueInRound(g, x.UserId, 0, s => s.roundScoreInPoints)).ToArray()),
                    RowData("Round 2", games.Select(g => ValueInRound(g, x.UserId, 1, s => s.roundScoreInPoints)).ToArray()),
                    RowData("Round 3", games.Select(g => ValueInRound(g, x.UserId, 2, s => s.roundScoreInPoints)).ToArray()),
                    RowData("Round 4", games.Select(g => ValueInRound(g, x.UserId, 3, s => s.roundScoreInPoints)).ToArray()),
                    RowData("Round 5", games.Select(g => ValueInRound(g, x.UserId, 4, s => s.roundScoreInPoints)).ToArray()),
                    SetBackground(RowData("Total score", games.Select(g => PlayerGame(g, x.UserId)?.totalScore).ToArray()), totalsBackground),
                    new RowData(),
                    SetBackground(SimpleRow("DISTANCE"), typesOfDataBackgroundColor),
                    RowData("Round 1", games.Select(g => ValueInRound(g, x.UserId, 0, s => decimal.Round(s.distanceInMeters, 2))).ToArray()),
                    RowData("Round 2", games.Select(g => ValueInRound(g, x.UserId, 1, s => decimal.Round(s.distanceInMeters, 2))).ToArray()),
                    RowData("Round 3", games.Select(g => ValueInRound(g, x.UserId, 2, s => decimal.Round(s.distanceInMeters, 2))).ToArray()),
                    RowData("Round 4", games.Select(g => ValueInRound(g, x.UserId, 3, s => decimal.Round(s.distanceInMeters, 2))).ToArray()),
                    RowData("Round 5", games.Select(g => ValueInRound(g, x.UserId, 4, s => decimal.Round(s.distanceInMeters, 2))).ToArray()),
                    SetBackground(RowData("Total distance", games.Select(g => decimal.Round(PlayerGame(g, x.UserId)?.game.player.totalDistanceInMeters ?? 0, 2)).ToArray()), totalsBackground),
                    new RowData(),
                    SetBackground(SimpleRow("TIME"), typesOfDataBackgroundColor),
                    RowData("Round 1", games.Select(g => ValueInRound(g, x.UserId, 0, s => s.time)).ToArray()),
                    RowData("Round 2", games.Select(g => ValueInRound(g, x.UserId, 1, s => s.time)).ToArray()),
                    RowData("Round 3", games.Select(g => ValueInRound(g, x.UserId, 2, s => s.time)).ToArray()),
                    RowData("Round 4", games.Select(g => ValueInRound(g, x.UserId, 3, s => s.time)).ToArray()),
                    RowData("Round 5", games.Select(g => ValueInRound(g, x.UserId, 4, s => s.time)).ToArray()),
                    SetBackground(RowData("Total time", games.Select(g => PlayerGame(g, x.UserId)?.game.player.totalTime ?? 0).ToArray()), totalsBackground),
                    new RowData(),
                    RowData("Aces", games.Count(g => ValueInRound(g, x.UserId, 3, s => s.roundScoreInPoints) == 5000)),
                    new RowData(),
                    new RowData(),
                };
            });
            var sheet = new Sheet
            {
                Properties = new SheetProperties {Title = Path.GetFileNameWithoutExtension(filenameSourceForCommand) ?? "Sheet1"},
                Data = new List<GridData>
                {
                    new()
                    {
                        RowData = initialTournamentRows.Concat(rows).ToList()
                    }
                }
            };

            myNewSheet.Sheets = new List<Sheet> { sheet };

            var newSheet = await service.Spreadsheets.Create(myNewSheet).ExecuteAsync();
            return newSheet.SpreadsheetUrl;
        }

        static T? ValueInRound<T>(GeoTournament.GameObject game, string userId, int index, Func<GeoTournament.Guess, T> selector)
        {
            var guess = PlayerGame(game, userId)?.game.player.guesses.Skip(index).First();
            return guess is not null ? selector(guess) : default;
        }

        static GeoTournament.PlayerGame? PlayerGame(GeoTournament.GameObject game, string userId) =>
            game.PlayerGames.FirstOrDefault(p => p.userId == userId);

        static RowData RowData<T>(string? header, params T[] values) =>
            header == null
                ? new RowData()
                : new()
                {
                    Values = new[] {CreateCell(header)}.Concat(values.Select(CreateCell)).ToList()
                };

        static RowData SimpleRow(string header) =>
            new()
            {
                Values = new List<CellData>
                {
                    CreateCell(header)
                }
            };

        static CellData CreateCell<T>(T arg) =>
            new()
            {
                UserEnteredValue = new ExtendedValue
                {
                    StringValue = arg is string s ? s : null,
                    NumberValue = arg switch
                    {
                        decimal n => (double?) n,
                        int n => n,
                        string _ => null,
                        null => null,
                        _ => throw new ArgumentException("Only string and number supported here")
                    }
                }
            };

        static CellData SetTextFormat(CellData cell, bool bold, int fontSize)
        {
            cell.UserEnteredFormat = new CellFormat
            {
                TextFormat = new TextFormat
                {
                    Bold = bold,
                    FontSize = fontSize
                }
            };
            return cell;
        }

        static RowData SetBackground(RowData row, System.Drawing.Color color)
        {
            foreach (var cellData in row.Values)
            {
                cellData.UserEnteredFormat = new CellFormat
                {
                    BackgroundColor = new Color
                    {
                        Red = color.R / 255f,
                        Green = color.G / 255f,
                        Blue = color.B / 255f,
                        Alpha = color.A / 255f
                    }
                };
            }

            return row;
        }
    }
}