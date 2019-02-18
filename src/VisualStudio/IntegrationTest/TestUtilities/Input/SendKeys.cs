// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
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
            inputs.Add(new KeyPress(ch));
        }

        private static void AddInputs(List<KeyPress> inputs, KeyPress keyPress)
            => inputs.Add(keyPress);

        private void SendInputs(KeyPress[] inputs)
        {
            var foregroundWindow = IntPtr.Zero;

            try
            {
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
            }

            _visualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }
    }
}
