using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace SuperMacro
{
    [PluginActionId("com.barraider.keystrokeptt")]
    public class KeystrokePTT : KeystrokeBase
    {
        private class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.Command = String.Empty; ;
                return instance;
            }
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

        #region Public Methods

        public KeystrokePTT(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");
            keyPressed = true;
            RunCommand();
        }

        public override void KeyReleased(KeyPayload payload)
        {
            keyPressed = false;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Released {this.GetType()}");
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings loaded: {payload.Settings}");
            HandleKeystroke();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void OnTick() { }

        public override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}
