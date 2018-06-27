// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class SendKeys_InProc2 : InProcComponent2
    {
        public SendKeys_InProc2(JoinableTaskFactory joinableTaskFactory)
            : base(joinableTaskFactory)
        {
        }

        private IntPtr MainWindowHandle => new WindowInteropHelper(Application.Current.MainWindow).Handle;

        public async Task SendAsync(params object[] keys)
        {
            var inputs = new List<MSG>(keys.Length * 2);

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

            await SendInputsAsync(inputs);
        }

        private void AddInputs(List<MSG> inputs, char ch)
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

        private void AddUnicodeInputs(List<MSG> inputs, char ch)
        {
            throw new NotImplementedException();
#if false
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
#endif
        }

        private void AddKeyDown(List<MSG> inputs, VirtualKey virtualKey)
        {
            const uint KeyPreviouslyUp = 0;
            const uint KeyNowDown = 0;

            var repeatCount = 1U;

            var scanCode = NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
            scanCode = (scanCode & 0xFFU) << 16;

            var extendedKey = IsExtendedKey(virtualKey) ? (1U << 24) : 0;

            var keyDown = new MSG
            {
                hwnd = MainWindowHandle,
                message = (int)NativeMethods.WM_KEYDOWN,
                wParam = (IntPtr)(int)virtualKey,
                lParam = (IntPtr)(int)(repeatCount | scanCode | extendedKey | KeyPreviouslyUp | KeyNowDown),
            };

            inputs.Add(keyDown);
        }

        private void AddKeyUp(List<MSG> inputs, VirtualKey virtualKey)
        {
            const uint KeyPreviouslyDown = 1U << 30;
            const uint KeyNowUp = 1U << 31;

            var repeatCount = 1U;

            var scanCode = NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
            scanCode = (scanCode & 0xFFU) << 16;

            var extendedKey = IsExtendedKey(virtualKey) ? (1U << 24) : 0;

            var keyUp = new MSG
            {
                hwnd = MainWindowHandle,
                message = (int)NativeMethods.WM_KEYUP,
                wParam = (IntPtr)(int)virtualKey,
                lParam = (IntPtr)(int)(repeatCount | scanCode | extendedKey | KeyPreviouslyDown | KeyNowUp),
            };

            inputs.Add(keyUp);
        }

        private static bool IsExtendedKey(VirtualKey virtualKey)
            => (virtualKey >= VirtualKey.PageUp && virtualKey <= VirtualKey.Down)
            || virtualKey == VirtualKey.Insert
            || virtualKey == VirtualKey.Delete;

        private void AddInputs(List<MSG> inputs, KeyPress keyPress)
            => AddInputs(inputs, keyPress.VirtualKey, keyPress.ShiftState);

        private void AddInputs(List<MSG> inputs, VirtualKey virtualKey, ShiftState shiftState = 0)
        {
            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddKeyDown(inputs, VirtualKey.Shift);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddKeyDown(inputs, VirtualKey.Control);
            }

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddKeyDown(inputs, VirtualKey.Alt);
            }

            AddKeyDown(inputs, virtualKey);
            AddKeyUp(inputs, virtualKey);

            if ((shiftState & ShiftState.Alt) != 0)
            {
                AddKeyUp(inputs, VirtualKey.Alt);
            }

            if ((shiftState & ShiftState.Ctrl) != 0)
            {
                AddKeyUp(inputs, VirtualKey.Control);
            }

            if ((shiftState & ShiftState.Shift) != 0)
            {
                AddKeyUp(inputs, VirtualKey.Shift);
            }
        }

        private async Task SendInputsAsync(IEnumerable<MSG> inputs)
        {
            await TaskScheduler.Default;

            foreach (var operation in inputs)
            {
                NativeMethods.PostMessage(operation.hwnd, (uint)operation.message, operation.wParam, operation.lParam);
            }

            await WaitForApplicationIdleAsync();
        }
    }
}
