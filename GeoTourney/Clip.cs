using System;
using System.Threading.Tasks;
using TextCopy;

namespace GeoTourney
{
    public static class Clip
    {
        public static async Task SetText(string text)
        {
            try
            {
                await ClipboardService.SetTextAsync(text);
                Console.WriteLine("Copied to clipboard");
            }
            catch
            {
            }
        }
    }
}