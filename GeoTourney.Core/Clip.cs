using System;
using System.Threading.Tasks;
using TextCopy;

namespace GeoTourney.Core
{
    public static class Clip
    {
        public static async Task SetText(string text, string logDescription)
        {
            try
            {
                await ClipboardService.SetTextAsync(text);
                Console.WriteLine(logDescription);
            }
            catch
            {
            }
        }
    }
}