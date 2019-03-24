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

        #endregion

        public SuperMacroBase(SDConnection connection, InitialPayload payload) : base(connection, payload) { }

        #region PluginBase Methods

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #endregion

        protected async void SendInput(string inputText, int delay, bool enterMode)
        {
            inputRunning = true;
            await Task.Run(() =>
            {
                InputSimulator iis = new InputSimulator();
                string text = inputText;

                if (enterMode)
                {
                    text = text.Replace("\r\n", "\n");
                }

                for (int idx = 0; idx < text.Length; idx++)
                {
                    if (enterMode && text[idx] == '\n')
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

        protected void HandleMacro(string macro)
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
    }
}
