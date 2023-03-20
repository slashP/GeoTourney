using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace GeoTourney.Core
{
    public static class BrowserSetup
    {
        public static readonly LaunchOptions LaunchOptions = new()
        {
            DefaultViewport = null,
            SlowMo = 5,
            Headless = false,
            Args = Array.Empty<string>()
        };

        public static async Task Initiate()
        {
            await new BrowserFetcher().DownloadAsync();
        }
    }
}