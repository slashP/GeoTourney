using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoTourney
{
    public class AppDataProvider
    {
        const string Filepath = "appdata.json";

        public static async Task BanUser(string userId)
        {
            try
            {
                var appData = await ReadCurrentAppData();
                appData.Bans = appData.Bans.Concat(new[]
                {
                    new Ban
                    {
                        UserId = userId
                    }
                }).DistinctBy(x => x.UserId).ToArray();
                await WriteAppData(appData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task UnbanUser(string userId)
        {
            try
            {
                var appData = await ReadCurrentAppData();
                appData.Bans = appData.Bans.Where(x => x.UserId != userId).ToArray();
                await WriteAppData(appData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task<IReadOnlyCollection<string>> BannedUsersIds()
        {
            try
            {
                var appData = await ReadCurrentAppData();
                return appData.Bans.Select(x => x.UserId).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Array.Empty<string>();
        }

        static async Task<AppData> ReadCurrentAppData()
        {
            await CreateIfNotExists();
            return await ReadAppData();
        }

        static async Task<AppData> ReadAppData()
        {
            var fileContent = await File.ReadAllTextAsync(Filepath);
            return JsonSerializer.Deserialize<AppData>(fileContent) ?? new();
        }

        static async Task CreateIfNotExists()
        {
            if (!File.Exists(Filepath))
            {
                await WriteAppData(new AppData());
            }
        }

        static async Task WriteAppData(AppData appData) =>
            await File.WriteAllTextAsync(Filepath, JsonSerializer.Serialize(appData, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

        record AppData
        {
            public IReadOnlyCollection<Ban> Bans { get; set; } = Array.Empty<Ban>();
        }

        record Ban
        {
            public string UserId { get; init; } = string.Empty;
        }
    }
}