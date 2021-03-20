using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney.Core
{
    public class GeoguessrChallenge
    {
        const int DefaultTimeLimit = 60;
        const string PathToUserDefinedMapDefinitions = "maps.txt";
        static Random random = new();

        public static async Task<(string? error, string? gameId, string? mapId)> Create(
            Page page,
            IConfiguration config,
            string? mapKey,
            string? timeDescription,
            string? gameModeDescription,
            IReadOnlyCollection<string> mapIdsPlayed)
        {
            var timeLimit = TimeLimit(config, timeDescription);
            var gameMode = gameModeDescription == null
                ? GeoguessrApi.GameMode.NoMove
                : Enum.TryParse<GeoguessrApi.GameMode>(gameModeDescription, true, out var g) ? g : GeoguessrApi.GameMode.Invalid;
            if (gameMode == GeoguessrApi.GameMode.Invalid)
            {
                var validGameModes = Enum.GetValues<GeoguessrApi.GameMode>()
                    .Except(new[] {GeoguessrApi.GameMode.Invalid}).Select(x => x.ToString().ToLower());
                return ($"Game mode {gameModeDescription} not known. One of {string.Join(", ", validGameModes)}", null, null);
            }

            var mapId = await FindMapId((mapKey ?? string.Empty).ToLower(), mapIdsPlayed);
            if (mapId == null)
            {
                return ($"Couldn't find map for {mapKey}.", null, null);
            }

            return await GeoguessrApi.GenerateChallengeLink(page, config, timeLimit, gameMode, mapId);
        }

        public static async Task<IReadOnlyCollection<GeoguessrMap>> GetMaps()
        {
            var maps = ReadMapsFromFile();
            var userMaps = (await GetUserDefinedMaps()).Select(x => new GeoguessrMap
            {
                mapId = x.MapId.HtmlEncode(),
                @group = (x.Group ?? string.Empty).HtmlEncode(),
                shortcut = x.MapKey.HtmlEncode(),
                mapCreator = string.Empty,
                mapName = x.MapKey.HtmlEncode()
            }).GroupBy(x => x.mapId).Select(x => x.First()).ToList();
            return userMaps.Concat(maps).ToList();
        }

        static async Task<string?> FindMapId(string mapKey, IReadOnlyCollection<string> mapIdsPlayed)
        {
            var userDefinedMaps = await GetUserDefinedMaps();
            var userEntriesAsDictionary = userDefinedMaps.ToLookup(x => x.MapKey, x => (mapId: x.MapId, (string?)x.Group))
                .ToDictionary(x => x.Key, x => x.First());
            var match = FindMapThatMatches(mapKey, userEntriesAsDictionary, ProgramMaps(), mapIdsPlayed);
            return match;
        }

        static async Task<IReadOnlyCollection<MapCommand>> GetUserDefinedMaps()
        {
            if (!File.Exists(PathToUserDefinedMapDefinitions)) return Array.Empty<MapCommand>();
            var lines = await File.ReadAllLinesAsync(PathToUserDefinedMapDefinitions);
            var valid = lines.Where(x => x.Split(' ').Length >= 2).Select(x => new MapCommand
            {
                MapKey = x.Split(' ')[0].ToLower(),
                MapId = x.Split(' ')[1].ToLower(),
                Group = x.Split(' ').Skip(2).FirstOrDefault()?.ToLower()
            }).Where(x => !string.IsNullOrEmpty(x.MapKey) && !string.IsNullOrEmpty(x.MapId)).ToList();
            return valid;
        }

        static string? FindMapThatMatches(string mapKey,
            Dictionary<string, (string mapId, string? @group)> userEntriesAsDictionary,
            Dictionary<string, (string mapId, string @group)> programEntries,
            IReadOnlyCollection<string> mapIdsPlayed)
        {
            if (mapKey.StartsWith("random"))
            {
                var takeFromGroup = mapKey.Skip("random".Length).AsString();
                Func<(string mapId, string? group), bool> selector = string.IsNullOrEmpty(takeFromGroup)
                    ? _ => true
                    : tuple => tuple.group == takeFromGroup;
                Func<(string mapId, string group), bool> selector2 = string.IsNullOrEmpty(takeFromGroup)
                    ? _ => true
                    : tuple => tuple.group == takeFromGroup;
                var randomCandidates = userEntriesAsDictionary.Values.Where(selector).Select(x => x.mapId)
                    .Concat(programEntries.Values.Where(selector2).Select(x => x.mapId)).Distinct().ToList();
                var selectRandomFrom = randomCandidates.Except(mapIdsPlayed).NullIfEmpty()?.ToList() ?? randomCandidates;
                return selectRandomFrom.Any() ? selectRandomFrom[random.Next(selectRandomFrom.Count)] : null;
            }
            var match = userEntriesAsDictionary.TryGetValue(mapKey, out var userMap) ? userMap :
                programEntries.TryGetValue(mapKey, out var hardcodedMap) ? hardcodedMap :
                PartialMatch(userEntriesAsDictionary, ProgramMaps(), mapKey);
            return match?.mapId;
        }

        static (string mapId, string? group)? PartialMatch(Dictionary<string, (string mapId, string? group)> userMaps, Dictionary<string, (string mapId, string group)> programMaps, string mapKey)
        {
            var userMatches = userMaps.Where(x => x.Key.Contains(mapKey)).ToList();
            if (userMatches.Count == 1)
            {
                return userMatches.First().Value;
            }

            var programMatches = programMaps.Where(x => x.Key.Contains(mapKey)).ToList();
            if (programMatches.Count == 1)
            {
                return programMatches.First().Value;
            }

            return null;
        }

        static Dictionary<string, (string mapId, string group)> ProgramMaps()
        {
            var maps =
                ReadMapsFromFile();
            return maps.GroupBy(x => x.shortcut).ToDictionary(x => x.Key, x => (x.First().mapId, x.First().group));
        }

        static IReadOnlyCollection<GeoguessrMap> ReadMapsFromFile()
        {
            var continents = new[] {"europe", "africa", "america", "asia", "oceania"};
            return (JsonSerializer.Deserialize<IReadOnlyCollection<GeoguessrMap>>(
                        EmbeddedFileHelper.Content("maps.json")) ??
                    Array.Empty<GeoguessrMap>())
                .OrderByDescending(x => x.group == "world")
                .ThenByDescending(x => continents.Contains(x.group)).ToList();
        }

        static ushort TimeLimit(IConfiguration config, string? timeDescription)
        {
            ushort? timeFromConfig = ushort.TryParse(config["DefaultChallengeTime"], out var n) ? n : null;
            var timeLimit = ushort.TryParse(timeDescription, out var time) ? time : timeFromConfig ?? DefaultTimeLimit;
            return timeLimit;
        }
    }

    public record MapCommand
    {
        public string MapKey { get; set; } = string.Empty;
        public string MapId { get; set; } = string.Empty;
        public string? Group { get; set; }
    }

    public record GeoguessrMap
    {
        public string shortcut { get; set; } = string.Empty;
        public string @group { get; set; } = string.Empty;
        public string mapId { get; set; } = string.Empty;
        public string mapName { get; set; } = string.Empty;
        public string mapCreator { get; set; } = string.Empty;
    }
}