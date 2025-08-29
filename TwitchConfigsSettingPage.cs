using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallySettings;
using TootTallySettings.TootTallySettingsObjects;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyTwitchLibs
{
    public class TwitchConfigsSettingPage : TootTallySettingPage
    {
        private static TwitchLibsController _twitchController;
        private TootTallySettingTextField _channelNameInput, _accessTokenInput;
        private TootTallySettingButton _connectButton, _disconnectButton;
        private const string TOOTTALLY_TWITCH_LINK = "https://toottally.com/twitch/";

        public TwitchConfigsSettingPage(TwitchLibsController controller) : base("Twitch Configs", "Twitch Configs", 40f, new Color(0, 0, 0, .1f), GetButtonColors)
        {
            _twitchController = controller;
            _twitchController.OnConnected += OnConnectShowDisconnectButton;
            _twitchController.OnDisconnected += OnDisconnectShowConnectButton;

            AddLabel("Twitch Channel Name");
            _channelNameInput = AddTextField("Twitch Channel Name", Plugin.Instance.ConfigVariables.TwitchChannelName, false, OnTwitchChannelNameEditSaveToPersistentTootTallyFile);
            AddLabel("Access Token");
            _accessTokenInput = AddTextField("Access Token", Plugin.Instance.ConfigVariables.AccessToken, true, OnAccessTokenEditSaveToPersistentTootTallyFile);
            _connectButton = AddButton("Connect to Channel", OnConnectToChannelButtonPress);
            _disconnectButton = AddButton("Disconnect from Channel", OnDisconnectFromChannelButtonPress);
            AddButton("Get Access Token", OnGetAccessTokenButtonPress);
            AddToggle("Enable Debug Mode", Plugin.Instance.DebugMode);
        }

        public override void Initialize()
        {
            base.Initialize();
            //_channelNameInput.inputField.text = Plugin.Instance.ConfigVariables.TwitchChannelName;
            //_accessTokenInput.inputField.text = Plugin.Instance.ConfigVariables.AccessToken;
            _twitchController.Init();
            if (_twitchController.IsConnected)
                OnConnectShowDisconnectButton(null, null);
        }

        private static ColorBlock GetButtonColors => new ColorBlock()
        {
            normalColor = new Color(.57f, .0f, 1f),
            highlightedColor = new Color(.53f, 0f, .9f),
            pressedColor = new Color(.25f, 0f, .5f),
            selectedColor = new Color(.57f, .0f, 1f),
            fadeDuration = .08f,
            colorMultiplier = 1
        };

        public void OnTwitchChannelNameEditSaveToPersistentTootTallyFile(string text) => Plugin.Instance.ConfigVariables.SetTwitchChannelName(text);
        public void OnAccessTokenEditSaveToPersistentTootTallyFile(string text) => Plugin.Instance.ConfigVariables.SetAccessToken(text);

        public void OnConnectShowDisconnectButton(object sender, EventArgs e)
        {
            _disconnectButton.button.gameObject.SetActive(true);
            _connectButton.button.gameObject.SetActive(false);
        }
        public void OnDisconnectShowConnectButton(object sender, EventArgs e)
        {
            _disconnectButton.button.gameObject.SetActive(false);
            _connectButton.button.gameObject.SetActive(true);
        }

        public void OnConnectToChannelButtonPress()
        {
            if (!_twitchController.IsReady && !_twitchController.IsConnectionPending)
            {
                _twitchController.Init();
            }
        }

        public void OnDisconnectFromChannelButtonPress()
        {
            if (_twitchController.IsConnected && !_twitchController.IsConnectionPending)
                _twitchController.Disconnect();
        }

        public void OnGetAccessTokenButtonPress()
        {
            Application.OpenURL(TOOTTALLY_TWITCH_LINK);
        }
    }
}
