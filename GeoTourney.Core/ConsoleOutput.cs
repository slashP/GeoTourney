using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core
{
    public class ConsoleOutput : IGameEventOutput
    {
        public Task<InitializationStatus> Initialize(
            IConfiguration configuration,
            EventHandler<string> onMessageReceived)
        {
            return Task.FromResult(InitializationStatus.Ok);
        }

        public Task Write(string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }


        public Task KeepAlive() => Task.CompletedTask;

        public bool SupportsPrivateMessages => true;
    }
}