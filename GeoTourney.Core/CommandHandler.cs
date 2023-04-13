using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using TwitchLib.Client.Enums;

namespace GeoTourney.Core
{
    public static class CommandHandler
    {
        // https://www.geoguessr.com/challenge/4AIgheuBiawHesVD
        // https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr
        static readonly Regex LessOrMoreThanRegex = new(@"^(elim|revive) (less|more) (than|then) (\d{1,5}?)");
        static readonly Regex LessOrMoreThanFinishGameRegex = new(@"^(less|more) (than|then) (\d{1,5}?)");
        public static readonly Regex ResultsUrlRegex = new(@"^[!]?https:\/\/www.geoguessr.com\/results\/([a-zA-Z0-9_.-]*)[\/]?$");
        static readonly Regex ChallengeUrlRegex = new(@"^[!]?https:\/\/www.geoguessr.com\/challenge\/([a-zA-Z0-9_.-]*)[\/]?$");
        static readonly Regex LoadTournamentFromUrlRegex = new(@"^loadtournamentfrom https:\/\/([\a-z]*).github.io\/");
        static readonly Regex BanByUrlRegex = new(@"^ban https:\/\/www.geoguessr.com\/user\/([a-zA-Z0-9_.-]*)[\/]?$");
        static readonly Regex UnbanByUrlRegex = new(@"^unban https:\/\/www.geoguessr.com\/user\/([a-zA-Z0-9_.-]*)[\/]?$");

        static readonly IGameEventOutput[] HardCodedGameEventOutputs =
        {
            new TwitchClient(),
            new ConsoleOutput(),
            new FileOutput(),
            new DiscordClient()
        };

        static List<IGameEventOutput> activeOutputs = new();

        static GeoTournament tournament = new("init", DateTime.UtcNow);
        static DateTime lastKnownHotkeyFileWriteTimeUtc = DateTime.UtcNow;
        private const string CountdownTournamentFilename = "countdown-tournament.txt";
        private const string CountdownProgressFilename = "countdown-progress.txt";
        private const string SubscriptionsFilename = "subscriptions.txt";
        private const string BitsFilename = "bits.txt";

        public static async Task Initialize(IConfiguration config)
        {
            foreach (var gameEventOutput in HardCodedGameEventOutputs)
            {
                var status = await gameEventOutput.Initialize(config, OnMessageReceived);
                if (status == InitializationStatus.Ok)
                {
                    activeOutputs.Add(gameEventOutput);
                }
            }
            tournament = new(await NameGenerator.New(config), DateTime.UtcNow);
            if (File.Exists(CountdownTournamentFilename) && Config.SectionExists("Countdown"))
            {
                var command = $"loadtournamentfrom {(await File.ReadAllLinesAsync(CountdownTournamentFilename)).FirstOrDefault()}";
                var t = await GetTournamentFromCommandWithUrl(command);
                if (t != null)
                {
                    tournament = t;
                }
            }
        }

