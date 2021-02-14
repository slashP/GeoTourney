using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace GeoTourney
{
    public static class Github
    {
        const string RequiredScope = "public_repo";
        const string InvalidTokenMessage = "Github token is not set correctly in appsettings.json file.";

        public static async Task<(bool hasAccess, string errorMessage)> VerifyGithubTokenAccess(IConfiguration configuration)
        {
            var client = GitHubClient(configuration);
            if (client == null)
            {
                return (false, InvalidTokenMessage);
            }

            var user = await client.User.Current();
            return !client.GetLastApiInfo().OauthScopes.Contains(RequiredScope)
                ? (false, $"Github token provided does not have '{RequiredScope}' permission. Go to https://github.com/settings/tokens to create a token or edit an existing one.")
                : (true, string.Empty);
        }

        public static async Task<string> UploadTournamentData(IConfiguration configuration, GithubTournamentData data)
        {
            var client = GitHubClient(configuration);
            if (client == null)
            {
                return InvalidTokenMessage;
            }

            var user = await client.User.Current();
            var owner = user.Login.ToLower();
            var repoName = $"{owner}.github.io";
            var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
            var githubHtmlFilePath = TournamentTemplateGithubPath();
            var today = Today();
            var secondsSinceMidnight = (int) (DateTime.Now - DateTime.Today).TotalSeconds;
            var id = $"{today}/{data.nickname}-{secondsSinceMidnight}";
            var path = $"geoguessr/{id}.json";
            await DeleteFilesInFolder(client, repo, $"geoguessr/{today}", file => !string.IsNullOrEmpty(data.nickname) && file.Name.StartsWith(data.nickname));
            var fileContent = JsonSerializer.Serialize(data);
            await CreateOrUpdateFile(client, repo, fileContent, path, "Geoguessr tournament.");
            return $"https://{repoName}/{githubHtmlFilePath}?id={id}{BranchQueryString(repo)}";
        }

        static string TournamentTemplateGithubPath()
        {
            var content = EmbeddedFileHelper.Content("htmlTemplate.html");
            var localSuffix = string.Empty;
#if DEBUG
            localSuffix = $"-{Extensions.ShortHash(content)}";
#endif
            var version = Extensions.GetMajorMinorVersion();
            var githubHtmlFilePath = $"geoguessr/{version}{localSuffix}/tournament.html";
            return githubHtmlFilePath;
        }

        public static async Task CreateOrUpdateTemplates(IConfiguration configuration)
        {
            var client = GitHubClient(configuration);
            if (client == null)
            {
                return;
            }

            var content = EmbeddedFileHelper.Content("htmlTemplate.html");
            var mapsContent = EmbeddedFileHelper.Content("mapsTemplate.html");
            var user = await client.User.Current();
            var owner = user.Login.ToLower();
            var repoName = $"{owner}.github.io";
            var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
            var tournamentFilePath = TournamentTemplateGithubPath();

            await CreateFileIfNotExists(client, repo, content, tournamentFilePath, "Tournament template.");
            await CreateOrUpdateFile(client, repo, mapsContent, "geoguessr/maps.html", "Maps template.");
        }

        public static async Task<string?> UploadMaps(IConfiguration configuration, IReadOnlyCollection<GeoguessrMap> maps)
        {
            try
            {
                var client = GitHubClient(configuration);
                if (client == null)
                {
                    return InvalidTokenMessage;
                }

                var user = await client.User.Current();
                var owner = user.Login.ToLower();
                var repoName = $"{owner}.github.io";
                var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
                var githubHtmlFilePath = "geoguessr/maps.html";
                var mapsContent = JsonSerializer.Serialize(maps);
                var id = DateTime.Now.Ticks.ToString();
                var path = $"geoguessr/maps/{id}.json";

                await DeleteFilesInFolder(client, repo, "geoguessr/maps", _ => true);
                await CreateOrUpdateFile(client, repo, mapsContent, path, "Maps page.");
                return $"https://{repoName}/{githubHtmlFilePath}?id={id}{BranchQueryString(repo)}";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public static async Task<bool> TournamentExists(IConfiguration configuration, string name)
        {
            try
            {
                var client = GitHubClient(configuration);
                if (client == null)
                {
                    return false;
                }

                var user = await client.User.Current();
                var owner = user.Login.ToLower();
                var repoName = $"{owner}.github.io";
                var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
                var files = await client.Repository.Content.GetAllContents(repo.Id, $"geoguessr/{Today()}");
                return files.Any(x => x.Name == FilenameFromTournamentName(name));
            }
            catch (NotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static GitHubClient? GitHubClient(IConfiguration configuration)
        {
            var token = configuration["GithubToken"];
            if (string.IsNullOrEmpty(token) || token == "GITHUB_TOKEN")
            {
                return null;
            }

            var client = new GitHubClient(new ProductHeaderValue("OMHToken"))
            {
                Credentials = new Credentials(token)
            };
            return client;
        }

        static async Task<Repository> CreateRepositoryIfNotExists(GitHubClient client, string owner, string repoName)
        {
            Repository? repo;
            try
            {
                repo = await client.Repository.Get(owner, repoName);
            }
            catch (NotFoundException)
            {
                try
                {
                    repo = await client.Repository.Create(new NewRepository(repoName));
                }
                catch (NotFoundException)
                {
                    Console.WriteLine(
                        $"The Github token provided does not have access to the 'public_repo' scope. Unable to create repository {repoName}");
                    throw;
                }
            }

            return repo;
        }

        static async Task CreateFileIfNotExists(GitHubClient client, Repository repo, string fileContent, string path, string commitMessage)
        {
            try
            {
                await client.Repository.Content.GetAllContents(repo.Id, path);
            }
            catch (NotFoundException)
            {
                try
                {
                    await client.Repository.Content.CreateFile(
                        repo.Id,
                        path,
                        new CreateFileRequest(commitMessage, fileContent, repo.DefaultBranch));
                }
                catch (NotFoundException)
                {
                    Console.WriteLine($"The Github token provided does not have access to the 'public_repo' scope. Unable to create file '{path}' in repository {repo.FullName}");
                    throw;
                }
            }
        }

        static async Task CreateOrUpdateFile(GitHubClient client, Repository repo, string fileContent, string path, string commitMessage)
        {
            try
            {
                var file = await client.Repository.Content.GetAllContents(repo.Id, path);
                await client.Repository.Content.UpdateFile(repo.Id, path,
                    new UpdateFileRequest(commitMessage, fileContent, file.First().Sha));
            }
            catch (NotFoundException)
            {
                try
                {
                    await client.Repository.Content.CreateFile(
                        repo.Id,
                        path,
                        new CreateFileRequest(commitMessage, fileContent, repo.DefaultBranch));
                }
                catch (NotFoundException)
                {
                    Console.WriteLine($"The Github token provided does not have access to the 'public_repo' scope. Unable to create file '{path}' in repository {repo.FullName}");
                }
            }
        }

        static async Task DeleteFilesInFolder(GitHubClient client, Repository repo, string path, Func<RepositoryContent, bool> nameSelector)
        {
            try
            {
                var contents = await client.Repository.Content.GetAllContents(repo.Id, path);
                foreach (var repositoryContent in contents.Where(nameSelector))
                {
                    await client.Repository.Content.DeleteFile(repo.Id, repositoryContent.Path,
                        new DeleteFileRequest("Clean up old files.", repositoryContent.Sha));
                }
            }
            catch (NotFoundException)
            {
            }
        }

        static string BranchQueryString(Repository repo) =>
            repo.DefaultBranch != "main" ? $"&branch={repo.DefaultBranch}" : string.Empty;

        static string Today() => $"{DateTime.Now:yyyy-MM-dd}";

        static string FilenameFromTournamentName(string name) => $"{name}.json";
    }
}