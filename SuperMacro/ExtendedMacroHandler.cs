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
        private const string EXTENDED_MACRO_PAUSE = "PAUSE";
        private const string EXTENDED_MACRO_KEY_DOWN = "KEYDOWN";
        private const string EXTENDED_MACRO_KEY_UP = "KEYUP";
        private const string EXTENDED_MACRO_MOUSE_MOVE = "MOUSEMOVE";
        private const string EXTENDED_MACRO_MOUSE_POS = "MOUSEPOS";
        private const string EXTENDED_MACRO_SCROLL_UP = "MSCROLLUP";
        private const string EXTENDED_MACRO_SCROLL_DOWN = "MSCROLLDOWN";
        private const string EXTENDED_MACRO_SCROLL_LEFT = "MSCROLLLEFT";
        private const string EXTENDED_MACRO_SCROLL_RIGHT = "MSCROLLRIGHT";
        private const string EXTENDED_MACRO_MOUSE_LEFT_DOWN = "MLEFTDOWN";
        private const string EXTENDED_MACRO_MOUSE_LEFT_UP = "MLEFTUP";
        private const string EXTENDED_MACRO_MOUSE_RIGHT_DOWN = "MRIGHTDOWN";
        private const string EXTENDED_MACRO_MOUSE_RIGHT_UP = "MRIGHTUP";
        private const string EXTENDED_MACRO_MOUSE_MIDDLE_DOWN = "MMIDDLEDOWN";
        private const string EXTENDED_MACRO_MOUSE_MIDDLE_UP = "MMIDDLEUP";
        private static readonly Dictionary<VirtualKeyCode, bool> dicRepeatKeydown = new Dictionary<VirtualKeyCode, bool>();


        public static bool IsExtendedMacro(string macroText, out string extendedData)
        {
            extendedData = String.Empty;
            if (macroText.StartsWith(EXTENDED_MACRO_PAUSE))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_PAUSE.Length);
                return true;
            }

            if (macroText.StartsWith(EXTENDED_MACRO_KEY_DOWN))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_KEY_DOWN.Length);
                return true;
            }

            if (macroText.StartsWith(EXTENDED_MACRO_KEY_UP))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_KEY_UP.Length);
                return true;
            }

            if (macroText.StartsWith(EXTENDED_MACRO_MOUSE_MOVE))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_MOUSE_MOVE.Length);
                return true;
            }

            if (macroText.StartsWith(EXTENDED_MACRO_MOUSE_POS))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_MOUSE_POS.Length);
                return true;
            }

            if (macroText.StartsWith(EXTENDED_MACRO_SCROLL_UP) ||
                macroText.StartsWith(EXTENDED_MACRO_SCROLL_DOWN) ||
                macroText.StartsWith(EXTENDED_MACRO_SCROLL_LEFT) ||
                macroText.StartsWith(EXTENDED_MACRO_SCROLL_RIGHT) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_LEFT_DOWN) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_LEFT_UP) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_RIGHT_DOWN) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_RIGHT_UP) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_MIDDLE_DOWN) ||
                macroText.StartsWith(EXTENDED_MACRO_MOUSE_MIDDLE_UP))
            {
                return true;
            }

            return false;
        }

        public static void HandleExtendedMacro(InputSimulator iis, VirtualKeyCodeContainer macro)
        {
            try
            {
                // Check if it's a pause command
                if (macro.ExtendedCommand == EXTENDED_MACRO_PAUSE)
                {
                    if (Int32.TryParse(macro.ExtendedData, out int pauseLength))
                    {
                        Thread.Sleep(pauseLength);
                        return;
                    }

                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_KEY_DOWN || macro.ExtendedCommand == EXTENDED_MACRO_KEY_UP)
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

                    if (macro.ExtendedCommand == EXTENDED_MACRO_KEY_DOWN)
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

                // Mouse Move commands
                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_MOVE || macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_POS)
                {
                    string[] mousePos = macro.ExtendedData.Split(',');
                    if (mousePos.Length == 2)
                    {
                        if (Double.TryParse(mousePos[0], out double x))
                        {
                            if (Double.TryParse(mousePos[1], out double y))
                            {
                                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_POS)
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

                if (macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_UP || macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_DOWN)
                {
                    int direction = (macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_UP) ? 1 : -1;
                    iis.Mouse.VerticalScroll(direction);
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_LEFT || macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_RIGHT)
                {
                    int direction = (macro.ExtendedCommand == EXTENDED_MACRO_SCROLL_RIGHT) ? 1 : -1;
                    iis.Mouse.HorizontalScroll(direction);
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_LEFT_DOWN)
                {
                    iis.Mouse.LeftButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_LEFT_UP)
                {
                    iis.Mouse.LeftButtonUp();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_RIGHT_DOWN)
                {
                    iis.Mouse.RightButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_RIGHT_UP)
                {
                    iis.Mouse.RightButtonUp();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_MIDDLE_DOWN)
                {
                    iis.Mouse.MiddleButtonDown();
                    return;
                }

                if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_MIDDLE_UP)
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
