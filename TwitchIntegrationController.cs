using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.TootTallyNotifs;
using TwitchLib.Api.Helix.Models.Search;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
using UnityEngine;
using static TootTallyTwitchLibs.Plugin;

namespace TootTallyTwitchLibs
{
    public class TwitchIntegrationController : MonoBehaviour
    {
        public bool IsLogged { get; private set; }
        public bool IsConnectionPending { get; private set; }
        public bool HasJoinedChannel { get; private set; }
        public bool IsConnected => _client != null && _client.IsConnected;
        public bool IsReady => IsLogged && IsConnected && HasJoinedChannel;
        
        private TwitchClient _client;
        private TwitchConfigVariables _config;
        private ConcurrentStack<string> _messageStack;

        public Action OnUpdate;
        public Action<object, OnLogArgs> OnLoggin;
        public Action<object, OnJoinedChannelArgs> OnJoinedChannel;
        public Action<object, OnConnectedArgs> OnConnected;
        public Action<object, OnChatCommandReceivedArgs> OnChatCommandReceived;
        public Action<object, OnIncorrectLoginArgs> OnIncorrectLogin;
        public Action<object, OnErrorEventArgs> OnError;
        public Action<object, OnDisconnectedEventArgs> OnDisconnected;

        public void Init()
        {
            IsConnectionPending = false;
            IsLogged = false;
            HasJoinedChannel = false;
            _config = Plugin.Instance.ConfigVariables;
            if (!IsConfigValid(_config)) return;

            ConnectionCredentials credentials = new ConnectionCredentials(_config.TwitchChannelName, _config.AccessToken);
            var clientOptions = new ClientOptions()
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(reconnectInterval: 5, maxAttempts: 3)
            };
            WebSocketClient webSocketClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(webSocketClient);
            _client.Initialize(credentials, _config.TwitchChannelName);

            _client.OnLog += ClientOnLoggin;
            _client.OnJoinedChannel += ClientOnJoinedChannel;
            _client.OnConnected += ClientOnConnected;
            _client.OnChatCommandReceived += ClientOnChatCommandReceived;
            _client.OnIncorrectLogin += ClientOnIncorrectLogin;
            _client.OnError += ClientOnError;
            _client.OnDisconnected += ClientOnDisconnected;

            _messageStack = new ConcurrentStack<string>();

            if (!IsConnected)
            {
                IsConnectionPending = true;
                _client.Connect();
            }
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (IsConnected) _client.Disconnect();
            _messageStack?.Clear();
            IsLogged = false;
            IsConnectionPending = false;
        }

        private bool IsConfigValid(TwitchConfigVariables config)
        {
            if (config.TwitchChannelName == null || config.TwitchChannelName == DEFAULT_TWITCH_NAME)
            {
                TootTallyNotifManager.DisplayError("Twitch Username is empty. Please fill it in.");
                return false;
            }
            if (config.AccessToken == null || config.AccessToken == DEFAULT_ACCESS_TOKEN)
            {
                TootTallyNotifManager.DisplayError("Twitch Access Token is empty. Please fill it in.");
                return false;
            }
            return true;
        }

        private void ClientOnChatCommandReceived(object sender, OnChatCommandReceivedArgs args) 
        {
            OnChatCommandReceived?.Invoke(sender, args);
        }

        private void ClientOnError(object sender, OnErrorEventArgs args)
        {
            Plugin.LogError($"{args.Exception}\n{args.Exception.StackTrace}");
            OnError?.Invoke(sender, args);
        }

        private void ClientOnIncorrectLogin(object sender, OnIncorrectLoginArgs args)
        {
            TootTallyNotifManager.DisplayError("Login credentials incorrect. Please re-authorize or refresh your access token, and re-check your Twitch username.");
            OnIncorrectLogin?.Invoke(sender, args);
            Disconnect();
        }

        private void ClientOnLoggin(object sender, OnLogArgs e)
        {
            Plugin.LogDebug($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            IsLogged = true;
            OnLoggin?.Invoke(sender, e);
        }

        private void ClientOnConnected(object sender, OnConnectedArgs e)
        {
            IsConnectionPending = false;
            Plugin.LogInfo($"Connected to {e.AutoJoinChannel}");
            OnConnected?.Invoke(sender, e);
        }

        private void ClientOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            HasJoinedChannel = true;
            _client.SendMessage(e.Channel, "! TootTally Twitch Integration ready!");
            TootTallyNotifManager.DisplayNotif("Twitch Integration successful!");
            Plugin.LogInfo("Twitch integration successfully attached to chat!");
            OnJoinedChannel?.Invoke(sender, e);
        }

        private void ClientOnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            IsConnectionPending = false;
            IsLogged = false;
            HasJoinedChannel = false;
            Plugin.LogInfo("Successfully disconnected from Twitch!");
            OnDisconnected?.Invoke(sender, e);
        }
    }
}
