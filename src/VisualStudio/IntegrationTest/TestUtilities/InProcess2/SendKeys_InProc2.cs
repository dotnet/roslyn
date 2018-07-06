// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class SendKeys_InProc2 : InProcComponent2
    {
        public SendKeys_InProc2(TestServices testService)
            : base(testService)
        {
        }

        public async Task SendAsync(params object[] keys)
        {
            var inputs = new List<NativeMethods.INPUT>(keys.Length);

            foreach (var key in keys)
            {
                if (key is string s)
                {
                    var text = s.Replace("\r\n", "\r")
                                .Replace("\n", "\r");

                    foreach (var ch in text)
                    {
                        AddInputs(inputs, ch);
                    }
                }
                else if (key is char c)
                {
                    AddInputs(inputs, c);
                }
                else if (key is VirtualKey virtualKey)
                {
                    AddInputs(inputs, virtualKey);
                }
                else if (key is KeyPress keyPress)
                {
                    AddInputs(inputs, keyPress);
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

            await SendInputsAsync(inputs.ToArray());
        }

        private static void AddInputs(List<NativeMethods.INPUT> inputs, char ch)
        {
            var result = NativeMethods.VkKeyScan(ch);
            if (result == -1)
            {
                // This is a Unicode character that must be handled differently.

                AddUnicodeInputs(inputs, ch);
                return;
            }

            var virtualKey = (VirtualKey)(result & 0xff);
            var shiftState = (ShiftState)(((ushort)result >> 8) & 0xff);

            AddInputs(inputs, virtualKey, shiftState);
        }

        private static void AddUnicodeInputs(List<NativeMethods.INPUT> inputs, char ch)
        {
            var keyDownInput = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            var keyUpInput = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            inputs.Add(keyDownInput);
            inputs.Add(keyUpInput);
        }

        private static void AddInputs(List<NativeMethods.INPUT> inputs, VirtualKey virtualKey, uint dwFlags)
        {
            var input = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                ki = new NativeMethods.KEYBDINPUT
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
                input.ki.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }

            inputs.Add(input);
        }

        private static bool IsExtendedKey(VirtualKey virtualKey)
            => (virtualKey >= VirtualKey.PageUp && virtualKey <= VirtualKey.Down)
            || virtualKey == VirtualKey.Insert
            || virtualKey == VirtualKey.Delete;

        private static void AddInputs(List<NativeMethods.INPUT> inputs, KeyPress keyPress)
            => AddInputs(inputs, keyPress.VirtualKey, keyPress.ShiftState);

        private static void AddInputs(List<NativeMethods.INPUT> inputs, VirtualKey virtualKey, ShiftState shiftState = 0)
        {
            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddInputs(inputs, VirtualKey.Shift, NativeMethods.KEYEVENTF_NONE);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddInputs(inputs, VirtualKey.Control, NativeMethods.KEYEVENTF_NONE);
            }

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddInputs(inputs, VirtualKey.Alt, NativeMethods.KEYEVENTF_NONE);
            }

            AddInputs(inputs, virtualKey, NativeMethods.KEYEVENTF_NONE);
            AddInputs(inputs, virtualKey, NativeMethods.KEYEVENTF_KEYUP);

            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddInputs(inputs, VirtualKey.Shift, NativeMethods.KEYEVENTF_KEYUP);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddInputs(inputs, VirtualKey.Control, NativeMethods.KEYEVENTF_KEYUP);
            }

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddInputs(inputs, VirtualKey.Alt, NativeMethods.KEYEVENTF_KEYUP);
            }
        }

        private async Task SendInputsAsync(NativeMethods.INPUT[] inputs)
        {
            await TaskScheduler.Default;

            var foregroundWindow = IntPtr.Zero;

            try
            {
                foregroundWindow = IntegrationHelper.GetForegroundWindow();

                await TestServices.VisualStudio.ActivateMainWindowAsync(skipAttachingThreads: true);

                IntegrationHelper.SendInput(inputs);
            }
            finally
            {
                if (foregroundWindow != IntPtr.Zero)
                {
                    IntegrationHelper.SetForegroundWindow(foregroundWindow, throwOnFailure: false);
                }
            }

            await WaitForApplicationIdleAsync(CancellationToken.None);
        }
    }
}
