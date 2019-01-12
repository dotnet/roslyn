// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using UIAutomationClient;
using Xunit;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;

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
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }

        public void VerifyClosed()
        {
            // FindDialog will wait until the dialog is closed, so the return value is unused.
            DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, isOpen: false);
        }

        public bool CloseWindow()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), ChangeSignatureDialogAutomationId, isOpen: true, wait: false);
            if (dialog == null)
            {
                return false;
            }

            ClickCancel();
            return true;
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

            var propertyCondition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.AutomationIdProperty.Id, "MemberSelectionList");
            var grid = dialogAutomationElement.FindFirst(TreeScope.TreeScope_Descendants, propertyCondition);

            var gridPattern = grid.GetCurrentPattern<IUIAutomationGridPattern>(UIA_PatternIds.UIA_GridPatternId);
            var rowCount = gridPattern.CurrentRowCount;
            var columnToSelect = 2;
            int i = 0;
            for (; i < rowCount; i++)
            {
                // Modifier | Type | Parameter | Default
                var item = gridPattern.GetItem(i, columnToSelect);
                var name = AutomationElementExtensions.RetryIfNotAvailable(element => element.CurrentName, item);
                if (name == parameterName)
                {
                    // The parent of a cell is of DataItem control type, which support SelectionItemPattern.
                    var walker = Helper.Automation.ControlViewWalker;
                    var parent = AutomationElementExtensions.RetryIfNotAvailable(element => walker.GetParentElement(element), item);
                    parent.Select();
                    return;
                }
            }

            if (i == rowCount)
            {
                Assert.True(false, $"Unable to find the parameter {parameterName}");
            }
        }

        private IntPtr GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
