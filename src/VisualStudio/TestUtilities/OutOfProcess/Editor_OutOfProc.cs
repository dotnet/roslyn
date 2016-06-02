// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities.InProcess;
using Roslyn.VisualStudio.Test.Utilities.Input;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc : OutOfProcComponent<Editor_InProc>
    {
        internal Editor_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void Activate() => InProc.Activate();

        public string GetText() => InProc.GetText();
        public void SetText(string value) => InProc.SetText(value);

        public string GetCurrentLineText() => InProc.GetCurrentLineText();
        public int GetCaretPosition() => InProc.GetCaretPosition();
        public string GetLineTextBeforeCaret() => InProc.GetLineTextBeforeCaret();
        public string GetLineTextAfterCaret() => InProc.GetLineTextAfterCaret();

        public void MoveCaret(int position) => InProc.MoveCaret(position);

        public string[] GetCompletionItems() => InProc.GetCompletionItems();
        public string GetCurrentCompletionItem() => InProc.GetCurrentCompletionItem();

        public void SendKeys(params object[] keys)
        {
            Activate();

            foreach (var key in keys)
            {
                if (key is string)
                {
                    var text = ((string)key)
                        .Replace("\r\n", "\r")
                        .Replace("\n", "\r");

                    foreach (var ch in text)
                    {
                        VisualStudioInstance.SendKeys.Send(ch);
                    }
                }
                else if (key is char)
                {
                    VisualStudioInstance.SendKeys.Send((char)key);
                }
                else if (key is VirtualKey)
                {
                    VisualStudioInstance.SendKeys.Send((VirtualKey)key);
                }
                else if (key is KeyPress)
                {
                    VisualStudioInstance.SendKeys.Send((KeyPress)key);
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

            VisualStudioInstance.WaitForApplicationIdle();
        }
    }
}
