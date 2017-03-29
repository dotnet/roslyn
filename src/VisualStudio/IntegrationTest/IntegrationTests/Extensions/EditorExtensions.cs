// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Automation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Editor
{
    public static partial class EditorExtensions
    {
        public static void AddWinFormButton(this AbstractIntegrationTest test, string buttonName)
            => test.VisualStudio.Instance.Editor.AddWinFormButton(buttonName);

        public static void DeleteWinFormButton(this AbstractIntegrationTest test, string buttonName)
            => test.VisualStudio.Instance.Editor.DeleteWinFormButton(buttonName);

        public static void EditWinFormButtonProperty(this AbstractIntegrationTest test, string buttonName, string propertyName, string propertyValue, string propertyTypeName = null)
            => test.VisualStudio.Instance.Editor.EditWinFormButtonProperty(buttonName, propertyName, propertyValue, propertyTypeName);

        public static void EditWinFormsButtonEvent(this AbstractIntegrationTest test, string buttonName, string eventName, string eventHandlerName)
            => test.VisualStudio.Instance.Editor.EditWinFormButtonEvent(buttonName, eventName, eventHandlerName);

        public static string GetWinFormButtonPropertyValue(this AbstractIntegrationTest test, string buttonName, string propertyName)
            => test.VisualStudio.Instance.Editor.GetWinFormButtonPropertyValue(buttonName, propertyName);

        public static void SelectTextInCurrentDocument(this AbstractIntegrationTest test, string text)
        {
            test.VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false);
            test.VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false);
        }

        public static void DeleteText(this AbstractIntegrationTest test, string text)
        {
            test.SelectTextInCurrentDocument(text);
            test.SendKeys(VirtualKey.Delete);
        }

        public static void PlaceCaret(this AbstractIntegrationTest test, string text, int charsOffset = 0)
            => test.VisualStudio.Instance.Editor.PlaceCaret(text, charsOffset: charsOffset, occurrence: 0, extendSelection: false, selectBlock: false);

        public static void SendKeys(this AbstractIntegrationTest test, params object[] keys)
            => test.VisualStudio.Instance.Editor.SendKeys(keys);

        public static void InvokeSignatureHelp(this AbstractIntegrationTest test)
        {
            test.ExecuteCommand(WellKnownCommandNames.Edit_ParameterInfo);
            test.WaitForAsyncOperations(FeatureAttribute.SignatureHelp);
        }

        public static void InvokeNavigateToAndPressEnter(this AbstractIntegrationTest test, string text)
        {
            test.ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            test.VisualStudio.Instance.Editor.NavigateToSendKeys(text);
            test.WaitForAsyncOperations(FeatureAttribute.NavigateTo);
            test.VisualStudio.Instance.Editor.NavigateToSendKeys("{ENTER}");
        }

        public static void PressDialogButton(this AbstractIntegrationTest test, string dialogAutomationName, string buttonAutomationName)
        {
            test.VisualStudio.Instance.Editor.PressDialogButton(dialogAutomationName, buttonAutomationName);
        }

        public static AutomationElement GetDialog(this AbstractIntegrationTest test, string dialogAutomationId)
        {
            var dialog = DialogHelpers.FindDialog(test.VisualStudio.Instance.Shell.GetHWnd(), dialogAutomationId, isOpen: true);
            return dialog;
        }
    }
}