        public static async Task<string?> Handle(IPage page, IConfiguration config, string? command, CommandType commandType, string? filenameSourceForCommand)
        {
            string? inputCommand = null;
            try
            {
                inputCommand = command;
                if (inputCommand == null)
                {
                    foreach (var output in activeOutputs) await output.KeepAlive();
                    var hotkeyFile = Config.Read("Countdown", "HotKeyFile");
                    if (File.Exists(hotkeyFile) && new FileInfo(hotkeyFile).LastWriteTimeUtc > lastKnownHotkeyFileWriteTimeUtc)
                    {
                        lastKnownHotkeyFileWriteTimeUtc = DateTime.UtcNow;
                        var (_, endGameError) = await tournament.CheckIfCurrentGameFinished(page, config);
                        if (endGameError != null)
                        {
                            var message = $"{endGameError} Link: {tournament.CurrentGameUrl()}";
                            await WriteOutput(message);
                            return message;
                        }

                        await File.WriteAllTextAsync(CountdownTournamentFilename, tournament.CurrentGithubResultsPageUrl);
                        var defaultMap = Config.Read("Countdown", "DefaultMap");
                        var map = string.IsNullOrEmpty(defaultMap) ? "aaw" : defaultMap;
                        var defaultTime = Config.Read("Countdown", "DefaultTime");
                        var time = string.IsNullOrEmpty(defaultTime) ? "30" : defaultTime;
                        var defaultGameMode = Config.Read("Countdown", "DefaultGameMode");
                        var gameMode = string.IsNullOrEmpty(defaultGameMode) ? "30" : defaultGameMode;
                        var (error, gameId, mapId) = await GeoguessrChallenge.Create(page, config, map, time, gameMode, Array.Empty<string>());
                        if (error != null)
                        {
                            await WriteOutput(error);
                            return error;
                        }

                        if (gameId != null)
                        {
                            var messageToChat = await tournament.SetCurrentGame(gameId, page, config, mapId);
                            if (messageToChat != null)
                            {
                                await WriteOutput(messageToChat);
                                Extensions.OpenUrl(GeoguessrApi.ChallengeLink(gameId));
                            }
                        }
                    }

                    var playerId = Config.Read("Countdown", "PlayerId") ?? string.Empty;
                    if (!string.IsNullOrEmpty(playerId))
                    {
                        var progress = await GetCountdownProgress(playerId);
                        var progressToFileText = Config.Read("Countdown", "ProgressToFileText")?.InterpolateCountdownValues(progress) ?? string.Empty;
                        await File.WriteAllTextAsync(CountdownProgressFilename, progressToFileText);
                    }
                }
                else if (ChallengeUrlRegex.IsMatch(inputCommand))
                {
                    var gameId = ChallengeUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt && !tournament.IsCurrentGameSameAs(gameId))
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !{inputCommand} to ignore.";
                        await WriteOutput(message);
                        return message;
                    }

                    var messageToChat = await tournament.SetCurrentGame(gameId, page, config, null);
                    if (messageToChat != null) await WriteOutput(messageToChat);
                }
                else if (ResultsUrlRegex.IsMatch(inputCommand))
                {
                    var gameId = ResultsUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt && !tournament.IsCurrentGameSameAs(gameId))
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !{inputCommand} to ignore.";
                        await WriteOutput(message);
                        return message;
                    }

