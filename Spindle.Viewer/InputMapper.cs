/*
 * COPYRIGHT:   See LICENSE.txt in the top level directory
 * PROJECT:     Spindle.Viewer
 * FILE:        InputMapper.cs
 * PURPOSE:     Key converter to make our engine agnostic from wpf.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Windows.Input;
using FolkTales.Enums;

namespace Spindle.Viewer
{
    /// <summary>
    /// Key Converter.
    /// </summary>
    public static class InputMapper
    {
        /// <summary>
        /// Converts to gamekey.
        /// </summary>
        /// <param name="wpfKey">The WPF key.</param>
        /// <returns>Enum of key.</returns>
        public static GameKey ToGameKey(this Key wpfKey)
        {
            switch (wpfKey)
            {
                case Key.W:
                    return GameKey.W;
                case Key.A:
                    return GameKey.A;
                case Key.S:
                    return GameKey.S;
                case Key.D:
                    return GameKey.D;
                case Key.Space:
                    return GameKey.Space;
                case Key.Left:
                    return GameKey.Left;
                case Key.Right:
                    return GameKey.Right;
                case Key.Up:
                    return GameKey.Up;
                case Key.Down:
                    return GameKey.Down;
                case Key.Enter:
                    return GameKey.Enter;
                case Key.F1:
                    return GameKey.F1;
                case Key.F2:
                    return GameKey.F2;
                case Key.F3:
                    return GameKey.F3;
                case Key.F4:
                    return GameKey.F4;
                case Key.F5:
                    return GameKey.F5;
                case Key.F6:
                    return GameKey.F6;
                case Key.F7:
                    return GameKey.F7;
                case Key.F8:
                    return GameKey.F8;
                case Key.F9:
                    return GameKey.F9;
                case Key.F10:
                    return GameKey.F10;
                case Key.F11:
                    return GameKey.F11;
                case Key.F12:
                    return GameKey.F12;
                case Key.PageUp:
                    return GameKey.PageUp;
                case Key.PageDown:
                    return GameKey.PageDown;
                default:
                    return GameKey.Unknown;
            }
        }
    }
}