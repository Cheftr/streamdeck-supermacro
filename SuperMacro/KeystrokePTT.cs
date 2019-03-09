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
    public class KeystrokePTT : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.Command = String.Empty; ;
                return instance;
            }

            [JsonProperty(PropertyName = "command")]
            public string Command { get; set; }
        }

        #region Private members

        private bool keyPressed = false;
        private PluginSettings settings;
        private InputSimulator iis = new InputSimulator();

        #endregion

        #region Public Methods

        public KeystrokePTT(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            try
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed {this.GetType()}");
                keyPressed = true;

                if (string.IsNullOrEmpty(settings.Command))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Command not configured");
                    return;
                }

                if (settings.Command.Length == 1)
                {
                    Task.Run(() => SimulateTextEntry(settings.Command[0]));
                }
                else // KeyStroke
                {
                    List<VirtualKeyCode> keyStrokes = CommandTools.ExtractKeyStrokes(settings.Command, true);

                    // Actually initiate the keystrokes
                    if (keyStrokes.Count > 0)
                    {
                        VirtualKeyCode keyCode = keyStrokes.Last();
                        keyStrokes.Remove(keyCode);

                        if (keyStrokes.Count > 0)
                        {
                            Task.Run(() => SimulateKeyStroke(keyStrokes.ToArray(), keyCode));
                        }
                        else
                        {
                            Task.Run(() => SimulateKeyDown(keyCode));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"PTT KeyPress Exception: {ex}");
            }
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
            HandleCommand();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        public override void OnTick() { }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        #endregion

        #region Private Methods

        private void HandleCommand()
        {
            if (String.IsNullOrEmpty(settings.Command))
            {
                return;
            }

            if (settings.Command.Length == 1) // 1 Character is fine
            {
                return;
            }

            string macro = CommandTools.ExtractMacro(settings.Command, 0);
            if (string.IsNullOrEmpty(macro)) // Not a macro, save only first character
            {
                settings.Command = settings.Command[0].ToString();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                if (settings.Command != macro) // Save only one keystroke
                {
                    settings.Command = macro;
                    Connection.SetSettingsAsync(JObject.FromObject(settings));
                }
            }
        }

        private void SimulateKeyDown(VirtualKeyCode keyCode)
        {
            while (keyPressed)
            {
                iis.Keyboard.KeyDown(keyCode);
                Thread.Sleep(30);
            }
        }

        private void SimulateKeyStroke(VirtualKeyCode[] keyStrokes, VirtualKeyCode keyCode)
        {
            while (keyPressed)
            {
                iis.Keyboard.ModifiedKeyStroke(keyStrokes, keyCode);
                Thread.Sleep(30);
            }
        }

        private void SimulateTextEntry(char character)
        {
            while (keyPressed)
            {
                iis.Keyboard.TextEntry(character);
                Thread.Sleep(30);
            }
        }

        #endregion
    }
}
