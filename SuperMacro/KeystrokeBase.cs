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
    public abstract class KeystrokeBase : PluginBase
    {
        protected abstract class PluginSettingsBase
        {
            [JsonProperty(PropertyName = "command")]
            public string Command { get; set; }

            [JsonProperty(PropertyName = "forcedKeydown")]
            public bool ForcedKeydown { get; set; }
        }

        #region Private Members

        private readonly InputSimulator iis = new InputSimulator();

        #endregion


        #region Protected Members

        protected bool keyPressed = false;
        protected PluginSettingsBase settings;

        #endregion

        #region Public Methods

        public KeystrokeBase(SDConnection connection, InitialPayload payload) : base(connection, payload) { }

        public virtual Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        protected void RunCommand()
        {
            try
            {
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
                    List<VirtualKeyCodeContainer> keyStrokes = CommandTools.ExtractKeyStrokes(settings.Command);

                    // Actually initiate the keystrokes
                    if (keyStrokes.Count > 0)
                    {
                        VirtualKeyCodeContainer keyCode = keyStrokes.Last();
                        keyStrokes.Remove(keyCode);

                        if (keyStrokes.Count > 0)
                        {
                            Task.Run(() => SimulateKeyStroke(keyStrokes.Select(ks => ks.KeyCode).ToArray(), keyCode.KeyCode));
                        }
                        else
                        {
                            if (keyCode.IsExtended)
                            {
                                Task.Run(() => SimulateExtendedMacro(keyCode));
                            }
                            else
                            {
                                Task.Run(() => SimulateKeyDown(keyCode.KeyCode));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RunCommand Exception: {ex}");
            }
}

        protected void HandleKeystroke()
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
                SaveSettings();
            }
            else
            {
                if (settings.Command != macro) // Save only one keystroke
                {
                    settings.Command = macro;
                    SaveSettings();
                }
            }
        }

        public override void Dispose()
        {
            keyPressed = false;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        #endregion

        #region Private Methods

        private void SimulateKeyDown(VirtualKeyCode keyCode)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} SimulateKeyDown");
            while (keyPressed)
            {
                if (!MouseHandler.HandleMouseMacro(iis, keyCode))
                {
                    iis.Keyboard.KeyDown(keyCode);
                }
                Thread.Sleep(30);
            }
            iis.Keyboard.KeyUp(keyCode); // Release key at the end
        }

        private void SimulateKeyStroke(VirtualKeyCode[] keyStrokes, VirtualKeyCode keyCode)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} SimulateKeyStroke. ForcedKeyDown: {settings.ForcedKeydown}");
            while (keyPressed)
            {
                if (settings.ForcedKeydown)
                {
                    foreach(var keystroke in keyStrokes)
                    {
                        iis.Keyboard.KeyDown(keystroke);
                    }
                    iis.Keyboard.KeyDown(keyCode);
                }
                else
                {
                    iis.Keyboard.ModifiedKeyStroke(keyStrokes, keyCode);
                }
                Thread.Sleep(30);
            }

            if (settings.ForcedKeydown)
            {
                iis.Keyboard.KeyUp(keyCode);
                foreach (var keystroke in keyStrokes)
                {
                    iis.Keyboard.KeyUp(keystroke);
                }
            }
        }

        private void SimulateExtendedMacro(VirtualKeyCodeContainer keyCode)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} SimulateExtendedMacro");
            while (keyPressed)
            {
                ExtendedMacroHandler.HandleExtendedMacro(iis, keyCode);
                Thread.Sleep(30);
            }
        }

        private void SimulateTextEntry(char character)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} SimulateTextEntry");
            while (keyPressed)
            {
                iis.Keyboard.TextEntry(character);
                Thread.Sleep(30);
            }
        }

        #endregion
    }
}
