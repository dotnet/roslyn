// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.Common;
using Roslyn.VisualStudio.Test.Utilities.InProcess;

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

        public string[] GetCompletionItems()
        {
            WaitForCompletionSet();
            return _inProc.GetCompletionItems();
        }

        public string GetCurrentCompletionItem()
        {
            WaitForCompletionSet();
            return _inProc.GetCurrentCompletionItem();
        }

        public bool IsCompletionActive()
        {
            WaitForCompletionSet();
            return _inProc.IsCompletionActive();
        }

        public Signature[] GetSignatures()
        {
            WaitForSignatureHelp();
            return _inProc.GetSignatures();
        }

        public Signature GetCurrentSignature()
        {
            WaitForSignatureHelp();
            return _inProc.GetCurrentSignature();
        }

        public bool IsCaretOnScreen() => _inProc.IsCaretOnScreen();

        /// <summary>
        /// Sends key strokes to the active editor in Visual Studio. Various types are supported by this method:
        /// <see cref="string"/> (each character will be sent separately, <see cref="char"/>, <see cref="VirtualKey"/>
        /// and <see cref="KeyPress"/>.
        /// </summary>
        public void SendKeys(params object[] keys)
        {
            Activate();
            VisualStudioInstance.SendKeys.Send(keys);
            VisualStudioInstance.WaitForApplicationIdle();
        }
    }
}
