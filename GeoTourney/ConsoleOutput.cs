using System;
using Microsoft.Extensions.Configuration;

namespace GeoTourney
{
    public class ConsoleOutput : IGameEventOutput
    {
        public InitializationStatus Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived)
        {
            return InitializationStatus.Ok;
        }

        public void Write(string message)
        {
            Console.WriteLine(message);
        }

        public void KeepAlive()
        {
        }
    }
}