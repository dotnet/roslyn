// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using WindowsInput.Native;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public class SendKeys
    {
        private readonly VisualStudioInstance _visualStudioInstance;

        public SendKeys(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;
        }

        public void Send(params object[] keys)
        {
            var inputs = new List<KeyPress>(keys.Length);

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
                else if (key is VirtualKeyCode virtualKey)
                {
                    AddInputs(inputs, new KeyPress(virtualKey));
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

        private static void AddInputs(List<KeyPress> inputs, char ch)
        {
            KeyPress keyPress;

            var result = NativeMethods.VkKeyScan(ch);
            if (result == -1)
            {
                // This is a unicode character that must be handled differently
                keyPress = new KeyPress(ch);
            }
            else
            {
                var shiftState = (ShiftState)(((ushort)result >> 8) & 0xFF);
                if ((shiftState & ~(ShiftState.Alt | ShiftState.Shift | ShiftState.Ctrl)) != 0)
                {
                    // The modifiers for the key were not recognized
                    keyPress = new KeyPress(ch);
                }
                else
                {
                    var virtualKey = (VirtualKeyCode)(result & 0xFF);
                    var modifiers = ImmutableArray<VirtualKeyCode>.Empty;
                    if (shiftState.HasFlag(ShiftState.Ctrl))
                    {
                        modifiers = modifiers.Add(VirtualKeyCode.CONTROL);
                    }

                    if (shiftState.HasFlag(ShiftState.Alt))
                    {
                        modifiers = modifiers.Add(VirtualKeyCode.MENU);
                    }

                    if (shiftState.HasFlag(ShiftState.Shift))
                    {
                        modifiers = modifiers.Add(VirtualKeyCode.SHIFT);
                    }

                    keyPress = new KeyPress(virtualKey, modifiers);
                }
            }

            inputs.Add(keyPress);
        }

        private static void AddInputs(List<KeyPress> inputs, KeyPress keyPress)
            => inputs.Add(keyPress);

        private void SendInputs(KeyPress[] inputs)
        {
            _visualStudioInstance.ActivateMainWindow();

            IntegrationHelper.SendInput(inputs);

            _visualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }
    }
}
