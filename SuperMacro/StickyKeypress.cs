using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMacro
{
    [PluginActionId("com.barraider.supermacrostickykeystroke")]
    public class StickyKeypress : KeystrokeBase
    {
        private class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Command = String.Empty,
                    EnabledImageFilename = string.Empty,
                    DisabledImageFilename = string.Empty,
                    ForcedKeydown = false
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "enabledImage")]
            public string EnabledImageFilename { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "disabledImage")]
            public string DisabledImageFilename { get; set; }
        }

        private PluginSettings Settings
        {
            get
            { 
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Private Members

        string enabledFile = null;
        string disabledFile = null;

        #endregion

        #region Public Methods

        public StickyKeypress(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                Settings = payload.Settings.ToObject<PluginSettings>();
                HandleFilenames();
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");

            keyPressed = !keyPressed;
            if (keyPressed)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Command Started");
                RunCommand();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings loaded: {payload.Settings}");
            HandleFilenames();
            HandleKeystroke();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public async override void OnTick()
        {
            string imgBase64;
            if (keyPressed)
            {
                imgBase64 = Properties.Settings.Default.StickyEnabled;

                if (!String.IsNullOrWhiteSpace(enabledFile))
                {
                    imgBase64 = enabledFile;
                }
                await Connection.SetImageAsync(imgBase64);
            }
            else
            {
                imgBase64 = Properties.Settings.Default.StickyDisabled;
                if (!String.IsNullOrWhiteSpace(disabledFile))
                {
                    imgBase64 = disabledFile;
                }
                await Connection.SetImageAsync(imgBase64);
            }
        }

        public override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion

        private void HandleFilenames()
        {
            enabledFile = Tools.FileToBase64(Settings.EnabledImageFilename, true);
            disabledFile = Tools.FileToBase64(Settings.DisabledImageFilename, true);
            SaveSettings();
        }
    }
}