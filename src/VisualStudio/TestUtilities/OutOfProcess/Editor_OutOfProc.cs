// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities.Common;
using Roslyn.VisualStudio.Test.Utilities.InProcess;
using Roslyn.VisualStudio.Test.Utilities.Input;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc : OutOfProcComponent
    {
        private readonly Editor_InProc _inProc;

        internal Editor_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            this._inProc = CreateInProcComponent<Editor_InProc>(visualStudioInstance);
        }

        public void Activate() => _inProc.Activate();

        public string GetText() => _inProc.GetText();
        public void SetText(string value) => _inProc.SetText(value);

        public string GetCurrentLineText() => _inProc.GetCurrentLineText();
        public int GetCaretPosition() => _inProc.GetCaretPosition();
        public string GetLineTextBeforeCaret() => _inProc.GetLineTextBeforeCaret();
        public string GetLineTextAfterCaret() => _inProc.GetLineTextAfterCaret();

        public void MoveCaret(int position) => _inProc.MoveCaret(position);

        public string[] GetCompletionItems() => _inProc.GetCompletionItems();
        public string GetCurrentCompletionItem() => _inProc.GetCurrentCompletionItem();
        public bool IsCompletionActive() => _inProc.IsCompletionActive();

        public Signature[] GetSignatures() => _inProc.GetSignatures();
        public Signature GetCurrentSignature() => _inProc.GetCurrentSignature();

        public bool IsCaretOnScreen() => _inProc.IsCaretOnScreen();

        /// <summary>
        /// Sends key strokes to the active editor in Visual Studio. Various types are supported by this method:
        /// <see cref="string"/> (each character will be sent separately, <see cref="char"/>, <see cref="VirtualKey"/>
        /// and <see cref="KeyPress"/>.
        /// </summary>
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
