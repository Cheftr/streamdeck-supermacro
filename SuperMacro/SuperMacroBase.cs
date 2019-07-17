using BarRaider.SdTools;
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
    public abstract class SuperMacroBase : PluginBase
    {
        #region Protected Members

        protected bool inputRunning = false;
        protected bool forceStop = false;
        protected MacroSettingsBase settings;

        #endregion

        public SuperMacroBase(SDConnection connection, InitialPayload payload) : base(connection, payload) { }

        #region PluginBase Methods

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #endregion

        protected async void SendInput(string inputText)
        {
            inputRunning = true;
            await Task.Run(() =>
            {
                InputSimulator iis = new InputSimulator();
                string text = inputText;

                if (settings.EnterMode)
                {
                    text = text.Replace("\r\n", "\n");
                }

                for (int idx = 0; idx < text.Length && !forceStop; idx++)
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
                            InputChar(iis, text[idx]);
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
                        InputChar(iis, text[idx]);
                    }
                    Thread.Sleep(settings.Delay);
                }
            });
            inputRunning = false;
        }

        protected void HandleMacro(string macro)
        {
            List<VirtualKeyCodeContainer> keyStrokes = CommandTools.ExtractKeyStrokes(macro);

            // Actually initiate the keystrokes
            if (keyStrokes.Count > 0)
            {
                InputSimulator iis = new InputSimulator();
                VirtualKeyCodeContainer keyCode = keyStrokes.Last();
                keyStrokes.Remove(keyCode);

                if (keyStrokes.Count > 0)
                {
                    if (settings.KeydownDelay)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} DelayedModifiedKeyStroke");
                        iis.Keyboard.DelayedModifiedKeyStroke(keyStrokes.Select(ks => ks.KeyCode).ToArray(), new VirtualKeyCode[] { keyCode.KeyCode }, settings.Delay);
                    }
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} ModifiedKeyStroke");
                        iis.Keyboard.ModifiedKeyStroke(keyStrokes.Select(ks => ks.KeyCode).ToArray(), keyCode.KeyCode);
                    }
                }
                else // Single Keycode
                {
                    if (keyCode.IsExtended)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} HandleExtendedMacro");
                        ExtendedMacroHandler.HandleExtendedMacro(iis, keyCode);
                    }
                    else // Normal single keycode
                    {
                        if (!MouseHandler.HandleMouseMacro(iis, keyCode.KeyCode))
                        {
                            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} KeyPress");
                            iis.Keyboard.KeyPress(keyCode.KeyCode);
                        }
                    }
                }
            }
        }

        protected void InputChar(InputSimulator iis, char c)
        {
            if (settings.ForcedMacro)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} InputChar ForcedMacro");
                VirtualKeyCode vk = VirtualKeyCode.LBUTTON;
                iis.Keyboard.KeyPress(vk.FromChar(c));
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} InputChar TextEntry");
                iis.Keyboard.TextEntry(c);
            }
        }
    }
}
