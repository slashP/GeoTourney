using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoTourney;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

var name = typeof(GeoTournament).Assembly.GetName();
Console.WriteLine($"Starting {name.Name} {GeoTourney.Extensions.GetVersion()}");
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").Build();
Console.OutputEncoding = Encoding.Unicode;
Regex readCommandFromFileRegex = new(@"^read ([^<>:;,?""*|\/]+)$");
await CommandHandler.Initialize(config);
var gihubAccess = await Github.VerifyGithubTokenAccess(config);
if (!gihubAccess.hasAccess)
{
    Console.WriteLine(gihubAccess.errorMessage);
    Console.ReadKey();
    return;
}

await Github.CreateOrUpdateTemplates(config);

var localExampleTournamentPath = config["LocalExampleTournamentPath"];
if (!string.IsNullOrEmpty(localExampleTournamentPath) && File.Exists(localExampleTournamentPath))
{
    var url = await Github.UploadTournamentData(config, JsonSerializer.Deserialize<GithubTournamentData>(await File.ReadAllTextAsync(localExampleTournamentPath))!, false);
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
    var inputCommand = ReadConsole.ReadLine(TimeSpan.FromSeconds(10));
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

    if (inputCommand != null && readCommandFromFileRegex.IsMatch(inputCommand))
    {
        var filename = readCommandFromFileRegex.Matches(inputCommand)[0].Groups[1].Value;
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Could not find file {filename}");
            continue;
        }

        var lines = await File.ReadAllLinesAsync(filename);
        foreach (var line in lines)
        {
            await CommandHandler.Handle(page, config, line, commandType, filename);
        }
    }
    else
    {
        await CommandHandler.Handle(page, config, inputCommand, commandType, null);
    }
}
