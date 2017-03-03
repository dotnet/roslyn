// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Automation;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ExtractInterfaceDialog_OutOfProc : OutOfProcComponent
    {
        private const string ExtractInterfaceDialogID = "ExtractInterfaceDialog";

        public ExtractInterfaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be open but it is not.");
            }
        }

        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: false);

            if (dialog != null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be closed but it is not.");
            }
        }

        public void ClickOK()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "OkButton");
            VisualStudioInstance.VisualStudioWorkspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        public void ClickCancel()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "CancelButton");
            VisualStudioInstance.VisualStudioWorkspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        public string GetTargetFileName()
        {
            var dialog = DialogHelpers.GetOpenDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var fileNameTextBox = dialog.FindDescendantByAutomationId("FileNameTextBox");

            return fileNameTextBox.GetValue();
        }

        public string[] GetSelectedItems()
        {
            var dialog = DialogHelpers.GetOpenDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var memberSelectionList = dialog.FindDescendantByAutomationId("MemberSelectionList");
            var listItems = memberSelectionList.FindDescendantsByClass("ListBoxItem");

            return listItems.Cast<AutomationElement>()
                .Select(item => item.FindDescendantByClass("CheckBox"))
                .Where(checkBox => checkBox.IsToggledOn())
                .Select(checkbox => checkbox.Current.AutomationId)
                .ToArray();
        }

        public void ClickDeselectAll()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "DeselectAllButton");
        }

        public void ClickSelectAll()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "SelectAllButton");
        }

        public void ToggleItem(string item)
        {
            var dialog = DialogHelpers.GetOpenDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var memberSelectionList = dialog.FindDescendantByAutomationId("MemberSelectionList");
            var checkBox = memberSelectionList.FindDescendantByAutomationId(item);

            checkBox.Toggle();
        }

        private int GetMainWindowHWnd()
        {
            return VisualStudioInstance.Shell.GetHWnd();
        }
    }
}
