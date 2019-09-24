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
    internal static class ExtendedMacroHandler
    {
        private const int EXTENDED_MACRO_PAUSE = 0;
        private const int EXTENDED_MACRO_KEY_DOWN = 1;
        private const int EXTENDED_MACRO_KEY_UP = 2;
        private const int EXTENDED_MACRO_MOUSE_MOVE = 3;
        private const int EXTENDED_MACRO_MOUSE_POS = 4;
        private const int EXTENDED_MACRO_SCROLL_UP = 5;
        private const int EXTENDED_MACRO_SCROLL_DOWN = 6;
        private const int EXTENDED_MACRO_SCROLL_LEFT = 7;
        private const int EXTENDED_MACRO_SCROLL_RIGHT = 8;
        private const int EXTENDED_MACRO_MOUSE_LEFT_DOWN = 9;
        private const int EXTENDED_MACRO_MOUSE_LEFT_UP = 10;
        private const int EXTENDED_MACRO_MOUSE_RIGHT_DOWN = 11;
        private const int EXTENDED_MACRO_MOUSE_RIGHT_UP = 12;
        private const int EXTENDED_MACRO_MOUSE_MIDDLE_DOWN = 13;
        private const int EXTENDED_MACRO_MOUSE_MIDDLE_UP = 14;
        private const int EXTENDED_MACRO_VARIABLE_INPUT = 15;
        private const int EXTENDED_MACRO_VARIABLE_OUTPUT = 16;

        private static readonly string[] EXTENDED_COMMANDS_LIST = { "PAUSE", "KEYDOWN", "KEYUP", "MOUSEMOVE", "MOUSEPOS", "MSCROLLUP", "MSCROLLDOWN", "MSCROLLLEFT", "MSCROLLRIGHT", "MLEFTDOWN", "MLEFTUP", "MRIGHTDOWN", "MRIGHTUP", "MMIDDLEDOWN", "MMIDDLEUP", "INPUT", "OUTPUT" };

        private static readonly Dictionary<VirtualKeyCode, bool> dicRepeatKeydown = new Dictionary<VirtualKeyCode, bool>();
        private static readonly Dictionary<string, string> dicVariables = new Dictionary<string, string>();


        public static bool IsExtendedMacro(string macroText, out string macroCommand, out string extendedData)
        {
            extendedData = String.Empty;
            macroCommand = null;
            foreach (string command in EXTENDED_COMMANDS_LIST)
            {
                if (macroText.StartsWith(command))
                {
                    macroCommand = command;
                    if (macroText.Length > command.Length)
                    {
                        extendedData = macroText.Substring(command.Length);

                        // Handle ":"
                        if (extendedData.StartsWith(":"))
                        {
                            extendedData = extendedData.Substring(1);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static void HandleExtendedMacro(InputSimulator iis, VirtualKeyCodeContainer macro)
        {
            try
            {
                // Check if it's a pause command
                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_PAUSE])
                {
                    if (Int32.TryParse(macro.ExtendedData, out int pauseLength))
                    {
                        Thread.Sleep(pauseLength);
                        return;
                    }
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_KEY_DOWN] || macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_KEY_UP])
                {
                    string commandText = CommandTools.ConvertSimilarMacroCommands(macro.ExtendedData.ToUpperInvariant());
                    if (string.IsNullOrEmpty(commandText))
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"Extended Keydown/Keyup - Missing Command");
                        return;
                    }

                    if (!Enum.TryParse<VirtualKeyCode>(commandText, true, out VirtualKeyCode code))
                    {
                        if (commandText.Length > 1)
                        {
                            Logger.Instance.LogMessage(TracingLevel.WARN, $"Extended Keydown/Keyup Shrinking {commandText} to {commandText[0]}");
                        }
                        code = (VirtualKeyCode)commandText[0];
                    }

                    if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_KEY_DOWN])
                    {
                        RepeatKeyDown(iis, code);
                        //iis.Keyboard.KeyDown(code);
                    }
                    else
                    {
                        dicRepeatKeydown[code] = false;
                        //iis.Keyboard.KeyUp(code);
                    }

                    return;
                }

                // Variables
                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_VARIABLE_INPUT])
                {
                    using (InputBox input = new InputBox("Variable Input", $"Enter value for \"{macro.ExtendedData}\":"))
                    {
                        input.ShowDialog();

                        // Value exists (cancel button was NOT pressed)
                        if (!string.IsNullOrEmpty(input.Input))
                        {
                            dicVariables[macro.ExtendedData] = input.Input;
                        }
                    }
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_VARIABLE_OUTPUT])
                {
                    if (dicVariables.ContainsKey(macro.ExtendedData))
                    {
                        iis.Keyboard.TextEntry(dicVariables[macro.ExtendedData]);
                    }
                    else
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"Variable Output called for {macro.ExtendedData} without an Input beforehand");
                    }
                    return;
                }

                // Mouse Move commands
                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_MOVE] || macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_POS])
                {
                    string[] mousePos = macro.ExtendedData.Split(',');
                    if (mousePos.Length == 2)
                    {
                        if (Double.TryParse(mousePos[0], out double x))
                        {
                            if (Double.TryParse(mousePos[1], out double y))
                            {
                                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_POS])
                                {
                                    iis.Mouse.MoveMouseToPositionOnVirtualDesktop(x, y);
                                }
                                else
                                {
                                    iis.Mouse.MoveMouseBy((int)x, (int)y);
                                }

                            }
                        }
                    }
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_UP] || macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_DOWN])
                {
                    int direction = (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_UP]) ? 1 : -1;
                    iis.Mouse.VerticalScroll(direction);
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_LEFT] || macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_RIGHT])
                {
                    int direction = (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_SCROLL_RIGHT]) ? 1 : -1;
                    iis.Mouse.HorizontalScroll(direction);
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_LEFT_DOWN])
                {
                    iis.Mouse.LeftButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_LEFT_UP])
                {
                    iis.Mouse.LeftButtonUp();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_RIGHT_DOWN])
                {
                    iis.Mouse.RightButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_RIGHT_UP])
                {
                    iis.Mouse.RightButtonUp();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_MIDDLE_DOWN])
                {
                    iis.Mouse.MiddleButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_COMMANDS_LIST[EXTENDED_MACRO_MOUSE_MIDDLE_UP])
                {
                    iis.Mouse.MiddleButtonUp();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to parse extended macro: {macro?.ExtendedCommand} {macro?.ExtendedData} {ex}");
            }
        }

        private static void RepeatKeyDown(InputSimulator iis, VirtualKeyCode code)
        {
            dicRepeatKeydown[code] = true;

            Task.Run(() =>
            {
                while (dicRepeatKeydown[code])
                {
                    iis.Keyboard.KeyDown(code);
                    Thread.Sleep(30);
                }
                iis.Keyboard.KeyUp(code);
            });
        }
    }
}
