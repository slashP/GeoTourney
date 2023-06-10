using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
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
                if (_client != null)
                {
                    _client.OnMessageReceived -= Client_OnMessageReceived;
                    _client.OnLog -= ClientOnOnLog;
                    _client.OnConnected -= ClientOnOnConnected;
                    _client.OnDisconnected -= ClientOnOnDisconnected;
                    _client.OnReconnected -= ClientOnOnReconnected;
                    _client.OnChannelStateChanged -= ClientOnOnChannelStateChanged;
                    _client.OnJoinedChannel -= ClientOnOnJoinedChannel;

                    _client.OnCommunitySubscription -= ClientOnOnCommunitySubscription;
                    _client.OnGiftedSubscription -= ClientOnOnGiftedSubscription;
                    _client.OnContinuedGiftedSubscription -= ClientOnOnContinuedGiftedSubscription;
                    _client.OnNewSubscriber -= ClientOnOnNewSubscriber;
                    _client.OnPrimePaidSubscriber -= ClientOnOnPrimePaidSubscriber;
                    _client.OnReSubscriber -= ClientOnOnReSubscriber;
                }

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
                _client.OnLog += ClientOnOnLog;
                _client.OnConnected += ClientOnOnConnected;
                _client.OnDisconnected += ClientOnOnDisconnected;
                _client.OnReconnected += ClientOnOnReconnected;
                _client.OnChannelStateChanged += ClientOnOnChannelStateChanged;
                _client.OnJoinedChannel += ClientOnOnJoinedChannel;
                void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
                {
                    var publicCommands = CommandHandler.PublicCommands();
                    if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
                    {
                        _onMessageReceived?.Invoke(null, e.ChatMessage.Message);
                    }
                    else if (e.ChatMessage.Message.StartsWith("!") && publicCommands.Contains(e.ChatMessage.Message.Split(' ').First()[1..]))
                    {
                        _onMessageReceived?.Invoke(null, e.ChatMessage.Message);
                    }

                    if (e.ChatMessage.Bits > 0)
                    {
                        _onMessageReceived?.Invoke(null, $"!bits {e.ChatMessage.Bits}|{e.ChatMessage.Username}");
                    }
                }

                _client.OnCommunitySubscription += ClientOnOnCommunitySubscription;
                _client.OnGiftedSubscription += ClientOnOnGiftedSubscription;
                _client.OnContinuedGiftedSubscription += ClientOnOnContinuedGiftedSubscription;
                _client.OnNewSubscriber += ClientOnOnNewSubscriber;
                _client.OnPrimePaidSubscriber += ClientOnOnPrimePaidSubscriber;
                _client.OnReSubscriber += ClientOnOnReSubscriber;
                var connected = _client.Connect();
                return connected ? InitializationStatus.Ok : InitializationStatus.Disabled;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return InitializationStatus.Failed;
            }
        }

        private void ClientOnOnReSubscriber(object? sender, OnReSubscriberArgs args) => OnSub(args.ReSubscriber.SubscriptionPlan, args.ReSubscriber.Login);

        private void ClientOnOnPrimePaidSubscriber(object? sender, OnPrimePaidSubscriberArgs args) => OnSub(args.PrimePaidSubscriber.SubscriptionPlan, args.PrimePaidSubscriber.Login);

        private void ClientOnOnNewSubscriber(object? sender, OnNewSubscriberArgs args) => OnSub(args.Subscriber.SubscriptionPlan, args.Subscriber.Login);

        private void ClientOnOnContinuedGiftedSubscription(object? sender, OnContinuedGiftedSubscriptionArgs args) => OnSub(SubscriptionPlan.Tier1, args.ContinuedGiftedSubscription.Login);

        private void ClientOnOnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs args) => OnSub(args.GiftedSubscription.MsgParamSubPlan, args.GiftedSubscription.Login);

        private void ClientOnOnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs args) => OnSub(args.GiftedSubscription.MsgParamSubPlan, args.GiftedSubscription.Login);

        private static void ClientOnOnJoinedChannel(object? sender, OnJoinedChannelArgs args) => WriteToConsole($"Joined channel {args.Channel} | {args.BotUsername}");

        private static void ClientOnOnChannelStateChanged(object? sender, OnChannelStateChangedArgs args) => WriteToConsole($"Channel state changed {args.Channel} | {args.ChannelState}");

        private static void ClientOnOnReconnected(object? sender, OnReconnectedEventArgs e) => WriteToConsole("Reconnected");

        private static void ClientOnOnDisconnected(object? sender, OnDisconnectedEventArgs e) => WriteToConsole("Disconnected");

        private static void ClientOnOnConnected(object? sender, OnConnectedArgs args) => WriteToConsole($"Connected {args.BotUsername} | {args.AutoJoinChannel}");

        private static void ClientOnOnLog(object? sender, OnLogArgs args) => WriteToConsole($"{args.DateTime} | {args.BotUsername} | {args.Data}");

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