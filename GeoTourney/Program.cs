using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeoTourney;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using Extensions = GeoTourney.Extensions;

// https://www.geoguessr.com/challenge/4AIgheuBiawHesVD
// https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr
Regex lessOrMoreThanRegex = new(@"^(elim|revive) (less|more) (than|then) (\d{1,5}?)");
Regex lessOrMoreThanFinishGameRegex = new(@"^(less|more) (than|then) (\d{1,5}?)");
var name = typeof(GeoTournament).Assembly.GetName();
Console.WriteLine($"Starting {name.Name} {GeoTourney.Extensions.GetVersion()}");
GeoTournament tournament = new();
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").Build();
Console.OutputEncoding = Encoding.Unicode;
IGameEventOutput[] possibleOutputs =
{
    new TwitchClient(),
    new ConsoleOutput(),
    new FileOutput()
};
var allOutputs = possibleOutputs.Select(x => new
{
    Status = x.Initialize(config, OnMessageReceived),
    Output = x
}).ToList();
var activeOutputs = allOutputs.Where(x => x.Status == InitializationStatus.Ok).Select(x => x.Output).ToList();
var gihubAccess = await Github.VerifyGithubTokenAccess(config);
if (!gihubAccess.hasAccess)
{
    Console.WriteLine(gihubAccess.errorMessage);
    Console.ReadKey();
    return;
}

var localExampleTournamentPath = config["LocalExampleTournamentPath"];
if (!string.IsNullOrEmpty(localExampleTournamentPath) && File.Exists(localExampleTournamentPath))
{
    var url = await Github.UploadTournamentData(config, await File.ReadAllTextAsync(localExampleTournamentPath));
    await Clip.SetText(url, "Copied to clipboard");
}

await BrowserSetup.Initiate();
var browser = await Puppeteer.LaunchAsync(BrowserSetup.LaunchOptions);
var page = await browser.NewPageAsync();
if (await GeoguessrApi.TrySignInFromLocalFile(page))
{
    await page.GoToAsync("https://www.geoguessr.com/me/profile");
}
else
{
    await page.GoToAsync("https://www.geoguessr.com/signin");
}

GeoTournament.PrintCommands();
Console.WriteLine();

