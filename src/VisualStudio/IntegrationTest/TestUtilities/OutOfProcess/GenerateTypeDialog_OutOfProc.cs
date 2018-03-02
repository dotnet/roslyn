// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Automation;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class GenerateTypeDialog_OutOfProc : OutOfProcComponent
    {
        private const string GenerateTypeDialogID = "GenerateTypeDialog";

        public GenerateTypeDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), GenerateTypeDialogID, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{GenerateTypeDialogID}' dialog to be open but it is not.");
            }

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle();
        }

        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), GenerateTypeDialogID, isOpen: false);

            if (dialog != null)
            {
                throw new InvalidOperationException($"Expected the '{GenerateTypeDialogID}' dialog to be closed but it is not.");
            }
        }

        public void SetAccessibility(string accessibility)
        {
            DialogHelpers.SelectComboBoxItem(GetMainWindowHWnd(), GenerateTypeDialogID, "AccessList", accessibility);
        }

        public void SetKind(string kind)
        {
            DialogHelpers.SelectComboBoxItem(GetMainWindowHWnd(), GenerateTypeDialogID, "KindList", kind);
        }

        public void SetTargetProject(string projectName)
        {
            DialogHelpers.SelectComboBoxItem(GetMainWindowHWnd(), GenerateTypeDialogID, "ProjectList", projectName);
        }

        public void SetTargetFileToNewName(string newFileName)
        {
            DialogHelpers.SelectRadioButton(GetMainWindowHWnd(), GenerateTypeDialogID, "CreateNewFileRadioButton");
            DialogHelpers.SetElementValue(GetMainWindowHWnd(), GenerateTypeDialogID, "CreateNewFileComboBox", newFileName);
        }

        public void SetTargetFileToExisting(string existingFileName)
        {
            DialogHelpers.SelectRadioButton(GetMainWindowHWnd(), GenerateTypeDialogID, "AddToExistingFileRadioButton");
            DialogHelpers.SetElementValue(GetMainWindowHWnd(), GenerateTypeDialogID, "AddToExistingFileComboBox", existingFileName);
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the related Code Action to complete.
        /// </summary>
        public void ClickOK()
        {
            DialogHelpers.PressButtonWithName(GetMainWindowHWnd(), GenerateTypeDialogID, "OK");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the related Code Action to complete.
        /// </summary>
        public void ClickCancel()
        {
            DialogHelpers.PressButtonWithName(GetMainWindowHWnd(), GenerateTypeDialogID, "Cancel");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        public string[] GetNewFileComboBoxItems()
        {
            var dialog = DialogHelpers.GetOpenDialogById(GetMainWindowHWnd(), GenerateTypeDialogID);
            var createNewFileComboBox = dialog.FindDescendantByAutomationId("CreateNewFileComboBox");
            createNewFileComboBox.Expand();

            var children = createNewFileComboBox.FindDescendantsByClass("ListBoxItem");

            createNewFileComboBox.Collapse();

            return children.Cast<AutomationElement>().Select(element => element.Current.Name).ToArray();
        }

        private int GetMainWindowHWnd()
        {
            return VisualStudioInstance.Shell.GetHWnd();
        }
    }
}
