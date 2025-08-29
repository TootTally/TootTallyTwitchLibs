using System;
using System.Collections.Concurrent;
using System.Linq;
using TootTallyCore.Utils.TootTallyNotifs;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using UnityEngine;

namespace TootTallyTwitchLibs
{
    public class TwitchIntegrationController : MonoBehaviour
    {
        public bool IsConnectionPending { get; private set; }
        public bool HasJoinedChannel { get; private set; }
        public bool IsConnected => _client != null && _client.IsConnected;
        public bool IsReady => IsConnected && HasJoinedChannel;

        private TwitchClient _client;
        private WebSocketClient _websocketClient;
        private Plugin.TwitchConfigVariables _config;
        private ConcurrentStack<string> _messageStack;

        public Action OnUpdate;
        public Action<object, OnLogArgs> OnLoggin;
        public Action<object, OnJoinedChannelArgs> OnJoinedChannel;
        public Action<object, OnConnectedArgs> OnConnected;
        public Action<object, OnChatCommandReceivedArgs> OnChatCommandReceived;
        public Action<object, OnIncorrectLoginArgs> OnIncorrectLogin;
        public Action<object, OnErrorEventArgs> OnError;
        public Action<object, OnDisconnectedEventArgs> OnDisconnected;

        public void Awake()
        {
            if (_client != null) return;
            var clientOptions = new ClientOptions()
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(reconnectInterval: 5, maxAttempts: 3),
            };
            _websocketClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(_websocketClient);
            _client.OnLog += ClientOnLoggin;
            _client.OnJoinedChannel += ClientOnJoinedChannel;
            _client.OnConnected += ClientOnConnected;
            _client.OnChatCommandReceived += ClientOnChatCommandReceived;
            _client.OnIncorrectLogin += ClientOnIncorrectLogin;
            _client.OnError += ClientOnError;
            _client.OnDisconnected += ClientOnDisconnected;
            Plugin.LogDebug("Client setup done.");
            _messageStack = new ConcurrentStack<string>();
        }

        public void Init()
        {
            IsConnectionPending = false;
            HasJoinedChannel = false;
            _config = Plugin.Instance.ConfigVariables;
            if (!IsConfigValid(_config)) return;

            if (_client.ConnectionCredentials != null && (_client.ConnectionCredentials.TwitchUsername != _config.TwitchChannelName || _client.ConnectionCredentials.TwitchOAuth != _config.AccessToken))
            {
                _client.SetConnectionCredentials(new ConnectionCredentials(_config.TwitchChannelName, _config.AccessToken));
                Plugin.LogDebug("New credentials are set.");
            }
            else if (_client.ConnectionCredentials == null)
            {
                _client.Initialize(new ConnectionCredentials(_config.TwitchChannelName, _config.AccessToken), _config.TwitchChannelName);
                Plugin.LogDebug("Client is being initialized.");
            }
            /*if (!_client.IsInitialized)
                _client.Initialize(credentials, _config.TwitchChannelName);
            else
            {
                _client.SetConnectionCredentials(credentials);
                if (!_client.JoinedChannels.Any(channelName => channelName.Channel == _config.TwitchChannelName))
                    _client.JoinChannel(_config.TwitchChannelName);
            }*/
            Connect();
        }

        public void Update()
        {
            OnUpdate?.Invoke();
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
            IsConnectionPending = true;
            Plugin.LogDebug("Client is trying to connect.");
            _client.Connect();
        }

        public void Disconnect()
        {
            if (IsConnected) _client.Disconnect();
            _messageStack?.Clear();
            IsConnectionPending = false;
        }

        private bool IsConfigValid(Plugin.TwitchConfigVariables config)
        {
            if (config.TwitchChannelName == null || config.TwitchChannelName == Plugin.DEFAULT_TWITCH_NAME)
            {
                TootTallyNotifManager.DisplayError("Twitch Username is empty. Please fill it in.");
                return false;
            }
            if (config.AccessToken == null || config.AccessToken == Plugin.DEFAULT_ACCESS_TOKEN)
            {
                TootTallyNotifManager.DisplayError("Twitch Access Token is empty. Please fill it in.");
                return false;
            }
            Plugin.LogDebug("Twitch Config is filled up.");
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
            OnLoggin?.Invoke(sender, e);
        }

        private void ClientOnConnected(object sender, OnConnectedArgs e)
        {
            IsConnectionPending = false;
            Plugin.LogInfo($"Connected with {e.BotUsername}");
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
            HasJoinedChannel = false;
            Plugin.LogInfo("Successfully disconnected from Twitch!");
            OnDisconnected?.Invoke(sender, e);
        }
    }
}
