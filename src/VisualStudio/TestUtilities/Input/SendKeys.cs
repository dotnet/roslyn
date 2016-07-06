// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.VisualStudio.Test.Utilities.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.Input
{
    public class SendKeys
    {
        private readonly VisualStudioInstance _visualStudioInstance;

        public SendKeys(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;
        }

        public void Send(object[] keys)
        {
            var inputs = new List<User32.INPUT>(keys.Length);

            foreach (var key in keys)
            {
                if (key is string)
                {
                    var text = ((string)key)
                        .Replace("\r\n", "\r")
                        .Replace("\n", "\r");

                    foreach (var ch in text)
                    {
                        AddInputs(inputs, ch);
                    }
                }
                else if (key is char)
                {
                    AddInputs(inputs, (char)key);
                }
                else if (key is VirtualKey)
                {
                    AddInputs(inputs, (VirtualKey)key);
                }
                else if (key is KeyPress)
                {
                    AddInputs(inputs, (KeyPress)key);
                }
                else if (key == null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }
                else
                {
                    throw new ArgumentException($"Unexpected type encountered: {key.GetType()}", nameof(keys));
                }
            }

            SendInputs(inputs.ToArray());
        }

        private static void AddInputs(List<User32.INPUT> inputs, char ch)
        {
            var result = User32.VkKeyScan(ch);
            if (result == -1)
            {
                // This is a unicode character that must be handled differently.

                AddUnicodeInputs(inputs, ch);
                return;
            }

            var virtualKey = (VirtualKey)(result & 0xff);
            var shiftState = (ShiftState)(((ushort)result >> 8) & 0xff);

            AddInputs(inputs, virtualKey, shiftState);
        }

        private static void AddUnicodeInputs(List<User32.INPUT> inputs, char ch)
        {
            var keyDownInput = new User32.INPUT
            {
                Type = User32.INPUT_KEYBOARD,
                ki = new User32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
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
                    wScan = ch,
                    dwFlags = User32.KEYEVENTF_UNICODE | User32.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            inputs.Add(keyDownInput);
            inputs.Add(keyUpInput);
        }

        private static void AddInputs(List<User32.INPUT> inputs, VirtualKey virtualKey, uint dwFlags)
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

            inputs.Add(input);
        }

        private static bool IsExtendedKey(VirtualKey virtualKey)
        {
            return (virtualKey >= VirtualKey.PageUp && virtualKey <= VirtualKey.Down)
                || virtualKey == VirtualKey.Insert
                || virtualKey == VirtualKey.Delete;
        }

        private static void AddInputs(List<User32.INPUT> inputs, KeyPress keyPress)
        {
            AddInputs(inputs, keyPress.VirtualKey, keyPress.ShiftState);
        }

        private static void AddInputs(List<User32.INPUT> inputs, VirtualKey virtualKey, ShiftState shiftState = 0)
        {
            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddInputs(inputs, VirtualKey.Shift, User32.KEYEVENTF_NONE);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddInputs(inputs, VirtualKey.Control, User32.KEYEVENTF_NONE);
            }

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddInputs(inputs, VirtualKey.Alt, User32.KEYEVENTF_NONE);
            }

            AddInputs(inputs, virtualKey, User32.KEYEVENTF_NONE);
            AddInputs(inputs, virtualKey, User32.KEYEVENTF_KEYUP);

            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddInputs(inputs, VirtualKey.Shift, User32.KEYEVENTF_KEYUP);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddInputs(inputs, VirtualKey.Control, User32.KEYEVENTF_KEYUP);
            }

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddInputs(inputs, VirtualKey.Alt, User32.KEYEVENTF_KEYUP);
            }
        }

        private void SendInputs(User32.INPUT[] inputs)
        {
            var foregroundWindow = IntPtr.Zero;
            var inputBlocked = false;

            try
            {
                inputBlocked = IntegrationHelper.BlockInput();
                foregroundWindow = IntegrationHelper.GetForegroundWindow();

                _visualStudioInstance.ActivateMainWindow();

                IntegrationHelper.SendInput(inputs);
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
    }
}
