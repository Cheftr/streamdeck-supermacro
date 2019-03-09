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
    public class SuperMacro : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.InputText = String.Empty; ;
                instance.Delay = 1;
                instance.EnterMode = false;

                return instance;
            }

            [JsonProperty(PropertyName = "inputText")]
            public string InputText { get; set; }

            [JsonProperty(PropertyName = "delay")]
            public int Delay { get; set; }

            [JsonProperty(PropertyName = "enterMode")]
            public bool EnterMode { get; set; }
        }

        #region Private members

        private const int RESET_COUNTER_KEYPRESS_LENGTH = 1;
        private bool inputRunning = false;
        private PluginSettings settings;

        #endregion

        #region Public Methods

        public SuperMacro(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            
            SendInput();
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override void OnTick()
        {
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings loaded: {payload.Settings}");
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        { }


        #endregion

        #region Private Methods

        private async void SendInput()
        {
            inputRunning = true;
            await Task.Run(() =>
            {
                InputSimulator iis = new InputSimulator();
                string text = settings.InputText;
                int delay = settings.Delay;

                if (settings.EnterMode)
                {
                    text = text.Replace("\r\n", "\n");
                }

                for (int idx = 0; idx < text.Length; idx++)
                {
                    if (settings.EnterMode && text[idx] == '\n')
                    {
                        iis.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                    }
                    else if (text[idx] == CommandTools.MACRO_START_CHAR)
                    {
                        string macro = CommandTools.ExtractMacro(text, idx);
                        if (String.IsNullOrWhiteSpace(macro)) // Not a macro, just input the character
                        {
                            iis.Keyboard.TextEntry(text[idx]);
                        }
                        else // This is a macro, run it
                        {
                            idx += macro.Length - 1;
                            macro = macro.Substring(1, macro.Length - 2);

                            HandleMacro(macro);

                        }
                    }
                    else
                    {
                        iis.Keyboard.TextEntry(text[idx]);
                    }
                    Thread.Sleep(delay);
                }
            });
            inputRunning = false;
        }

        private void HandleMacro(string macro)
        {
            List<VirtualKeyCode> keyStrokes = CommandTools.ExtractKeyStrokes(macro, true);

            // Actually initiate the keystrokes
            if (keyStrokes.Count > 0)
            {
                InputSimulator iis = new InputSimulator();
                VirtualKeyCode keyCode = keyStrokes.Last();
                keyStrokes.Remove(keyCode);

                if (keyStrokes.Count > 0)
                {
                    iis.Keyboard.ModifiedKeyStroke(keyStrokes.ToArray(), keyCode);
                }
                else
                {
                    iis.Keyboard.KeyPress(keyCode);
                }
            }
        }

       
        #endregion
    }
}
