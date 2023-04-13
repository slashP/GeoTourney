using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace GeoTourney.Core
{
    public class TwitchClient : IGameEventOutput
    {
        public const string TwitchChannelConfigKey = "TwitchChannel";
        TwitchLib.Client.TwitchClient? _client;
        string? _twitchChannel;
        string? _twitchBotUsername;
        EventHandler<string>? _onMessageReceived;
        string? _twitchOAuth;

        public Task<InitializationStatus> Initialize(
            IConfiguration configuration,
            EventHandler<string> onMessageReceived)
        {
            _onMessageReceived = onMessageReceived;
            _twitchChannel = configuration[TwitchChannelConfigKey];
            _twitchBotUsername = configuration["TwitchBotUsername"];
            _twitchOAuth = configuration["TwitchToken"];
            if (string.IsNullOrEmpty(_twitchChannel) || string.IsNullOrEmpty(_twitchBotUsername) || string.IsNullOrEmpty(_twitchOAuth))
            {
                return Task.FromResult(InitializationStatus.Disabled);
            }

            return Task.FromResult(Initialize());
        }

        public Task Write(string message)
        {
            try
            {
                _client?.SendMessage(_twitchChannel, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }

        public Task KeepAlive()
        {
            if (_client == null || !_client.IsConnected)
            {
                Initialize();
            }

            if (_client != null && !_client.JoinedChannels.Any())
            {
                _client.JoinChannel(_twitchChannel);
            }

            return Task.CompletedTask;
        }

        public bool SupportsPrivateMessages => false;

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
                _client.OnLog += (sender, args) => WriteToConsole($"{args.DateTime} | {args.BotUsername} | {args.Data}");
                _client.OnConnected += (sender, args) => WriteToConsole($"Connected {args.BotUsername} | {args.AutoJoinChannel}");
                _client.OnDisconnected += (sender, args) => WriteToConsole("Disconnected");
                _client.OnReconnected += (sender, args) => WriteToConsole("Reconnected");
                _client.OnChannelStateChanged += (sender, args) => WriteToConsole($"Channel state changed {args.Channel} | {args.ChannelState}");
                _client.OnJoinedChannel += (sender, args) => WriteToConsole($"Joined channel {args.Channel} | {args.BotUsername}");
                void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
                {
                    if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
                    {
                        _onMessageReceived?.Invoke(null, e.ChatMessage.Message);
                    }

                    if (e.ChatMessage.Bits > 0)
                    {
                        _onMessageReceived?.Invoke(null, $"!bits {e.ChatMessage.Bits}|{e.ChatMessage.Username}");
                    }
                }

                _client.OnCommunitySubscription += (sender, args) => OnSub(args.GiftedSubscription.MsgParamSubPlan, args.GiftedSubscription.Login);
                _client.OnGiftedSubscription += (sender, args) => OnSub(args.GiftedSubscription.MsgParamSubPlan, args.GiftedSubscription.Login);
                _client.OnContinuedGiftedSubscription += (sender, args) => OnSub(SubscriptionPlan.Tier1, args.ContinuedGiftedSubscription.Login);
                _client.OnNewSubscriber += (sender, args) => OnSub(args.Subscriber.SubscriptionPlan, args.Subscriber.Login);
                _client.OnPrimePaidSubscriber += (sender, args) => OnSub(args.PrimePaidSubscriber.SubscriptionPlan, args.PrimePaidSubscriber.Login);
                _client.OnReSubscriber += (sender, args) => OnSub(args.ReSubscriber.SubscriptionPlan, args.ReSubscriber.Login);
                var connected = _client.Connect();
                return connected ? InitializationStatus.Ok : InitializationStatus.Disabled;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return InitializationStatus.Failed;
            }
        }

        private void OnSub(SubscriptionPlan subscriptionPlan, string login)
        {
            _onMessageReceived?.Invoke(null, $"!subscription {subscriptionPlan}|{login}");
        }

        private static void WriteToConsole(string? message)
        {
            if (Config.IsDebug())
            {
                Console.WriteLine(message);
            }
        }
    }
}