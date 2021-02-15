using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Windows
{
    public class TextboxOutput : IGameEventOutput
    {
        readonly Action<string> _log;

        public TextboxOutput(Action<string> log)
        {
            _log = log;
        }

        public Task<InitializationStatus> Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived) =>
            Task.FromResult(InitializationStatus.Ok);

        public Task Write(string message)
        {
            _log(message);
            return Task.CompletedTask;
        }

        public Task KeepAlive() => Task.CompletedTask;

        public bool SupportsPrivateMessages => true;
    }
}