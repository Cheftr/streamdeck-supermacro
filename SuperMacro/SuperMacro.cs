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
    [PluginActionId("com.barraider.supermacro")]
    public class SuperMacro : SuperMacroBase
    {
        protected class PluginSettings : MacroSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.InputText = String.Empty; ;
                instance.Delay = 10;
                instance.EnterMode = false;
                instance.ForcedMacro = false;
                instance.KeydownDelay = false;

                return instance;
            }
        }

        protected PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert MacroSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Public Methods

        public SuperMacro(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                Settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");
            if (inputRunning)
            {
                forceStop = true;
                return;
            }

            forceStop = false;
            SendInput(Settings.InputText);
        }

       
        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            bool prevKeydownDelay = Settings.KeydownDelay;
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings loaded: {payload.Settings}");

            if (Settings.KeydownDelay && !prevKeydownDelay && Settings.Delay < CommandTools.RECOMMENDED_KEYDOWN_DELAY)
            {
                Settings.Delay = CommandTools.RECOMMENDED_KEYDOWN_DELAY;
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
        }

        #endregion
    }
}
