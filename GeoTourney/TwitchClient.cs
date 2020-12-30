using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace GeoTourney
{
    public class TwitchClient : IGameEventOutput
    {
        TwitchLib.Client.TwitchClient? _client;
        string? _twitchChannel;
        string? _twitchBotUsername;
        EventHandler<string>? _onMessageReceived;
        string? _twitchOAuth;

        public InitializationStatus Initialize(IConfiguration configuration, EventHandler<string> onMessageReceived)
        {
            _onMessageReceived = onMessageReceived;
            _twitchChannel = configuration["TwitchChannel"];
            _twitchBotUsername = configuration["TwitchBotUsername"];
            _twitchOAuth = configuration["TwitchToken"];
            if (string.IsNullOrEmpty(_twitchChannel) || string.IsNullOrEmpty(_twitchBotUsername) || string.IsNullOrEmpty(_twitchOAuth))
            {
                return InitializationStatus.Disabled;
            }

            return Initialize();
        }

        public void Write(string message)
        {
            try
            {
                _client?.SendMessage(_twitchChannel, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void KeepAlive()
        {
            if (_client == null || !_client.IsConnected || !_client.JoinedChannels.Any())
            {
                Initialize();
            }
        }

        InitializationStatus Initialize()
        {
            try
            {
                var credentials = new ConnectionCredentials(_twitchBotUsername, _twitchOAuth);
                var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30),
                    ReconnectionPolicy = new ReconnectionPolicy(reconnectInterval: 3000, maxAttempts: 500)
                };
                var customClient = new WebSocketClient(clientOptions);
                _client = new TwitchLib.Client.TwitchClient(customClient);
                _client.Initialize(credentials, _twitchChannel);
                _client.OnMessageReceived += Client_OnMessageReceived;

                void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
                {
                    if (e.ChatMessage.IsBroadcaster)
                    {
                        _onMessageReceived?.Invoke(null, e.ChatMessage.Message);
                    }
                }

                _client.Connect();
                return InitializationStatus.Ok;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return InitializationStatus.Failed;
            }
        }
    }
}