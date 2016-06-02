// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities.Interop;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities.Input
{
    public class SendKeys
    {
        private readonly VisualStudioInstance _visualStudioInstance;

        public SendKeys(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;
        }

        public void Send(KeyPress keyPress)
        {
            Send(keyPress.VirtualKey, keyPress.ShiftState);
        }

        public void Send(VirtualKey virtualKey, ShiftState shiftState = 0)
        {
            var foregroundWindow = IntPtr.Zero;
            var inputBlocked = false;

            try
            {
                inputBlocked = IntegrationHelper.BlockInput();
                foregroundWindow = IntegrationHelper.GetForegroundWindow();

                _visualStudioInstance.ExecuteInHostProcess(
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

        public void Send(char character)
        {
            var result = User32.VkKeyScan(character);
            if (result == -1)
            {
                SendUnicodeCharacter(character);
                return;
            }

            var virtualKeyCode = (VirtualKey)(result & 0xff);
            var shiftState = (ShiftState)(((ushort)result >> 8) & 0xff);

            Send(virtualKeyCode, shiftState);
        }

        private static bool IsExtendedKey(VirtualKey virtualKey)
        {
            return (virtualKey >= VirtualKey.PageUp && virtualKey <= VirtualKey.Down)
                || virtualKey == VirtualKey.Insert
                || virtualKey == VirtualKey.Delete;
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
