// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Handles interaction with the Extract Interface Dialog.
    /// </summary>
    public class ExtractInterfaceDialog_OutOfProc : OutOfProcComponent
    {
        private const string ExtractInterfaceDialogID = "ExtractInterfaceDialog";

        public ExtractInterfaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently open.
        /// </summary>
        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be open but it is not.");
            }

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently closed.
        /// </summary>
        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: false);

            if (dialog != null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be closed but it is not.");
            }
        }

        public bool CloseWindow()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: true, wait: false);
            if (dialog == null)
            {
                return false;
            }

            ClickCancel();
            return true;
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public void ClickOK()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "OkButton");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public void ClickCancel()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "CancelButton");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Returns the name of the generated file that will contain the interface.
        /// </summary>
        public string GetTargetFileName()
        {
            var dialog = DialogHelpers.GetOpenDialogById(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var fileNameTextBox = dialog.FindDescendantByAutomationId("FileNameTextBox");

            return fileNameTextBox.GetValue();
        }

        /// <summary>
        /// Gets the set of members that are currently checked.
        /// </summary>
        public string[] GetSelectedItems()
        {
            var dialog = DialogHelpers.GetOpenDialogById(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var memberSelectionList = dialog.FindDescendantByAutomationId("MemberSelectionList");
            var comListItems = memberSelectionList.FindDescendantsByClass("ListBoxItem");
            var listItems = Enumerable.Range(0, comListItems.Length).Select(comListItems.GetElement);

            return listItems
                .Select(item => item.FindDescendantByClass("CheckBox"))
                .Where(checkBox => checkBox.IsToggledOn())
                .Select(checkbox => checkbox.CurrentAutomationId)
                .ToArray();
        }

        /// <summary>
        /// Clicks the "Deselect All" button.
        /// </summary>
        public void ClickDeselectAll()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "DeselectAllButton");
        }

        /// <summary>
        ///  Clicks the "Select All" button.
        /// </summary>
        public void ClickSelectAll()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "SelectAllButton");
        }

        public void SelectSameFile()
        {
            DialogHelpers.SelectRadioButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "DestinationCurrentFileSelectionRadioButton");
        }

        /// <summary>
        /// Clicks the checkbox on the given item, cycling it from on to off or from off to on.
        /// </summary>
        /// <param name="item"></param>
        public void ToggleItem(string item)
        {
            var dialog = DialogHelpers.GetOpenDialogById(GetMainWindowHWnd(), ExtractInterfaceDialogID);

            var memberSelectionList = dialog.FindDescendantByAutomationId("MemberSelectionList");
            var checkBox = memberSelectionList.FindDescendantByAutomationId(item);

            checkBox.Toggle();
        }

        private IntPtr GetMainWindowHWnd()
        {
            return VisualStudioInstance.Shell.GetHWnd();
        }
    }
}
