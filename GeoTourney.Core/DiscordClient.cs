using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core
{
    public class DiscordClient : IGameEventOutput
    {
        DiscordSocketClient? _client;
        EventHandler<string>? _onMessageReceived;
        string? _token;

        public async Task<InitializationStatus> Initialize(
            IConfiguration configuration,
            EventHandler<string> onMessageReceived)
        {
            _client = new DiscordSocketClient();
            _onMessageReceived = onMessageReceived;
            _client.MessageReceived += ClientOnMessageReceived;

            _token = configuration["DiscordToken"];
            if (string.IsNullOrEmpty(_token))
            {
                return InitializationStatus.Failed;
            }

            return await Initialize();
        }

        public async Task Write(string message)
        {
            var channelId = _client?.Guilds.SelectMany(x => x.Channels).FirstOrDefault(x => x.Name == "geotourney")?.Id;
            if (channelId.HasValue && _client?.GetChannel(channelId.Value) is ISocketMessageChannel channel)
            {
                await channel.SendMessageAsync(message);
            }
        }

        public async Task KeepAlive()
        {
            if (_client == null || _client.ConnectionState != ConnectionState.Connected)
            {
                await Initialize();
            }
        }

        public bool SupportsPrivateMessages => true;

        async Task<InitializationStatus> Initialize()
        {
            try
            {
                if (_client == null)
                {
                    return InitializationStatus.Failed;
                }

                await _client.LoginAsync(TokenType.Bot, _token);
                await _client.StartAsync();
                return InitializationStatus.Ok;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return InitializationStatus.Failed;
            }
        }

        Task ClientOnMessageReceived(SocketMessage arg)
        {
            _onMessageReceived?.Invoke(this, arg.Content);
            return Task.CompletedTask;
        }
    }
}