// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class GenerateTypeDialog_InProc : InProcComponent
    {
        private const string GenerateTypeDialogID = "GenerateTypeDialog";

        private GenerateTypeDialog_InProc() { }

        public static GenerateTypeDialog_InProc Create() =>
            new GenerateTypeDialog_InProc();

        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), GenerateTypeDialogID, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{GenerateTypeDialogID}' dialog to be open but it is not.");
            }
        }
        
        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), GenerateTypeDialogID, isOpen: false);

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

        public void ClickOK()
        {
            DialogHelpers.PressButtonWithName(GetMainWindowHWnd(), GenerateTypeDialogID, "OK");
        }

        public void ClickCancel()
        {
            DialogHelpers.PressButtonWithName(GetMainWindowHWnd(), GenerateTypeDialogID, "Cancel");
        }

        public string[] GetNewFileComboBoxItems()
        {
            var dialog = DialogHelpers.GetOpenDialog(GetMainWindowHWnd(), GenerateTypeDialogID);
            var createNewFileComboBox = dialog.FindDescendantByAutomationId("CreateNewFileComboBox");
            createNewFileComboBox.Expand();

            var children = createNewFileComboBox.FindDescendantsByClass("ListBoxItem");

            createNewFileComboBox.Collapse();

            return children.Cast<AutomationElement>().Select(element => element.Current.Name).ToArray();
        }

        private static int GetMainWindowHWnd()
        {
            return GetDTE().MainWindow.HWnd;
        }
    }
}
