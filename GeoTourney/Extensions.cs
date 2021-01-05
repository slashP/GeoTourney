using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GeoTourney
{
    public static class Extensions
    {
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

        public static string GetVersion()
        {
            var name = typeof(GeoTournament).Assembly.GetName();
            var version = name.Version;
            return $"v.{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        public static string ShortHash(string text)
        {
            using MD5 md5 = MD5.Create();
            byte[] byteHash = md5.ComputeHash(Encoding.UTF8.GetBytes(text)).Skip(12).ToArray();
            string hash = BitConverter.ToString(byteHash).Replace("-", "");
            return hash.ToLower();
        }
    }
}