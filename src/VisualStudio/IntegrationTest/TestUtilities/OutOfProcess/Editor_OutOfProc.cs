// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var tagInfo = _editorInProc.GetTagSpans(tagId).ToList();

            // The spans are returned in an array:
            //    [s1.Start, s1.Length, s2.Start, s2.Length, ...]
            // Reconstruct the spans from their component parts

            var builder = ArrayBuilder<TextSpan>.GetInstance();

            for (int i = 0; i < tagInfo.Count; i += 2)
            {
                builder.Add(new TextSpan(tagInfo[i], tagInfo[i + 1]));
            }

            return builder.ToImmutableAndFree();
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
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SignatureHelp);
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

        public void InvokeNavigateTo(string text)
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            NavigateToSendKeys(text);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigateTo);
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

        public IUIAutomationElement GetDialog(string dialogAutomationId)
        {
            var dialog = DialogHelpers.GetOpenDialogById(_instance.Shell.GetHWnd(), dialogAutomationId);
            return dialog;
        }

        public void VerifyDialog(string dialogName, bool isOpen)
            => _editorInProc.VerifyDialog(dialogName, isOpen);

        public void PressDialogButton(string dialogAutomationName, string buttonAutomationName)
            => _editorInProc.PressDialogButton(dialogAutomationName, buttonAutomationName);

        public void DialogSendKeys(string dialogAutomationName, string keys)
            => _editorInProc.DialogSendKeys(dialogAutomationName, keys);

        public void FormatDocument() {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SendKeys(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.D, ShiftState.Ctrl));
        }

        public void FormatDocumentViaCommand()
        {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Dte.ExecuteCommand(WellKnownCommandNames.Edit_FormatDocument);
        }

        public void FormatSelection() {
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SendKeys(new KeyPress(VirtualKey.K, ShiftState.Ctrl), new KeyPress(VirtualKey.F, ShiftState.Ctrl));
        }

        public void Paste(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            VisualStudioInstance.Dte.ExecuteCommand("Edit.Paste");
        }

        public void Undo()
            => _editorInProc.Undo();

        public void NavigateToSendKeys(string keys)
            => _editorInProc.SendKeysToNavigateTo(keys);
            
        public ClassifiedToken[] GetLightbulbPreviewClassification(string menuText) =>
            _editorInProc.GetLightbulbPreviewClassifications(menuText);

        public void WaitForActiveView(string viewName)
            => _editorInProc.WaitForActiveView(viewName);

        public string[] GetErrorTags()
            => _editorInProc.GetErrorTags();

        public List<string> GetF1Keyword()
            => _editorInProc.GetF1Keywords();        

        public void ExpandProjectNavBar()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            _editorInProc.ExpandNavigationBar(0);
        }

        public void ExpandTypeNavBar()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            _editorInProc.ExpandNavigationBar(1);
        }

        public void ExpandMemberNavBar()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            _editorInProc.ExpandNavigationBar(2);
        }

        public string[] GetProjectNavBarItems()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetNavBarItems(0);
        }

        public string[] GetTypeNavBarItems()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetNavBarItems(1);
        }

        public string[] GetMemberNavBarItems()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetNavBarItems(2);
        }

        public string GetProjectNavBarSelection()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetSelectedNavBarItem(0);
        }

        public string GetTypeNavBarSelection()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetSelectedNavBarItem(1);
        }

        public string GetMemberNavBarSelection()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.GetSelectedNavBarItem(2);
        }

        public void SelectProjectNavbarItem(string item)
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
             _editorInProc.SelectNavBarItem(0, item);
        }

        public void SelectTypeNavBarItem(string item)
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            _editorInProc.SelectNavBarItem(1, item);
        }

        public void SelectMemberNavBarItem(string item)
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            _editorInProc.SelectNavBarItem(2, item);
        }

        public bool IsNavBarEnabled()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigationBar);
            return _editorInProc.IsNavBarEnabled();
        }

        public TextSpan[] GetKeywordHighlightTags()
            => Deserialize(_editorInProc.GetHighlightTags());

        public TextSpan[] GetOutliningSpans()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.Outlining);
            return Deserialize(_editorInProc.GetOutliningSpans());
        }

        private TextSpan[] Deserialize(string[] v)
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

        public void GoToDefinition()
            => _editorInProc.GoToDefinition();

        public void GoToImplementation()
            => _editorInProc.GoToImplementation();

        public void SendExplicitFocus()
            => _editorInProc.SendExplicitFocus();
    }
}
