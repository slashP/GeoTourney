using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public static class CommandHandler
    {
        // https://www.geoguessr.com/challenge/4AIgheuBiawHesVD
        // https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr
        static readonly Regex LessOrMoreThanRegex = new(@"^(elim|revive) (less|more) (than|then) (\d{1,5}?)");
        static readonly Regex LessOrMoreThanFinishGameRegex = new(@"^(less|more) (than|then) (\d{1,5}?)");
        static readonly Regex ResultsUrlRegex = new(@"^[!]?https:\/\/www.geoguessr.com\/results\/([a-zA-Z0-9_.-]*)[\/]?$");
        static readonly Regex ChallengeUrlRegex = new(@"^[!]?https:\/\/www.geoguessr.com\/challenge\/([a-zA-Z0-9_.-]*)[\/]?$");
        static readonly Regex LoadTournamentFromUrlRegex = new(@"^loadtournamentfrom https:\/\/([\a-z]*).github.io\/");

        static readonly IGameEventOutput[] GameEventOutputs =
        {
            new TwitchClient(),
            new ConsoleOutput(),
            new FileOutput()
        };

        static List<IGameEventOutput> activeOutputs = new();

        static GeoTournament tournament = new("init", DateTime.UtcNow);

        public static async Task Initialize(IConfiguration config)
        {
            var allOutputs = GameEventOutputs.Select(x => new
            {
                Status = x.Initialize(config, OnMessageReceived),
                Output = x
            }).ToList();
            activeOutputs = allOutputs.Where(x => x.Status == InitializationStatus.Ok).Select(x => x.Output).ToList();
            tournament = new(await NameGenerator.New(config), DateTime.UtcNow);
        }

        public static async Task Handle(Page page, IConfiguration config, string? command, CommandType commandType, string? filenameSourceForCommand)
        {
            string? inputCommand = null;
            try
            {
                inputCommand = command;
                if (inputCommand == null)
                {
                    foreach (var output in activeOutputs) output.KeepAlive();
                }
                else if (ChallengeUrlRegex.IsMatch(inputCommand))
                {
                    var gameId = ChallengeUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt && !tournament.IsCurrentGameSameAs(gameId))
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !{inputCommand} to ignore.";
                        WriteOutput(message);
                        return;
                    }

                    var messageToChat = await tournament.SetCurrentGame(gameId, page, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (ResultsUrlRegex.IsMatch(inputCommand))
                {
                    var gameId = ResultsUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt && !tournament.IsCurrentGameSameAs(gameId))
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !{inputCommand} to ignore.";
                        WriteOutput(message);
                        return;
                    }

                    await tournament.SetCurrentGame(gameId, page, config);
                    var messageToChat = await tournament.CheckIfCurrentGameFinished(page, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (inputCommand == "gamescore")
                {
                    var url = await tournament.PrintGameScore(config);
                    WriteOutput($"Last game results: {url}");
                }
                else if (inputCommand == "totalscore")
                {
                    var url = await tournament.PrintTotalScore(config);
                    WriteOutput($"Tournament results: {url}");
                }
                else if (inputCommand == "endgame")
                {
                    var messageToChat = await tournament.CheckIfCurrentGameFinished(page, config);
                    if (messageToChat != null)
                    {
                        WriteOutput(messageToChat);
                    }
                }
                else if (inputCommand == "elim")
                {
                    var message = await tournament.ToggleEliminations(page, config);
                    WriteOutput($"Eliminations are now {tournament.PlayWithEliminations.ToOnOrOffString()}.");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    if (message != null) WriteOutput(message);
                }
                else if (inputCommand == "restart")
                {
                    tournament = tournament.Restart(await NameGenerator.New(config));
                }
                else if (int.TryParse(inputCommand, out var number) && number >= 0)
                {
                    var messageToChat = await tournament.EliminateAndFinish(page, config, number);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (LessOrMoreThanRegex.IsMatch(inputCommand))
                {
                    var isEliminationAction = inputCommand.Contains("elim", StringComparison.InvariantCultureIgnoreCase);
                    var points = Extensions.IntFromString(inputCommand);
                    var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
                    var messageToChat = isEliminationAction
                        ? await tournament.EliminatePlayers(pointsDescription, points, config)
                        : await tournament.RevivePlayers(pointsDescription, points, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (inputCommand.StartsWith("elim "))
                {
                    var playerSearchTerm = inputCommand.Skip("elim ".Length).AsString();
                    var messageToChat = await tournament.EliminateSpecificPlayer(playerSearchTerm, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (inputCommand.StartsWith("revive "))
                {
                    var playerSearchTerm = inputCommand.Skip("revive ".Length).AsString();
                    var messageToChat = await tournament.ReviveSpecificPlayer(playerSearchTerm, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (LessOrMoreThanFinishGameRegex.IsMatch(inputCommand))
                {
                    var points = Extensions.IntFromString(inputCommand);
                    var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
                    var messageToChat = await tournament.EliminateAndFinish(page, pointsDescription, points, config);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (inputCommand.StartsWith("game") || inputCommand.StartsWith("!game"))
                {
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt)
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !!{inputCommand} to ignore.";
                        WriteOutput(message);
                        return;
                    }

                    var parts = inputCommand.Split(' ');
                    var mapKey = parts.Skip(1).FirstOrDefault();
                    var timeDescription = parts.Skip(2).FirstOrDefault();
                    var gameModeDescription = parts.Skip(3).FirstOrDefault();
                    var (error, gameId) = await GeoguessrChallenge.Create(page, config, mapKey, timeDescription, gameModeDescription);
                    if (error != null)
                    {
                        WriteOutput(error);
                    }

                    if (gameId != null)
                    {
                        var messageToChat = await tournament.SetCurrentGame(gameId, page, config);
                        if (messageToChat != null) WriteOutput(messageToChat);
                    }
                }
                else if (inputCommand == "maps")
                {
                    var maps = await GeoguessrChallenge.GetMaps();
                    var messageToChat = await Github.UploadMaps(config, maps);
                    if (messageToChat != null) WriteOutput(messageToChat);
                }
                else if (inputCommand == "apiinfo")
                {
                    WriteOutput(GeoguessrApi.ApiCallsInfo());
                }
                else if (inputCommand == "currentgame")
                {
                    var messageToChat = tournament.CurrentGameUrl() ?? "No game running.";
                    WriteOutput(messageToChat);
                }
                else if (LoadTournamentFromUrlRegex.IsMatch(inputCommand))
                {
                    var username = LoadTournamentFromUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    var url = inputCommand.Split(' ').Skip(1).First();
                    var id = Extensions.GetFromQueryString(new Uri(url).Query, "id");
                    if (id == null)
                    {
                        WriteOutput($"Invalid URL. Missing ?id= | {url}");
                        return;
                    }

                    var t = await TournamentHistory.CreateTournamentFromUrl(username, id, url);
                    if (t != null)
                    {
                        tournament = t;
                        WriteOutput($"Loaded tournament with {t.Games.Count} games.");
                    }
                }
                else if (inputCommand.StartsWith("google-sheet"))
                {
                    var url = await GoogleSheet.Create(tournament, filenameSourceForCommand);
                    await Clip.SetText(url, "URL to Google sheet copied to clipboard");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("An unexpected error occurred.");
                try
                {
                    await File.AppendAllTextAsync("errors.txt", $"{DateTime.UtcNow:s}: {GeoTourney.Extensions.GetVersion()}{Environment.NewLine}{e}{Environment.NewLine}");
                    if (inputCommand != null)
                    {
                        WriteOutput("Looks like you found a bug. That did not work as expected.");
                    }
                }
                catch (Exception second)
                {
                    Console.WriteLine("Woha. Everything fails now?");
                    Console.WriteLine(second);
                }
            }
        }

        static void OnMessageReceived(object? sender, string? e)
        {
            var message = e ?? string.Empty;
            if (ChallengeUrlRegex.IsMatch(message) || ResultsUrlRegex.IsMatch(message))
                ReadConsole.QueueCommand(message);
            else if (message.StartsWith("!")) ReadConsole.QueueCommand(message.Skip(1).AsString());
            else if (int.TryParse(message, out var number) && number >= 0) ReadConsole.QueueCommand(number.ToString());
        }

        static void WriteOutput(string message)
        {
            foreach (var output in activeOutputs) output.Write(message);
        }
    }
}