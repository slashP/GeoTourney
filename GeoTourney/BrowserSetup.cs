using System.Threading.Tasks;
using PuppeteerSharp;

namespace GeoTourney
{
    public static class BrowserSetup
    {
        public static readonly LaunchOptions LaunchOptions = new()
        {
            DefaultViewport = new ViewPortOptions { Width = 1000, Height = 800 },
            SlowMo = 5,
            Headless = false,
            Args = new[] { "--start-maximized" }
        };

        public static async Task Initiate()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
        }
    }
}