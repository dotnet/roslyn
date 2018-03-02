// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Automation;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ChangeSignatureDialog_OutOfProc : OutOfProcComponent
    {
        private const string ChangeSignatureDialogAutomationId = "ChangeSignatureDialog";

        public ChangeSignatureDialog_OutOfProc(VisualStudioInstance visualStudioInstance) 
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen()
        {
            // FindDialog will wait until the dialog is open, so the return value is unused.
            DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, isOpen: true);

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle();
        }
  
        public void VerifyClosed()
        {
            // FindDialog will wait until the dialog is closed, so the return value is unused.
            DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, isOpen: false);
        }

        public void Invoke()
            => VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.V, ShiftState.Ctrl));

        public void ClickOK()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "OKButton");

        public void ClickCancel()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "CancelButton");

        public void ClickDownButton()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "DownButton");

        public void ClickUpButton()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "UpButton");

        public void ClickRemoveButton()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "RemoveButton");

        public void ClickRestoreButton()
            => DialogHelpers.PressButton(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, "RestoreButton");

        public void SelectParameter(string parameterName)
        {
            var dialogAutomationElement = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, isOpen: true);

            Condition propertyCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, "MemberSelectionList");
            var grid = dialogAutomationElement.FindFirst(TreeScope.Descendants, propertyCondition);

            var gridPattern = grid.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
            var rowCount = (int)grid.GetCurrentPropertyValue(GridPattern.RowCountProperty);
            var columnToSelect = 2;
            int i = 0;
            for (; i < rowCount; i++)
            {
                // Modifier | Type | Parameter | Default
                var item = gridPattern.GetItem(i, columnToSelect);
                var name = item.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
                if (name == parameterName)
                {
                    // The parent of a cell is of DataItem control type, which support SelectionItemPattern.
                    TreeWalker walker = TreeWalker.ControlViewWalker;
                    var parent = walker.GetParent(item);
                    if (parent.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
                    {
                        (pattern as SelectionItemPattern).Select();
                    }
                    else
                    {
                        Assert.True(false, "Unexpected error. Item's parent is expected to support SelectionItemPattern.");
                    }

                    return;
                }
            }

            if (i == rowCount)
            {
                Assert.True(false, $"Unable to find the parameter {parameterName}");
            }
        }

        private int GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
