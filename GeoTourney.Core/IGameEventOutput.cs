using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core
{
    public interface IGameEventOutput
    {
        Task<InitializationStatus> Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived);

        Task Write(string message);
        Task KeepAlive();
        bool SupportsPrivateMessages { get; }
    }
}