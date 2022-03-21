// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using UIAutomationClient;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc : TextViewWindow_OutOfProc
    {
        public new Verifier Verify { get; }

        private readonly Editor_InProc _editorInProc;
        private readonly VisualStudioInstance _instance;

        internal Editor_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _editorInProc = (Editor_InProc)_textViewWindowInProc;
            Verify = new Verifier(this, _instance);
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

        public ImmutableArray<TextSpan> GetTagSpans(string tagId)
        {
            if (tagId == _instance.InlineRenameDialog.ValidRenameTag)
            {
                _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
            }

            var tagInfo = _editorInProc.GetTagSpans(tagId).ToList();

            // The spans are returned in an array:
            //    [s1.Start, s1.Length, s2.Start, s2.Length, ...]
            // Reconstruct the spans from their component parts

            var builder = ArrayBuilder<TextSpan>.GetInstance();

            for (var i = 0; i < tagInfo.Count; i += 2)
            {
                builder.Add(new TextSpan(tagInfo[i], tagInfo[i + 1]));
            }

            return builder.ToImmutableAndFree();
        }

        public void InvokeNavigateToNextHighlightedReference()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_NextHighlightedReference);
        }

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

        public void InvokeSignatureHelp()
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_ParameterInfo);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
        }

        public bool IsSignatureHelpActive()
        {
            WaitForSignatureHelp();
            return _editorInProc.IsSignatureHelpActive();
        }

        public Signature GetCurrentSignature()
        {
            WaitForSignatureHelp();
            return _editorInProc.GetCurrentSignature();
        }

        public void InvokeNavigateTo(params object[] keys)
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            NavigateToSendKeys(keys);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.NavigateTo);
        }

        public void SelectTextInCurrentDocument(string text)
        {
            PlaceCaret(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            PlaceCaret(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        public int GetLine() => _editorInProc.GetLine();

        public int GetColumn() => _editorInProc.GetColumn();

        public void DeleteText(string text)
        {
            SelectTextInCurrentDocument(text);
            SendKeys(VirtualKey.Delete);
        }

        public void ReplaceText(string oldText, string newText)
            => _editorInProc.ReplaceText(oldText, newText);

        public bool IsCaretOnScreen()
            => _editorInProc.IsCaretOnScreen();

        public void AddWinFormButton(string buttonName)
            => _editorInProc.AddWinFormButton(buttonName);

        public void DeleteWinFormButton(string buttonName)
            => _editorInProc.DeleteWinFormButton(buttonName);

        public void EditWinFormButtonProperty(string buttonName, string propertyName, string propertyValue, string? propertyTypeName = null)
            => _editorInProc.EditWinFormButtonProperty(buttonName, propertyName, propertyValue, propertyTypeName);

        public void EditWinFormButtonEvent(string buttonName, string eventName, string eventHandlerName)
            => _editorInProc.EditWinFormButtonEvent(buttonName, eventName, eventHandlerName);

        public string? GetWinFormButtonPropertyValue(string buttonName, string propertyName)
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

        public void VerifyDialog(string dialogName, bool isOpen)
            => _editorInProc.VerifyDialog(dialogName, isOpen);

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
            => _editorInProc.PressDialogButton(dialogAutomationName, buttonAutomationName);

        public void DialogSendKeys(string dialogAutomationName, params object[] keys)
            => _editorInProc.DialogSendKeys(dialogAutomationName, keys);

        public void FormatDocument()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            SendKeys(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.D, ShiftState.Ctrl));
        }

        public void FormatDocumentViaCommand()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            _editorInProc.FormatDocumentViaCommand();
        }

        public void FormatSelection()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            SendKeys(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.F, ShiftState.Ctrl));
        }

        public void Paste(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            _editorInProc.Paste();
        }

        public void Undo()
            => _editorInProc.Undo();

        public void Redo()
            => _editorInProc.Redo();

        public void NavigateToSendKeys(params object[] keys)
            => _editorInProc.SendKeysToNavigateTo(keys);

        public ClassifiedToken[] GetLightbulbPreviewClassification(string menuText) =>
            _editorInProc.GetLightbulbPreviewClassifications(menuText);

        public void SetUseSuggestionMode(bool value)
        {
            Assert.False(IsCompletionActive());
            _editorInProc.SetUseSuggestionMode(value);
        }

        public void WaitForActiveView(string viewName)
            => _editorInProc.WaitForActiveView(viewName);

        public string[] GetErrorTags()
            => _editorInProc.GetErrorTags();

        public string[] GetProjectNavBarItems()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.NavigationBar);
            return _editorInProc.GetNavBarItems(0);
        }

        public TextSpan[] GetKeywordHighlightTags()
            => Deserialize(_editorInProc.GetHighlightTags());

        public TextSpan[] GetOutliningSpans()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Outlining);
            return Deserialize(_editorInProc.GetOutliningSpans());
        }

        private static TextSpan[] Deserialize(string[] v)
        {
            // returned tag looks something like 'text'[12-13]
            return v.Select(tag =>
            {
                var open = tag.LastIndexOf('[') + 1;
                var comma = tag.LastIndexOf('-');
                var close = tag.LastIndexOf(']');
                var start = tag.Substring(open, comma - open);
                var end = tag.Substring(comma + 1, close - comma - 1);
                return TextSpan.FromBounds(int.Parse(start), int.Parse(end));
            }).ToArray();
        }

        public void SendExplicitFocus()
            => _editorInProc.SendExplicitFocus();

        public void WaitForEditorOperations(TimeSpan timeout)
            => _editorInProc.WaitForEditorOperations(timeout);
    }
}
