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

        public static async Task<(bool hasAccess, string errorMessage)> VerifyGithubTokenAccess(IConfiguration configuration)
        {
            var client = GitHubClient(configuration);
            var user = await client.User.Current();
            return !client.GetLastApiInfo().OauthScopes.Contains(RequiredScope)
                ? (false, $"Github token provided does not have '{RequiredScope}' permission. Go to https://github.com/settings/tokens to create a token or edit an existing one.")
                : (true, string.Empty);
        }

        public static async Task<string> UploadTournamentData(IConfiguration configuration, string fileContent)
        {
            var client = GitHubClient(configuration);
            var user = await client.User.Current();
            var owner = user.Login.ToLower();
            var repoName = $"{owner}.github.io";
            var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
            var content = EmbeddedFileHelper.Content("htmlTemplate.html");
            var localSuffix = string.Empty;
#if DEBUG
            localSuffix = $"-{Extensions.ShortHash(content)}";
#endif
            var version = Extensions.GetMajorMinorVersion();
            var githubHtmlFilePath = $"geoguessr/{version}{localSuffix}/tournament.html";
            await CreateFileIfNotExists(client, repo, content, githubHtmlFilePath, "File change.");
            var id = DateTime.Now.Ticks.ToString();
            var path = $"geoguessr/{id}.json";
            await CreateFileIfNotExists(client, repo, fileContent, path, "Geoguessr tournament.");
            return $"https://{repoName}/{githubHtmlFilePath}?id={id}";
        }

        public static async Task<string?> UploadMaps(IConfiguration configuration, IReadOnlyCollection<GeoguessrMap> maps)
        {
            try
            {
                var client = GitHubClient(configuration);
                var user = await client.User.Current();
                var owner = user.Login.ToLower();
                var repoName = $"{owner}.github.io";
                var repo = await CreateRepositoryIfNotExists(client, owner, repoName);
                var content = EmbeddedFileHelper.Content("mapsTemplate.html");
                var githubHtmlFilePath = "geoguessr/maps.html";
                var mapsContent = JsonSerializer.Serialize(maps);
                var id = DateTime.Now.Ticks.ToString();
                var path = $"geoguessr/maps.{id}.json";

                await CreateOrUpdateFile(client, repo, mapsContent, path, "Maps page.");
                await CreateOrUpdateFile(client, repo, content, githubHtmlFilePath, "Maps page.");
                return $"https://{repoName}/{githubHtmlFilePath}?id={id}";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        static GitHubClient GitHubClient(IConfiguration configuration)
        {
            var client = new GitHubClient(new ProductHeaderValue("OMHToken"))
            {
                Credentials = new Credentials(configuration["GithubToken"])
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
    }
}