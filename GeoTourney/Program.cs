using System;
using System.IO;
using System.Text.RegularExpressions;
using GeoTourney.Core;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using Extensions = GeoTourney.Core.Extensions;

Extensions.Version = typeof(Here).Assembly.GetName().Version;
await BrowserSetup.Initiate();
var browser = await Puppeteer.LaunchAsync(BrowserSetup.LaunchOptions);
var page = await browser.NewPageAsync();
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").Build();
Regex readCommandFromFileRegex = new(@"^read ([^<>:;,?""*|\/]+)$");

await Startup.InitiateAsync(config, page);

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

class Here { }