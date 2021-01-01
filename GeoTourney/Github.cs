using System;
using System.IO;
using System.Linq;
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

            var githubHtmlFilePath = "geoguessr/v3.21/tournament.html";
            await CreateFileIfNotExists(client, repo, EmbeddedFileHelper.Content("htmlTemplate.html"), githubHtmlFilePath, "File change.");
            var id = DateTime.Now.Ticks.ToString();
            var path = $"geoguessr/{id}.json";
            await CreateFileIfNotExists(client, repo, fileContent, path, "Geoguessr tournament.");
            return $"https://{repoName}/{githubHtmlFilePath}?id={id}";
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
    }
}