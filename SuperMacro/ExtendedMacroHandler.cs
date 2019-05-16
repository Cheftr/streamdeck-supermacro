using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace SuperMacro
{
    internal static class ExtendedMacroHandler
    {
        private const string EXTENDED_MACRO_PAUSE = "PAUSE";
        private const string EXTENDED_MACRO_MOUSE_MOVE = "MOUSEMOVE";
        private const string EXTENDED_MACRO_MOUSE_POS = "MOUSEPOS";

        public static bool IsExtendedMacro(string macroText, out string extendedData)
        {
            if (macroText.StartsWith(EXTENDED_MACRO_PAUSE))
            {
                extendedData = macroText.Substring(EXTENDED_MACRO_PAUSE.Length);
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

            extendedData = null;
            return false;
        }

        public static void HandleExtendedMacro(InputSimulator iis, VirtualKeyCodeContainer macro)
        {
            // Check if it's a pause command
            if (macro.ExtendedCommand == EXTENDED_MACRO_PAUSE)
            {
                int pauseLength;
                if (Int32.TryParse(macro.ExtendedData, out pauseLength))
                {
                    Thread.Sleep(pauseLength);
                    return;
                }

            }

            // Mouse Move commands
            if (macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_MOVE || macro.ExtendedCommand == EXTENDED_MACRO_MOUSE_POS)
            {
                string[] mousePos = macro.ExtendedData.Split(',');
                if (mousePos.Length == 2)
                {
                    double x;
                    double y;
                    if (Double.TryParse(mousePos[0], out x))
                    {
                        if (Double.TryParse(mousePos[1], out y))
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
            }
        }
    }
}
