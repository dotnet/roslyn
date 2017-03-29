// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc : TextViewWindow_OutOfProc
    {
        private readonly Editor_InProc _editorInProc;

        internal Editor_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _editorInProc = (Editor_InProc)_textViewWindowInProc;
        }

        internal override TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance)
            => CreateInProcComponent<Editor_InProc>(visualStudioInstance);

        public void Activate()
            => _editorInProc.Activate();

        public string GetText()
            => _editorInProc.GetText();

        public void SetText(string value)
            => _editorInProc.SetText(value);

        public string GetCurrentLineText()
            => _editorInProc.GetCurrentLineText();

        public string GetLineTextBeforeCaret()
            => _editorInProc.GetLineTextBeforeCaret();

        public string GetSelectedText()
            => _editorInProc.GetSelectedText();

        public string GetLineTextAfterCaret()
            => _editorInProc.GetLineTextAfterCaret();

        public void MoveCaret(int position)
            => _editorInProc.MoveCaret(position);

        public string GetCurrentCompletionItem()
        {
            WaitForCompletionSet();
            return _editorInProc.GetCurrentCompletionItem();
        }

        public bool IsCompletionActive()
        {
            WaitForCompletionSet();
            return _editorInProc.IsCompletionActive();
        }

        public bool IsSignatureHelpActive()
        {
            WaitForSignatureHelp();
            return _editorInProc.IsSignatureHelpActive();
        }

        public Signature[] GetSignatures()
        {
            WaitForSignatureHelp();
            return _editorInProc.GetSignatures();
        }

        public Signature GetCurrentSignature()
        {
            WaitForSignatureHelp();
            return _editorInProc.GetCurrentSignature();
        }

        public bool IsCaretOnScreen()
            => _editorInProc.IsCaretOnScreen();

        public void AddWinFormButton(string buttonName)
            => _editorInProc.AddWinFormButton(buttonName);

        public void DeleteWinFormButton(string buttonName)
            => _editorInProc.DeleteWinFormButton(buttonName);

        public void EditWinFormButtonProperty(string buttonName, string propertyName, string propertyValue, string propertyTypeName = null)
            => _editorInProc.EditWinFormButtonProperty(buttonName, propertyName, propertyValue, propertyTypeName);

        public void EditWinFormButtonEvent(string buttonName, string eventName, string eventHandlerName)
            => _editorInProc.EditWinFormButtonEvent(buttonName, eventName, eventHandlerName);

        public string GetWinFormButtonPropertyValue(string buttonName, string propertyName)
            => _editorInProc.GetWinFormButtonPropertyValue(buttonName, propertyName);

        /// <summary>
        /// Sends key strokes to the active editor in Visual Studio. Various types are supported by this method:
        /// <see cref="string"/> (each character will be sent separately, <see cref="char"/>, <see cref="VirtualKey"/>
        /// and <see cref="KeyPress"/>.
        /// </summary>
        public void SendKeys(params object[] keys)
        {
            Activate();
            VisualStudioInstance.SendKeys.Send(keys);
        }

        public void MessageBox(string message)
            => _editorInProc.MessageBox(message);

        public void VerifyDialog(string dialogName, bool isOpen)
            => _editorInProc.VerifyDialog(dialogName, isOpen);

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
            => _editorInProc.PressDialogButton(dialogAutomationName, buttonAutomationName);

        public void DialogSendKeys(string dialogAutomationName, string keys)
            => _editorInProc.DialogSendKeys(dialogAutomationName, keys);

        public void Undo()
            => _editorInProc.Undo();

        public void NavigateToSendKeys(string keys)
            => _editorInProc.SendKeysToNavigateTo(keys);

        public void WaitForActiveView(string viewName)
            => _editorInProc.WaitForActiveView(viewName);

        public string[] GetErrorTags()
            => _editorInProc.GetErrorTags();
    }
}