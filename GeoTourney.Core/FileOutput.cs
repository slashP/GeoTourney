using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core
{
    public class FileOutput : IGameEventOutput
    {
        string _folder = string.Empty;

        public Task<InitializationStatus> Initialize(
            IConfiguration configuration,
            EventHandler<string> onMessageReceived)
        {
            _folder = configuration["LogFolder"] ?? string.Empty;
            var initializationStatus = string.IsNullOrEmpty(_folder) || !Directory.Exists(_folder) ? InitializationStatus.Disabled : InitializationStatus.Ok;
            return Task.FromResult(initializationStatus);
        }

        public Task Write(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(_folder, "game-events.txt"), message + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }

        public Task KeepAlive() => Task.CompletedTask;

        public bool SupportsPrivateMessages => true;
    }
}