using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace GeoTourney
{
    public static class Extensions
    {
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

        public static string AsString(this IEnumerable<char> characters) => new(characters.ToArray());

        public static string Pluralize(this string text) => text?.Length == 1 ? text : $"{text}s";

        public static string HtmlEncode(this string text) => HttpUtility.HtmlEncode(text);

        public static string GetVersion()
        {
            var version = Version();
            return $"v.{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        public static string GetMajorMinorVersion()
        {
            var version = Version();
            return $"v.{version?.Major}.{version?.Minor}";
        }

        static Version? Version() => typeof(GeoTournament).Assembly.GetName().Version;

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
    }
}