                    await tournament.SetCurrentGame(gameId, page, config, null);
                    var (messageToChat, error) = await tournament.CheckIfCurrentGameFinished(page, config);
                    var toChat = messageToChat ?? error;
                    if (toChat != null) await WriteOutput(toChat);
                }
                else if (inputCommand == "gamescore")
                {
                    var url = await tournament.PrintGameScore(config);
                    var message = $"Last game results: {url}";
                    await WriteOutput(message);
                    return message;
                }
                else if (inputCommand == "endgame")
                {
                    var (messageToChat, error) = await tournament.CheckIfCurrentGameFinished(page, config);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                    }
                    return messageToChat;
                }
                else if (inputCommand == "elim")
                {
                    var message = await tournament.ToggleEliminations(page, config);
                    await WriteOutput($"Eliminations are now {tournament.PlayWithEliminations.ToOnOrOffString()}.");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    if (message != null)
                    {
                        await WriteOutput(message);
                        return message;
                    }
                }
                else if (inputCommand == "restart")
                {
                    tournament = tournament.Restart(await NameGenerator.New(config));
                }
                else if (int.TryParse(inputCommand, out var number) && number >= 0)
                {
                    var messageToChat = await tournament.EliminateAndFinish(page, config, number);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (LessOrMoreThanRegex.IsMatch(inputCommand))
                {
                    var isEliminationAction = inputCommand.Contains("elim", StringComparison.InvariantCultureIgnoreCase);
                    var points = Extensions.IntFromString(inputCommand);
                    var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
                    var messageToChat = isEliminationAction
                        ? await tournament.EliminatePlayers(pointsDescription, points, config)
                        : await tournament.RevivePlayers(pointsDescription, points, config);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (inputCommand.StartsWith("elim "))
                {
                    var playerSearchTerm = inputCommand.Skip("elim ".Length).AsString();
                    var messageToChat = await tournament.EliminateSpecificPlayer(playerSearchTerm, config);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (inputCommand.StartsWith("revive "))
                {
                    var playerSearchTerm = inputCommand.Skip("revive ".Length).AsString();
                    var messageToChat = await tournament.ReviveSpecificPlayer(playerSearchTerm, config);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (LessOrMoreThanFinishGameRegex.IsMatch(inputCommand))
                {
                    var points = Extensions.IntFromString(inputCommand);
                    var pointsDescription = inputCommand.Contains("less", StringComparison.InvariantCultureIgnoreCase) ? PointsDescription.LessThan : PointsDescription.MoreThan;
                    var messageToChat = await tournament.EliminateAndFinish(page, pointsDescription, points, config);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (inputCommand.StartsWith("game") || inputCommand.StartsWith("!game"))
                {
                    if (tournament.GameState == GameState.Running && commandType != CommandType.DamnIt)
                    {
                        var message = $"Game #{tournament.CurrentGameNumber()} has not ended. Use !endgame to end it first, or !!{inputCommand} to ignore and overwrite.";
                        await WriteOutput(message);
                        return message;
                    }

                    var parts = inputCommand.Split(' ');
                    var mapKey = parts.Skip(1).FirstOrDefault();
                    var timeDescription = parts.Skip(2).FirstOrDefault();
                    var gameModeDescription = parts.Skip(3).FirstOrDefault();
                    var groupedPlayedMaps = tournament.Games
                        .Select(x => x.MapId).Where(x => x != null).Select(x => x!)
                        .GroupBy(x => x).ToArray();
                    var maxPlayCount = groupedPlayedMaps.Any() ? groupedPlayedMaps.Max(x => x.Count()) : 0;
                    var mapIdsPlayed = groupedPlayedMaps.Where(x => x.Count() == maxPlayCount).Select(x => x.Key).ToList();
                    var (error, gameId, mapId) = await GeoguessrChallenge.Create(page, config, mapKey, timeDescription, gameModeDescription, mapIdsPlayed);
                    if (error != null)
                    {
                        await WriteOutput(error);
                        return error;
                    }

                    if (gameId != null)
                    {
                        var messageToChat = await tournament.SetCurrentGame(gameId, page, config, mapId);
                        if (messageToChat != null)
                        {
                            await WriteOutput(messageToChat);
                            return messageToChat;
                        }
                    }
                }
                else if (inputCommand == "maps")
                {
                    var maps = await GeoguessrChallenge.GetMaps();
                    var messageToChat = await Github.UploadMaps(config, maps);
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                        return messageToChat;
                    }
                }
                else if (inputCommand == "apiinfo")
                {
                    var messageToChat = GeoguessrApi.ApiCallsInfo();
                    await WriteOutput(messageToChat);
                    return messageToChat;
                }
                else if (inputCommand == "link")
                {
                    var messageToChat = tournament.CurrentGithubResultsPageUrl == null ? null : $"{tournament.CurrentGithubResultsPageUrl}&total=true";
                    if (messageToChat != null)
                    {
                        await WriteOutput(messageToChat);
                    }
                }
                else if (inputCommand == "currentgame")
                {
                    var messageToChat = tournament.CurrentGameUrl() ?? "No game running.";
                    await WriteOutput(messageToChat);
                    return messageToChat;
                }
                else if (LoadTournamentFromUrlRegex.IsMatch(inputCommand))
                {
                    var t = await GetTournamentFromCommandWithUrl(inputCommand);
                    if (t != null)
                    {
                        tournament = t;
                        var message = $"Loaded tournament with {t.Games.Count} games.";
                        await WriteOutput(message);
                        return message;
                    }
                }
                else if (inputCommand.StartsWith("google-sheet"))
                {
                    var url = await GoogleSheet.Create(tournament, filenameSourceForCommand);
                    var message = "URL to Google sheet copied to clipboard";
                    await Clip.SetText(url, message);
                    return message;
                }
                else if (!string.IsNullOrEmpty(Config.Read("Countdown", "ProgressCommand")) && inputCommand.StartsWith(Config.Read("Countdown", "ProgressCommand") ?? Guid.NewGuid().ToString()))
                {
                    var playerId = Config.Read("Countdown", "PlayerId");
                    if (string.IsNullOrEmpty(playerId))
                    {
                        var countdownMessage = "Countdown mode not active.";
                        await WriteOutput(countdownMessage);
                        return countdownMessage;
                    }

                    var progress = await GetCountdownProgress(playerId);
                    var message = (Config.Read("Countdown", "ProgressCommandMessage") ?? "{PointsRemaining} points remaining.")
                        .InterpolateCountdownValues(progress);
                    await WriteOutput(message);
                    return message;
                }
                else if (BanByUrlRegex.IsMatch(inputCommand))
                {
                    var userId = BanByUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    await AppDataProvider.BanUser(userId);
                    var message = "User banned";
                    await WritePrivateOutput(message);
                    var bannedUsersIds = await AppDataProvider.BannedUsersIds();
                    tournament.UpdateBans(bannedUsersIds);
                    return message;
                }
                else if (UnbanByUrlRegex.IsMatch(inputCommand))
                {
                    var userId = UnbanByUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
                    await AppDataProvider.UnbanUser(userId);
                    var message = "User unbanned";
                    await WritePrivateOutput(message);
                    return message;
                }
                else if (inputCommand == "bans")
                {
                    var bannedUserUrls = (await AppDataProvider.BannedUsersIds()).Select(x => $"https://www.geoguessr.com/user/{x}").ToList();
                    var message = $"{bannedUserUrls.Count} {"user".Pluralize(bannedUserUrls.Count)} banned{Environment.NewLine}{string.Join(Environment.NewLine, bannedUserUrls)}";
                    await WritePrivateOutput(message);
                    return message;
                }
                else if (inputCommand.StartsWith("create-or-update-map"))
                {
                    await GeoguessrApi.GenerateMap(page, inputCommand.Split(' ').Last());
                    return "Map update/creation done.";
                }
                else if (inputCommand.StartsWith("subscription "))
                {
                    var subDescription = inputCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last().Split('|');
                    var subscriptionPlan = Enum.TryParse<SubscriptionPlan>(subDescription.FirstOrDefault(), out var plan) ? plan : SubscriptionPlan.NotSet;
                    var loginName = subDescription.Length > 1 ? subDescription[1] : string.Empty;
                    await File.AppendAllLinesAsync(SubscriptionsFilename, new[] { $"{subscriptionPlan} {loginName} {DateTime.Now:O}" });
                    await WritePrivateOutput($"{subscriptionPlan} subscription from {loginName}");
                    return null;
                }
                else if (inputCommand.StartsWith("bits "))
                {
                    var bitsDescription = inputCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last().Split('|');
                    var bits = int.TryParse(bitsDescription.FirstOrDefault(), out var bitNumber) ? bitNumber : 0;
                    var loginName = bitsDescription.Length > 1 ? bitsDescription[1] : string.Empty;
                    await File.AppendAllLinesAsync(BitsFilename, new[] { $"{bits} {loginName} {DateTime.Now:O}" });
                    await WritePrivateOutput($"{bits} bits from {loginName}");
                    return null;
                }
                else if (inputCommand == "all-game-links")
                {
                    var links = await Github.GameLinks(config);
                    if (links.Any())
                    {
                        var filePath = "all-game-links.txt";
                        await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, links));
                        await WriteOutput($"Saved links in {filePath}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("An unexpected error occurred.");
                try
                {
                    await File.AppendAllTextAsync("errors.txt", $"{DateTime.UtcNow:s}: {Extensions.GetVersion()}{Environment.NewLine}{e}{Environment.NewLine}");
                    if (inputCommand != null)
                    {
                        await WriteOutput("Looks like you found a bug. That did not work as expected.");
                        return e.ToString();
                    }
                }
                catch (Exception second)
                {
                    Console.WriteLine("Woha. Everything fails now?");
                    Console.WriteLine(second);
                }
            }

            return null;
        }

        private static async Task<GeoTournament?> GetTournamentFromCommandWithUrl(string inputCommand)
        {
            var username = LoadTournamentFromUrlRegex.Matches(inputCommand)[0].Groups[1].Value;
            var url = inputCommand.Split(' ').Skip(1).First();
            var id = Extensions.GetFromQueryString(new Uri(url).Query, "id");
            if (id == null)
            {
                var message = $"Invalid URL. Missing ?id= | {url}";
                await WriteOutput(message);
                return null;
            }

            try
            {
                return await TournamentHistory.CreateTournamentFromUrl(username, id, url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        static void OnMessageReceived(object? sender, string? e)
        {
            var message = e ?? string.Empty;
            if (ChallengeUrlRegex.IsMatch(message) || ResultsUrlRegex.IsMatch(message))
                ReadConsole.QueueCommand(message);
            else if (message.StartsWith("!")) ReadConsole.QueueCommand(message.Skip(1).AsString());
            else if (int.TryParse(message, out var number) && number >= 0) ReadConsole.QueueCommand(number.ToString());
        }

        static async Task<CountDownProgress> GetCountdownProgress(string playerId)
        {
            var totalSum = tournament.Games.Sum(x => x.PlayerGames.Where(p => p.userId == playerId).Sum(p => p.totalScore));
            var averageGameSum = tournament.Games.Any() ? tournament.Games.Average(x => x.PlayerGames.Where(p => p.userId == playerId).Sum(p => p.totalScore)) : 0;
            var gamesPlayed = tournament.Games.Count(x => x.PlayerGames.Any(p => p.userId == playerId));
            var countDownGoal = Config.Read("Countdown", "Goal");
            var goalPoints = int.TryParse(countDownGoal, out var points) ? points : 0;
            var subscriptionPunishment = await SubscriptionPunishment();
            var bitsPunishment = await BitsPunishment();
            var punishment = subscriptionPunishment + bitsPunishment;
            var remainingPoints = Math.Max(goalPoints - totalSum + punishment, 0);
            var approximateNumberOfGamesLeft = averageGameSum > 0 ? Math.Round(remainingPoints / averageGameSum, 0) : 0;
            return new CountDownProgress
            {
                TotalSum = totalSum,
                TotalSumMinusPunishment = totalSum - punishment,
                AverageGameSum = averageGameSum,
                ApproximateNumberOfGamesLeft = approximateNumberOfGamesLeft,
                GoalPoints = goalPoints,
                GoalPlusPunishment = goalPoints + punishment,
                GamesPlayed = gamesPlayed,
                RemainingPoints = remainingPoints
            };
        }

        private static async Task<int> SubscriptionPunishment()
        {
            if (!File.Exists(SubscriptionsFilename))
            {
                return 0;
            }

            var subPunishmentPoints = int.TryParse(Config.Read("Countdown", "SubscriptionPunishment"), out var punishPoints) ? punishPoints : 25_000;
            var subscriptionLines = await File.ReadAllLinesAsync(SubscriptionsFilename);
            return subscriptionLines.Select(x => new
            {
                SubPlan = Enum.TryParse<SubscriptionPlan>(x.Split(' ').FirstOrDefault(), out var plan)
                    ? plan
                    : SubscriptionPlan.NotSet
            }).Sum(x => SubPlanMultiplier(x.SubPlan) * subPunishmentPoints);
        }

        private static async Task<int> BitsPunishment()
        {
            if (!File.Exists(BitsFilename))
            {
                return 0;
            }

            var pointPunishmentPerBit = int.TryParse(Config.Read("Countdown", "PointsPerBitPunishment"), out var punishPoints) ? punishPoints : 50;
            var bitsLines = await File.ReadAllLinesAsync(BitsFilename);
            return bitsLines.Select(x => new
            {
                Bits = int.TryParse(x.Split(' ').FirstOrDefault(), out var plan)
                    ? plan
                    : 0
            }).Sum(x => x.Bits * pointPunishmentPerBit);
        }

        private static int SubPlanMultiplier(SubscriptionPlan subPlan) =>
            subPlan switch
            {
                SubscriptionPlan.NotSet => 0,
                SubscriptionPlan.Prime => 1,
                SubscriptionPlan.Tier1 => 1,
                SubscriptionPlan.Tier2 => 2,
                SubscriptionPlan.Tier3 => 5,
                _ => 0
            };

        static string InterpolateCountdownValues(this string message, CountDownProgress progress) => message
            .Replace("{PointsRemaining}", progress.RemainingPoints.ToString("N0", CultureInfo.InvariantCulture))
            .Replace("{TotalPointsPlayed}", progress.TotalSum.ToString("N0", CultureInfo.InvariantCulture))
            .Replace("{GoalPlusPunishment}", progress.GoalPlusPunishment.ToString("N0", CultureInfo.InvariantCulture))
            .Replace("{ApproximateGamesLeft}", progress.ApproximateNumberOfGamesLeft.ToString("N0", CultureInfo.InvariantCulture))
            .Replace("{TotalSumMinusPunishment}", progress.TotalSumMinusPunishment.ToString("N0", CultureInfo.InvariantCulture))
            .Replace("{GamesPlayed}", progress.GamesPlayed.ToString("N0", CultureInfo.InvariantCulture));


        static async Task WriteOutput(string message)
        {
            foreach (var output in activeOutputs)
            {
                await output.Write(message);
            }
        }

        static async Task WritePrivateOutput(string message)
        {
            foreach (var output in activeOutputs.Where(x => x.SupportsPrivateMessages))
            {
                await output.Write(message);
            }
        }

        record CountDownProgress
        {
            public int TotalSum { get; set; }
            public double AverageGameSum { get; set; }
            public int GamesPlayed { get; set; }
            public int GoalPoints { get; set; }
            public int RemainingPoints { get; set; }
            public double ApproximateNumberOfGamesLeft { get; set; }
            public int GoalPlusPunishment { get; set; }
            public int TotalSumMinusPunishment { get; set; }
        }
    }
}