while (true)
{
    string? inputCommand = null;
    try
    {
        inputCommand = ReadConsole.ReadLine(TimeSpan.FromSeconds(10));
        var commandType = inputCommand?.StartsWith("!") ?? false ? CommandType.DamnIt : CommandType.Normal;
        if (inputCommand?.Equals("shutdown", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            try
            {
                await page.CloseAsync();
                await browser.CloseAsync();
            }
            catch (Exception)
            {
                // Can't error on shutdown.
            }

            break;
        }

        if (inputCommand == null)
        {
            foreach (var output in activeOutputs) output.KeepAlive();
        }
        else if (Uri.TryCreate(inputCommand, UriKind.Absolute, out var uri) || Uri.TryCreate(inputCommand.Skip(1).AsString(), UriKind.Absolute, out uri))
        {
            if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt && !tournament.IsCurrentGameSameAs(uri))
            {
                var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !{inputCommand} to ignore.";
                WriteOutput(activeOutputs, message);
                continue;
            }

            var messageToChat = await tournament.SetCurrentGame(uri, page, config);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand == "restart")
        {
            tournament = tournament.Restart();
        }
        else if (inputCommand == "gamescore")
        {
            var url = await tournament.PrintGameScore(config);
            WriteOutput(activeOutputs, $"Last game results: {url}");
        }
        else if (inputCommand == "totalscore")
        {
            var url = await tournament.PrintTotalScore(config);
            WriteOutput(activeOutputs, $"Tournament results: {url}");
        }
        else if (inputCommand == "endgame")
        {
            var messageToChat = await tournament.CheckIfCurrentGameFinished(page, config);
            if (messageToChat != null)
            {
                WriteOutput(activeOutputs, messageToChat);
            }
        }
        else if (inputCommand == "elim")
        {
            var message = await tournament.ToggleEliminations(page, config);
            WriteOutput(activeOutputs, $"Eliminations are now {(tournament.PlayWithEliminations ? "ON" : "OFF")}.");
            await Task.Delay(TimeSpan.FromSeconds(2));
            if(message != null) WriteOutput(activeOutputs, message);
        }
        else if (int.TryParse(inputCommand, out var number) && number >= 0)
        {
            var messageToChat = await tournament.EliminateAndFinish(page, config, number);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (lessOrMoreThanRegex.IsMatch(inputCommand))
        {
            var isEliminationAction = inputCommand.Contains("elim", StringComparison.InvariantCultureIgnoreCase);
            var points = Extensions.IntFromString(inputCommand);
            var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
            var messageToChat = isEliminationAction
                ? await tournament.EliminatePlayers(pointsDescription, points, config)
                : await tournament.RevivePlayers(pointsDescription, points, config);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand.StartsWith("elim "))
        {
            var playerSearchTerm = inputCommand.Skip("elim ".Length).AsString();
            var messageToChat = await tournament.EliminateSpecificPlayer(playerSearchTerm, config);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand.StartsWith("revive "))
        {
            var playerSearchTerm = inputCommand.Skip("revive ".Length).AsString();
            var messageToChat = await tournament.ReviveSpecificPlayer(playerSearchTerm, config);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (lessOrMoreThanFinishGameRegex.IsMatch(inputCommand))
        {
            var points = Extensions.IntFromString(inputCommand);
            var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
            var messageToChat = await tournament.EliminateAndFinish(page, pointsDescription, points, config);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand.StartsWith("game") || inputCommand.StartsWith("!game"))
        {
            if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt)
            {
                var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !!{inputCommand} to ignore.";
                WriteOutput(activeOutputs, message);
                continue;
            }

            var parts = inputCommand.Split(' ');
            var mapKey = parts.Skip(1).FirstOrDefault();
            var timeDescription = parts.Skip(2).FirstOrDefault();
            var gameModeDescription = parts.Skip(3).FirstOrDefault();
            var (error, link) = await GeoguessrChallenge.Create(page, config, mapKey, timeDescription, gameModeDescription);
            if (error != null)
            {
                WriteOutput(activeOutputs, error);
            }

            if (link != null)
            {
                var messageToChat = await tournament.SetCurrentGame(new Uri(link), page, config);
                if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
            }
        }
        else if (inputCommand == "maps")
        {
            var maps = await GeoguessrChallenge.GetMaps();
            var messageToChat = await Github.UploadMaps(config, maps);
            if (messageToChat != null) WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand == "apiinfo")
        {
            WriteOutput(activeOutputs, GeoguessrApi.ApiCallsInfo());
        }
        else if (inputCommand == "currentgame")
        {
            var messageToChat = tournament.CurrentGameUrl() ?? "No game running.";
            WriteOutput(activeOutputs, messageToChat);
        }
        else if (inputCommand.StartsWith("loadgamefrom"))
        {
            var t = await TournamentHistory.CreateTournamentFromUrl(inputCommand);
            if (t != null)
            {
                tournament = t;
                WriteOutput(activeOutputs, $"Loaded tournament with {t.Games.Count} games.");
            }
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
                WriteOutput(activeOutputs, "Looks like you found a bug. That did not work as expected.");
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
    if (message.StartsWith(GeoguessrApi.ChallengeUrlPrefix) || message.StartsWith($"!{GeoguessrApi.ChallengeUrlPrefix}"))
        ReadConsole.QueueCommand(message);
    else if (message.StartsWith("!")) ReadConsole.QueueCommand(message.Skip(1).AsString());
    else if(int.TryParse(message, out var number) && number >= 0) ReadConsole.QueueCommand(number.ToString());
}

static void WriteOutput(IEnumerable<IGameEventOutput> activeOutputs, string message)
{
    foreach (var output in activeOutputs) output.Write(message);
}