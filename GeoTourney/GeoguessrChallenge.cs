using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney
{
    public class GeoguessrChallenge
    {
        const int DefaultTimeLimit = 60;
        const string PathToUserDefinedMapDefinitions = "maps.txt";
        static Random random = new();

        static readonly Dictionary<string, (string mapId, string group)> Maps = new()
        {
            { "adw", ("59a1514f17631e74145b6f47", "world") },
            { "auw", ("5b3d510b7a2b425ef47b54fd", "world") },
            { "arw", ("5be0de51fe3a84037ca36447", "world") },
            { "aiw", ("5b0a80f8596695b708122809", "world") },
            { "aigen", ("5dbaf08ed0d2a478444d2e8e", "world") },
            { "adaiw", ("5f5a6d3b265fce00018381a5", "world") },
            { "abw", ("5d73f83d82777cb5781464f2", "world") },
            { "aew", ("5cd30a0d17e6fc441ceda867", "world") },
            { "abnm", ("5f3933944b63e60001e85762", "nordic") },
            { "remote", ("5cbe435275345f7b9074bdf8", "world") },

            // Europe
            { "albania", ("5b0dcc661c70126adc543db2", "country") },
            { "andorra", ("5b81ca041ee0107188662a1c", "country") },
            { "austria", ("5b468099a6fd96096470f2b6", "country") },
            { "belgium", ("59ccca2c572f0e4c689c5a28", "country") },
            { "bulgaria", ("5a26e60ef3701413a09784c2", "country") },
            { "croatia", ("5bcf4af2fdd598641cb4c599", "country") },
            { "czechia", ("5a5fc4f3a1835d2820f7443e", "country") },
            { "czech", ("5a5fc4f3a1835d2820f7443e", "country") },
            { "czechrepublic", ("5a5fc4f3a1835d2820f7443e", "country") },
            { "denmark", ("5b4bbe2248b83b0d7ccab676", "country") },
            { "estonia", ("5b185a1159669593c4ce61c7", "country") },
            { "finland", ("5c4c2b5b3d925b4af4b878e2", "country") },
            { "france", ("5971ce86a237ab6bf0b235bf", "country") },
            { "germany", ("5a104ed7d4f53d58e4f6651c", "country") },
            { "greece", ("5b2c126daf2b2a1230e37b0f", "country") },
            { "hungary", ("5c69e11c1554906e04dede26", "country") },
            { "iceland", ("iceland", "country") },
            { "ireland", ("ireland", "country") },
            { "italy", ("5dc15e26d0d2a446b8ed181a", "country") },
            { "latvia", ("5b3058bef65f8d3f044aa78e", "country") },
            { "lithuania", ("583cbd98f0b56c867c523acf", "country") },
            { "luxembourg", ("5bb3a00a2c0173520855355b", "country") },
            { "malta", ("5b1ec9f4faa4cf6dc8731fbf", "country") },
            { "montenegro", ("5b12e7814559f48980b6c65a", "country") },
            { "netherlands", ("5a523bcdf6c9460978be3544", "country") },
            { "holland", ("5a523bcdf6c9460978be3544", "country") },
            { "northmacedonia", ("5b13fd9fff760f24e405f020", "country") },
            { "nmk", ("5b13fd9fff760f24e405f020", "country") },
            { "norway", ("5df50ca9dfc00528f0ebca5d", "country") },
            { "poland", ("5bda4d8d9f90a81f581e503b", "country") },
            { "portugal", ("595901183f3b795344252c0e", "country") },
            { "romania", ("56fe4cec8be4658218780505", "country") },
            { "russia", ("5e1f8e1727ec176794714886", "country") },
            { "serbia", ("5b19d2cc59669593c4ce9355", "country") },
            { "slovakia", ("5b4921d35385271b9ce42aa0", "country") },
            { "slovenia", ("5ae81358b18d088cd07d04f5", "country") },
            { "spain", ("5e09e7ca37a93b0b249b7fbc", "country") },
            { "sweden", ("5e0a81a0328e461c0cb114fc", "country") },
            { "switzerland", ("56e4a542dc7cd6a164dc2be3", "country") },
            { "ukraine", ("5b1f1d9d59669593c4cf1ba1", "country") },
            { "unitedkingdom", ("5ba862d12c0173524cd9327a", "country") },
            { "uk", ("5ba862d12c0173524cd9327a", "country") },

            // Asia
            { "bangladesh", ("5b119671596695ad483a2c7b", "country") },
            { "bhutan", ("5d0cc78c8b19a91fe05aa3b0", "country") },
            { "cambodia", ("5cd188c08701a87fd05639d0", "country") },
            { "indonesia", ("5a7877ec7a437a6fd430a657", "country") },
            { "israel", ("59f061ba72d16b86e034a76d", "country") },
            { "japan", ("59cf49695d2de4db80351e6e", "country") },
            { "jordan", ("5ada43c1582de2514cbb1dcb", "country") },
            { "kyrgyzstan", ("5b58bc1efdcd8a103401800a", "country") },
            { "malaysia", ("5b16d04459669593c4ce2a99", "country") },
            { "mongolia", ("5b7ee7ec4ee03584bc718352", "country") },
            { "philippines", ("5b706f487135fa0e48b57018", "country") },
            { "singapore", ("5b7c7f0d1ee01071886578ad", "country") },
            { "southkorea", ("59cc0d4b56b1c23bc81dee8e", "country") },
            { "srilanka", ("5700295fdc7cd6f788207bc4", "country") },
            { "thailand", ("5a06280a5955f8f64060c24d", "country") },
            { "turkey", ("5a733d9f7a437a4514cd413f", "country") },
            { "unitedarabemirates", ("5fcb4a73ac5db8000177b655", "country") },
            { "uae", ("5fcb4a73ac5db8000177b655", "country") },

            // Americas
            { "argentina", ("5b858a07602b2e3a94fccde3", "country") },
            { "bolivia", ("5b0f151dff760f7f903a7897", "country") },
            { "brazil", ("5e3e9aa427ec178a58bd920b", "country") },
            { "canada", ("5cd9429c93096059b42c8a71", "country") },
            { "chile", ("5ccefe2f17e6fc491cfdd67e", "country") },
            { "colombia", ("5a7597f25eefedbf68abdf30", "country") },
            { "ecuador", ("5b088bf8faa4cf3ce43b3ed9", "country") },
            { "guatemala", ("59d15b2d56b1c23bc81ed2eb", "country") },
            { "mexico", ("5cec6a9219e12f598447c334", "country") },
            { "peru", ("5ccf05f28701a823e429ddeb", "country") },
            { "unitedstates", ("5ab6b56818399e27583294d0", "country") },
            { "us", ("5ab6b56818399e27583294d0", "country") },
            { "usa", ("5ab6b56818399e27583294d0", "country") },
            { "uruguay", ("5b035065ff760f65bce086dd", "country") },

            // Africa
            { "botswana", ("5b01b787ff760f65bce046f1", "country") },
            { "eswatini", ("5b1d2fbd596695bb0ce41ffb", "country") },
            { "ghana", ("5b770fd44ee03584bc708f0e", "country") },
            { "kenya", ("5bb75139a8e5d55294d73cff", "country") },
            { "lesotho", ("5b1ea6c34559f41ad859b7ec", "country") },
            { "nigeria", ("5d318d213b4b6a49685edae1", "country") },
            { "senegal", ("59e5364072d16bfdbc8df37b", "country") },
            { "southafrica", ("5cc224b075345f8dbcdebcdf", "country") },
            { "za", ("5cc224b075345f8dbcdebcdf", "country") },
            { "tunisia", ("5b0453b1ff760f65bce0adf6", "country") },
            { "uganda", ("5b69a0527135fa0e48b4bca5", "country") },
        };

        public static async Task<(string? error, string? link)> Create(Page page, IConfiguration config, string? mapKey, string? timeDescription, string? gameModeDescription)
        {
            var timeLimit = TimeLimit(config, timeDescription);
            var gameMode = gameModeDescription == null
                ? GeoguessrApi.GameMode.NoMove
                : Enum.TryParse<GeoguessrApi.GameMode>(gameModeDescription, true, out var g) ? g : GeoguessrApi.GameMode.Invalid;
            if (gameMode == GeoguessrApi.GameMode.Invalid)
            {
                var validGameModes = Enum.GetValues<GeoguessrApi.GameMode>()
                    .Except(new[] {GeoguessrApi.GameMode.Invalid}).Select(x => x.ToString().ToLower());
                return ($"Game mode {gameModeDescription} not known. One of {string.Join(", ", validGameModes)}", null);
            }

            var mapId = await FindMapId((mapKey ?? string.Empty).ToLower());
            if (mapId == null)
            {
                return ($"Couldn't find map for {mapKey}.", null);
            }

            return await GeoguessrApi.GenerateChallengeLink(page, config, timeLimit, gameMode, mapId);
        }

        static async Task<string?> FindMapId(string mapKey)
        {
            var dict = Maps;
            if (File.Exists(PathToUserDefinedMapDefinitions))
            {
                var lines = await File.ReadAllLinesAsync(PathToUserDefinedMapDefinitions);
                var valid = lines.Where(x => x.Split(' ').Length >= 2).Select(x => new
                {
                    mapKey = x.Split(' ')[0].ToLower(),
                    mapId = x.Split(' ')[1].ToLower(),
                    group = x.Split(' ').Skip(2).FirstOrDefault()?.ToLower()
                }).Where(x => !string.IsNullOrEmpty(x.mapKey) && !string.IsNullOrEmpty(x.mapId)).ToList();
                var userEntriesAsDictionary = valid.ToLookup(x => x.mapKey, x => (x.mapId, (string?)x.group))
                    .ToDictionary(x => x.Key, x => x.First());
                var match = FindMapThatMatches(mapKey, userEntriesAsDictionary, dict);
                return match;
            }

            return FindMapThatMatches(mapKey, new Dictionary<string, (string mapKey, string? mapId)>(), dict);
        }

        static string? FindMapThatMatches(
            string mapKey,
            Dictionary<string, (string mapId, string? group)> userEntriesAsDictionary,
            Dictionary<string, (string mapId, string group)> programEntries)
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
                var selectRandomFrom = userEntriesAsDictionary.Values.Where(selector).Select(x => x.mapId)
                    .Concat(programEntries.Values.Where(selector2).Select(x => x.mapId)).Distinct().ToList();
                return selectRandomFrom.Any() ? selectRandomFrom[random.Next(selectRandomFrom.Count)] : null;
            }
            var match = userEntriesAsDictionary.TryGetValue(mapKey, out var userMap) ? userMap :
                programEntries.TryGetValue(mapKey, out var hardcodedMap) ? hardcodedMap :
                PartialMatch(userEntriesAsDictionary, Maps, mapKey);
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

        static ushort TimeLimit(IConfiguration config, string? timeDescription)
        {
            ushort? timeFromConfig = ushort.TryParse(config["DefaultChallengeTime"], out var n) ? n : null;
            var timeLimit = ushort.TryParse(timeDescription, out var time) ? time : timeFromConfig ?? DefaultTimeLimit;
            return RoundToNearest(timeLimit);
        }

        static ushort RoundToNearest(in ushort timeLimit)
        {
            return (ushort) Math.Min((timeLimit / 10) * 10, 600);
        }
    }
}