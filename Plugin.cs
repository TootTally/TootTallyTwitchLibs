using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyTwitchLibs
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string PERSISTENT_CONFIG_NAME = "TootTallyTwitchIntegration.cfg";
        public const string DEFAULT_TWITCH_NAME = "ChannelName";
        public const string DEFAULT_ACCESS_TOKEN = "";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public static TootTallySettingPage settingPage;
        private static TwitchIntegrationController _twitchController;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);
        public static void LogDebug(string msg) => Instance.Logger.LogDebug(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "TootTallyTwitchLibs", true, "TootTally's twitch API implementation.");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            ConfigVariables = FileHelper.LoadFromTootTallyAppData<TwitchConfigVariables>(PERSISTENT_CONFIG_NAME);
            if (ConfigVariables == null)
            {
                ConfigVariables = new TwitchConfigVariables();
                ConfigVariables.SetTwitchChannelName(DEFAULT_TWITCH_NAME, false);
                ConfigVariables.SetAccessToken(DEFAULT_ACCESS_TOKEN);
            }
            else
            {
                Plugin.LogInfo($"Config file found with {ConfigVariables.AccessToken} and {ConfigVariables.TwitchChannelName}");
            }

            _twitchController = gameObject.AddComponent<TwitchIntegrationController>();
            settingPage = TootTallySettingsManager.AddNewPage(new TwitchConfigsSettingPage(_twitchController));
            TootTallySettings.Plugin.TryAddThunderstoreIconToPageButton(Instance.Info.Location, Name, settingPage);
            //_harmony.PatchAll(typeof(TwitchIntegrationManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            //_harmony.UnpatchSelf();
            if (_twitchController != null)
                GameObject.DestroyImmediate(_twitchController);
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public TwitchConfigVariables ConfigVariables { get; set; }

        [Serializable]
        public class TwitchConfigVariables
        {
            public string TwitchChannelName { get; set; }
            public string AccessToken { get; set; }

            public void SetTwitchChannelName(string name, bool saveToFile = true)
            {
                TwitchChannelName = name;
                if (saveToFile)
                    FileHelper.SaveToTootTallyAppData(PERSISTENT_CONFIG_NAME, this);
            }

            public void SetAccessToken(string accessToken, bool saveToFile = true)
            {
                AccessToken = accessToken;
                if (saveToFile)
                    FileHelper.SaveToTootTallyAppData(PERSISTENT_CONFIG_NAME, this);
            }
        }
    }
}