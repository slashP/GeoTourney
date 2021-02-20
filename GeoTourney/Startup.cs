using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public static class Startup
    {
        public static async Task InitiateAsync(IConfiguration config, Page page)
        {
            var name = typeof(GeoTournament).Assembly.GetName();
            Console.WriteLine($"Starting {name.Name} {GeoTourney.Extensions.GetVersion()}");
            Console.OutputEncoding = Encoding.Unicode;
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
                var url = await Github.UploadTournamentData(config, JsonSerializer.Deserialize<GithubTournamentData>(await File.ReadAllTextAsync(localExampleTournamentPath))!);
                await Clip.SetText(url, "Copied to clipboard");
            }

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
        }
    }
}