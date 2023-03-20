using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core;

public class Config
{
    public static string? Read(string section, string key)
    {
        var value = GetConfig()
            .GetSection(section)[key];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool SectionExists(string section) => GetConfig().GetSection(section).Exists();

    public static bool IsDebug() => Convert.ToBoolean(GetConfig()["Debug"]);

    private static IConfigurationRoot GetConfig()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").Build();
    }
}
