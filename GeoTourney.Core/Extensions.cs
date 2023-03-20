using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace GeoTourney.Core
{
    public static class Extensions
    {
        public static Version? Version;

        public static int IntFromString(string inputCommand) => int.Parse(inputCommand.Where(char.IsDigit).AsString());

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T> source) => source.Any() ? source : null;

        public static string AsString(this IEnumerable<char> characters) => new(characters.ToArray());

        public static string Pluralize(this string text, int count) => count == 1 ? text : $"{text}s";

        public static string HtmlEncode(this string text) => HttpUtility.HtmlEncode(text);

        public static string ToOnOrOffString(this bool val) => val ? "ON" : "OFF";

        public static string GetVersion()
        {
            return $"v.{Version?.Major}.{Version?.Minor}.{Version?.Build}";
        }

        public static string GetMajorMinorVersion()
        {
            return $"v.{Version?.Major}.{Version?.Minor}";
        }

        public static string ShortHash(string text)
        {
            using MD5 md5 = MD5.Create();
            byte[] byteHash = md5.ComputeHash(Encoding.UTF8.GetBytes(text)).Skip(12).ToArray();
            string hash = BitConverter.ToString(byteHash).Replace("-", "");
            return hash.ToLower();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string? GetFromQueryString(string queryString, string part)
        {
            Dictionary<string, string> rc = new();
            string[] ar1 = queryString.Split(new[] { '&', '?' });
            foreach (string row in ar1)
            {
                if (string.IsNullOrEmpty(row)) continue;
                var index = row.IndexOf('=');
                if (index < 0) continue;
                rc[Uri.UnescapeDataString(row.Substring(0, index))] = Uri.UnescapeDataString(row.Substring(index + 1));
            }

            return rc.TryGetValue(part, out var value) ? value : null;
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}