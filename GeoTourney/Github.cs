using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace GeoTourney
{
    public static class Github
    {
        public static async Task<string> UploadTournamentData(IConfiguration configuration, string fileContent)
        {
            var client = new GitHubClient(new ProductHeaderValue("OMHToken"))
            {
                Credentials = new Credentials(configuration["GithubToken"])
            };
            var user = await client.User.Current();
            var owner = user.Login.ToLower();
            var repoName = $"{owner}.github.io";
            Repository? repo;
            try
            {
                repo = await client.Repository.Get(owner, repoName);
            }
            catch (NotFoundException)
            {
                repo = await client.Repository.Create(new NewRepository(repoName));
            }

            await CreateFileIfNotExists(client, repo, "geoguessr/leaflet.js", "js/leaflet.js");
            var githubHtmlFilePath = "geoguessr/v3/tournament.html";
            await CreateFileIfNotExists(client, repo, githubHtmlFilePath, "htmlTemplate.html");
            var id = DateTime.Now.Ticks.ToString();
            var path = $"geoguessr/{id}.json";
            var changeSet = await client.Repository.Content.CreateFile(
                owner,
                repoName,
                path,
                new CreateFileRequest("Geoguessr tournament", fileContent, repo.DefaultBranch));
            return $"https://{repoName}/{githubHtmlFilePath}?id={id}";
        }

        static async Task CreateFileIfNotExists(GitHubClient client, Repository repo, string path, string localFilePath)
        {
            try
            {
                await client.Repository.Content.GetAllContents(repo.Id, path);
            }
            catch (NotFoundException)
            {
                var fileContent = await File.ReadAllTextAsync(localFilePath);
                await client.Repository.Content.CreateFile(
                    repo.Id,
                    path,
                    new CreateFileRequest("File change.", fileContent, repo.DefaultBranch));
            }
        }
    }
}