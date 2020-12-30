using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GeoTourney
{
    public class FileOutput : IGameEventOutput
    {
        string _folder = string.Empty;

        public InitializationStatus Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived)
        {
            _folder = configuration["LogFolder"];
            return string.IsNullOrEmpty(_folder) || !Directory.Exists(_folder) ? InitializationStatus.Disabled : InitializationStatus.Ok;
        }

        public void Write(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(_folder, "game-events.txt"), message + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void KeepAlive()
        {
        }
    }
}