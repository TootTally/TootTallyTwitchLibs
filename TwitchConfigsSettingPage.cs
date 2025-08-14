using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Utils.Helpers;
using TootTallySettings;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyTwitchLibs
{
    public class TwitchConfigsSettingPage : TootTallySettingPage
    {
        

        public TwitchConfigsSettingPage() : base("Twitch Configs", "Twitch Configs", 40f, new Color(0,0,0,.1f), GetButtonColors)
        {
            AddLabel("Twitch Channel Name");
            AddTextField("Twitch Channel Name", Plugin.Instance.ConfigVariables.TwitchChannelName, false, OnTwitchChannelNameEditSaveToPersistentTootTallyFile);
            AddLabel("Access Token");
            AddTextField("Access Token", Plugin.Instance.ConfigVariables.AccessToken, true, OnAccessTokenEditSaveToPersistentTootTallyFile);
        }

        private static ColorBlock GetButtonColors => new ColorBlock()
        {
            normalColor = new Color(.20f, .20f, 1),
            highlightedColor = new Color(.15f, .15f, .65f),
            pressedColor = new Color(.5f, .5f, .75f),
            selectedColor = new Color(.20f, .20f, 1),
            fadeDuration = .08f,
            colorMultiplier = 1
        };

        public void OnTwitchChannelNameEditSaveToPersistentTootTallyFile(string text) => Plugin.Instance.ConfigVariables.SetAccessToken(text);
        public void OnAccessTokenEditSaveToPersistentTootTallyFile(string text) => Plugin.Instance.ConfigVariables.SetTwitchChannelName(text);
    }
}
