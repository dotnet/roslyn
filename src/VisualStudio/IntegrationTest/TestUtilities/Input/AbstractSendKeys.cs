// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public abstract class AbstractSendKeys
    {
        protected abstract void ActivateMainWindow();

        protected abstract void WaitForApplicationIdle(CancellationToken cancellationToken);

        public void Send(params object[] keys)
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

            SendInputs(inputs.ToArray());
        }

        private static void AddInputs(List<NativeMethods.INPUT> inputs, char ch)
        {
            var result = NativeMethods.VkKeyScan(ch);
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

        private static void AddUnicodeInputs(List<NativeMethods.INPUT> inputs, char ch)
        {
            var keyDownInput = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                Input =
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = NativeMethods.GetMessageExtraInfo(),
                    }
                }
            };

            var keyUpInput = new NativeMethods.INPUT
            {
                Type = NativeMethods.INPUT_KEYBOARD,
                Input =
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = NativeMethods.GetMessageExtraInfo(),
                    }
                }
            };

            inputs.Add(keyDownInput);
            inputs.Add(keyUpInput);
        }

        private static void AddInputs(List<NativeMethods.INPUT> inputs, VirtualKey virtualKey, uint dwFlags)
        {
            NativeMethods.INPUT input;
            var scanCode = NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
            if (scanCode != 0)
            {
                input = new NativeMethods.INPUT
                {
                    Type = NativeMethods.INPUT_KEYBOARD,
                    Input =
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)scanCode,
                            dwFlags = dwFlags | NativeMethods.KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = NativeMethods.GetMessageExtraInfo(),
                        }
                    }
                };
            }
            else
            {
                input = new NativeMethods.INPUT
                {
                    Type = NativeMethods.INPUT_KEYBOARD,
                    Input =
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = (ushort)virtualKey,
                            wScan = 0,
                            dwFlags = dwFlags,
                            time = 0,
                            dwExtraInfo = NativeMethods.GetMessageExtraInfo(),
                        }
                    }
                };
            }

            if (IsExtendedKey(virtualKey))
            {
                input.Input.ki.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
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

        private void SendInputs(NativeMethods.INPUT[] inputs)
        {
            ActivateMainWindow();

            IntegrationHelper.SendInput(inputs);

            WaitForApplicationIdle(CancellationToken.None);
        }
    }
}
