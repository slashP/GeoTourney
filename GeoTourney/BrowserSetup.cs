using System.Threading.Tasks;
using PuppeteerSharp;

namespace GeoTourney
{
    public static class BrowserSetup
    {
        public static readonly LaunchOptions LaunchOptions = new()
        {
            DefaultViewport = new ViewPortOptions { Width = 1600, Height = 1000 },
            SlowMo = 5,
            Headless = false
        };

        public static async Task Initiate()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
        }
    }
}