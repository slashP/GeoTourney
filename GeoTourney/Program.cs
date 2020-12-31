using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GeoTourney;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

/// <summary>
/// https://www.geoguessr.com/challenge/4AIgheuBiawHesVD
/// https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr
/// </summary>
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
    return;
}

await BrowserSetup.Initiate();
var browser = await Puppeteer.LaunchAsync(BrowserSetup.LaunchOptions);
var page = await browser.NewPageAsync();
await page.GoToAsync("https://www.geoguessr.com/signin");
GeoTournament.PrintCommands();
Console.WriteLine();
while (true)
{
    var inputCommand = ReadConsole.ReadLine(TimeSpan.FromSeconds(10));
    if (inputCommand?.Equals("exit", StringComparison.OrdinalIgnoreCase) ?? false)
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

        var result = await tournament.CheckIfCurrentGameFinished(page, config);
        if (result.finished) WriteOutput(activeOutputs, $"Tournament results here: {result.gistUrl}");
    }
    else if (Uri.TryCreate(inputCommand, UriKind.Absolute, out var uri))
    {
        tournament.SetCurrentGame(uri);
        var currentGameNumber = (tournament.Games.OrderByDescending(x => x.GameNumber).FirstOrDefault()?.GameNumber ?? 0) + 1;
        WriteOutput(activeOutputs, $"Game #{currentGameNumber}: {uri}");
    }
    else if (inputCommand == "restart")
    {
        tournament = new GeoTournament();
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
}

static void OnMessageReceived(object? sender, string e)
{
    if (e?.StartsWith("https://www.geoguessr.com/challenge") ?? false)
        ReadConsole.QueueCommand(e);
    else if (e?.StartsWith("!") ?? false) ReadConsole.QueueCommand(new string(e.Skip(1).ToArray()));
}

static void WriteOutput(IEnumerable<IGameEventOutput> activeOutputs, string message)
{
    foreach (var output in activeOutputs) output.Write(message);
}