using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace SuperMacro
{
    [PluginActionId("com.barraider.supermacrotoggle")]
    public class SuperMacroToggle : SuperMacroBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.InputText = String.Empty; ;
                instance.SecondaryText = String.Empty;
                instance.PrimaryImageFilename = String.Empty;
                instance.SecondaryImageFilename = string.Empty;
                instance.Delay = 10;
                instance.EnterMode = false;

                return instance;
            }

            [JsonProperty(PropertyName = "inputText")]
            public string InputText { get; set; }

            [JsonProperty(PropertyName = "secondaryText")]
            public string SecondaryText { get; set; }

            [JsonProperty(PropertyName = "delay")]
            public int Delay { get; set; }

            [JsonProperty(PropertyName = "enterMode")]
            public bool EnterMode { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "primaryImage")]
            public string PrimaryImageFilename { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "secondaryImage")]
            public string SecondaryImageFilename { get; set; }
        }

        #region Private members

        private PluginSettings settings;
        string primaryFile = null;
        string secondaryFile = null;
        bool isPrimary = false;

        #endregion

        #region Public Methods

        public SuperMacroToggle(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");
            if (inputRunning)
            {
                return;
            }

            isPrimary = !isPrimary;
            string text = isPrimary ? settings.InputText : settings.SecondaryText;
            SendInput(text, settings.Delay, settings.EnterMode);
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public async override void OnTick()
        {
            string imgBase64;
            if (isPrimary)
            {
                imgBase64 = Properties.Settings.Default.TogglePrimary;

                if (!String.IsNullOrWhiteSpace(primaryFile))
                {
                    imgBase64 = primaryFile;
                }
                await Connection.SetImageAsync(imgBase64);
            }
            else
            {
                imgBase64 = Properties.Settings.Default.ToggleSecondary;
                if (!String.IsNullOrWhiteSpace(secondaryFile))
                {
                    imgBase64 = secondaryFile;
                }
                await Connection.SetImageAsync(imgBase64);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings loaded: {payload.Settings}");
            HandleFilenames();
        }

        #endregion

        #region Private Methods

        private void HandleFilenames()
        {
            primaryFile = Tools.FileToBase64(settings.PrimaryImageFilename, true);
            secondaryFile = Tools.FileToBase64(settings.SecondaryImageFilename, true);
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion

    }
}
