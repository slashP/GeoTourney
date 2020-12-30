using System;
using Microsoft.Extensions.Configuration;

namespace GeoTourney
{
    public interface IGameEventOutput
    {
        InitializationStatus Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived);

        void Write(string message);
        void KeepAlive();
    }
}