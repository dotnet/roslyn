// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Roslyn.VisualStudio.Test.Utilities.Interop;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public partial class EditorWindow
    {
        public enum VirtualKey : byte
        {
            Enter = 0x0D,
            Tab = 0x09,
            Escape = 0x1B,

            PageUp = 0x21,
            PageDown = 0x22,
            End = 0x23,
            Home = 0x24,
            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,

            Shift = 0x10,
            Control = 0x11,
            Alt = 0x12,

            CapsLock = 0x14,
            NumLock = 0x90,
            ScrollLock = 0x91,
            PrintScreen = 0x2C,
            Break = 0x03,
            Help = 0x2F,

            Backspace = 0x08,
            Clear = 0x0C,
            Insert = 0x2D,
            Delete = 0x2E,

            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
            F13 = 0x7C,
            F14 = 0x7D,
            F15 = 0x7E,
            F16 = 0x7F
        }

        [Flags]
        public enum ShiftState : byte
        {
            Shift = 1,
            Ctrl = 1 << 1,
            Alt = 1 << 2,
            Hankaku = 1 << 3,
            Reserved1 = 1 << 4,
            Reserved2 = 1 << 5
        }

        public async Task TypeTextAsync(string text, int wordsPerMinute = 120)
        {
            Activate();

            var normalizedText = text
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");

            var charactersPerSecond = (wordsPerMinute * 4.5) / 60;
            var delayBetweenCharacters = (int)((1 / charactersPerSecond) * 1000);

            foreach (var character in normalizedText)
            {
                SendKey(character);

                await Task.Delay(delayBetweenCharacters);
            }
        }

        public void SendKey(VirtualKey virtualKey, ShiftState shiftState = 0)
        {
            var foregroundWindow = IntPtr.Zero;
            var inputBlocked = false;

            try
            {
                inputBlocked = IntegrationHelper.BlockInput();
                foregroundWindow = IntegrationHelper.GetForegroundWindow();

                _visualStudioInstance.IntegrationService.Execute(
                    type: typeof(RemotingHelper),
                    methodName: nameof(RemotingHelper.ActivateMainWindow));

                if ((shiftState & ShiftState.Shift) != 0)
                {
                    SendKey(VirtualKey.Shift, User32.KEYEVENTF_NONE);
                }

                if ((shiftState & ShiftState.Ctrl) != 0)
                {
                    SendKey(VirtualKey.Control, User32.KEYEVENTF_NONE);
                }

                if ((shiftState & ShiftState.Alt) != 0)
                {
                    SendKey(VirtualKey.Alt, User32.KEYEVENTF_NONE);
                }

                SendKeyPressAndRelease(virtualKey);

                if ((shiftState & ShiftState.Shift) != 0)
                {
                    SendKey(VirtualKey.Shift, User32.KEYEVENTF_KEYUP);
                }

                if ((shiftState & ShiftState.Ctrl) != 0)
                {
                    SendKey(VirtualKey.Control, User32.KEYEVENTF_KEYUP);
                }

                if ((shiftState & ShiftState.Alt) != 0)
                {
                    SendKey(VirtualKey.Alt, User32.KEYEVENTF_KEYUP);
                }
            }
            finally
            {
                if (foregroundWindow != IntPtr.Zero)
                {
                    IntegrationHelper.SetForegroundWindow(foregroundWindow);
                }

                if (inputBlocked)
                {
                    IntegrationHelper.UnblockInput();
                }
            }
        }

        private void SendKey(char character)
        {
            var result = User32.VkKeyScan(character);
            if (result == -1)
            {
                SendUnicodeCharacter(character);
                return;
            }

            var virtualKeyCode = (VirtualKey)(result & 0xff);
            var shiftState = (ShiftState)(((ushort)result >> 8) & 0xff);

            SendKey(virtualKeyCode, shiftState);
        }

        private static bool IsExtendedKey(VirtualKey virtualKey)
        {
            return ((virtualKey >= VirtualKey.PageUp) && (virtualKey <= VirtualKey.Down))
                || (virtualKey == VirtualKey.Insert)
                || (virtualKey == VirtualKey.Delete);
        }

        private void SendKeyPressAndRelease(VirtualKey virtualKey)
        {
            SendKey(virtualKey, User32.KEYEVENTF_NONE);
            SendKey(virtualKey, User32.KEYEVENTF_KEYUP);
        }

        private void SendKey(VirtualKey virtualKey, uint dwFlags)
        {
            var input = new User32.INPUT
            {
                Type = User32.INPUT_KEYBOARD,
                ki = new User32.KEYBDINPUT
                {
                    wVk = (ushort)virtualKey,
                    wScan = 0,
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            if (IsExtendedKey(virtualKey))
            {
                input.ki.dwFlags |= User32.KEYEVENTF_EXTENDEDKEY;
            }

            IntegrationHelper.SendInput(new[] { input });
        }

        private void SendUnicodeCharacter(char character)
        {
            var keyDownInput = new User32.INPUT
            {
                Type = User32.INPUT_KEYBOARD,
                ki = new User32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = User32.KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            var keyUpInput = new User32.INPUT
            {
                Type = User32.INPUT_KEYBOARD,
                ki = new User32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = User32.KEYEVENTF_UNICODE | User32.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            IntegrationHelper.SendInput(new[] { keyDownInput, keyUpInput });
        }
    }
}
