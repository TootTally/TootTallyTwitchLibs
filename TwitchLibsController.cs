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
    public class TwitchLibsController : MonoBehaviour
    {
        public bool IsConnectionPending { get; private set; }
        public bool HasJoinedChannel { get; private set; }
        public string ChannelName;
        public bool IsConnected => Client != null && Client.IsConnected;
        public bool IsReady => IsConnected && HasJoinedChannel;

        public TwitchClient Client;
        private WebSocketClient _websocketClient;
        private Plugin.TwitchConfigVariables _config;

        public Action OnUpdate;
        public Action<TwitchLibsController, OnLogArgs> OnLoggin;
        public Action<TwitchLibsController, OnJoinedChannelArgs> OnJoinedChannel;
        public Action<TwitchLibsController, OnConnectedArgs> OnConnected;
        public Action<TwitchLibsController, OnChatCommandReceivedArgs> OnChatCommandReceived;
        public Action<TwitchLibsController, OnIncorrectLoginArgs> OnIncorrectLogin;
        public Action<TwitchLibsController, OnErrorEventArgs> OnError;
        public Action<TwitchLibsController, OnDisconnectedEventArgs> OnDisconnected;

        public void Awake()
        {
            if (Client != null) return;
            var clientOptions = new ClientOptions()
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(reconnectInterval: 5, maxAttempts: 0),
            };
            _websocketClient = new WebSocketClient(clientOptions);
            Client = new TwitchClient(_websocketClient);
            Client.OnLog += ClientOnLoggin;
            Client.OnJoinedChannel += ClientOnJoinedChannel;
            Client.OnConnected += ClientOnConnected;
            Client.OnChatCommandReceived += ClientOnChatCommandReceived;
            Client.OnIncorrectLogin += ClientOnIncorrectLogin;
            Client.OnError += ClientOnError;
            Client.OnDisconnected += ClientOnDisconnected;
            Plugin.LogDebug("Client setup done.");
        }

        public void Init()
        {
            IsConnectionPending = false;
            HasJoinedChannel = false;
            _websocketClient.Open();
            _config = Plugin.Instance.ConfigVariables;
            if (!IsConfigValid(_config)) return;

            Client.Initialize(new ConnectionCredentials(_config.TwitchChannelName, _config.AccessToken), _config.TwitchChannelName);
            Plugin.LogDebug("Client is being initialized.");
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
            Client.Connect();
        }

        public void Disconnect()
        {
            if (IsConnected) Client.Disconnect();
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
            OnChatCommandReceived?.Invoke(this, args);
        }

        private void ClientOnError(object sender, OnErrorEventArgs args)
        {
            Plugin.LogError($"{args.Exception}\n{args.Exception.StackTrace}");
            OnError?.Invoke(this, args);
        }

        private void ClientOnIncorrectLogin(object sender, OnIncorrectLoginArgs args)
        {
            TootTallyNotifManager.DisplayError("Login credentials incorrect. Please re-authorize or refresh your access token, and re-check your Twitch username.");
            OnIncorrectLogin?.Invoke(this, args);
            Disconnect();
        }

        private void ClientOnLoggin(object sender, OnLogArgs e)
        {
            Plugin.LogDebug($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            OnLoggin?.Invoke(this, e);
        }

        private void ClientOnConnected(object sender, OnConnectedArgs e)
        {
            IsConnectionPending = false;
            Plugin.LogInfo($"Connected with {e.BotUsername}");
            OnConnected?.Invoke(this, e);
        }

        private void ClientOnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            HasJoinedChannel = true;
            ChannelName = e.Channel;
            Client.SendMessage(e.Channel, "! TootTally Twitch Integration ready!");
            TootTallyNotifManager.DisplayNotif("Twitch Integration successful!");
            Plugin.LogInfo("Twitch integration successfully attached to chat!");
            OnJoinedChannel?.Invoke(this, e);
        }

        private void ClientOnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            IsConnectionPending = false;
            HasJoinedChannel = false;
            ChannelName = "";
            Plugin.LogInfo("Successfully disconnected from Twitch!");
            OnDisconnected?.Invoke(this, e);
        }

        

        public void SendChannelMessage(string message)
        {
            if (!IsConnected) return;
            Client.SendMessage(ChannelName, message);
        }
    }
}
