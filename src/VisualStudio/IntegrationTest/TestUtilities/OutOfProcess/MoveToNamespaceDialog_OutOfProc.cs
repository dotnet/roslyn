// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class MoveToNamespaceDialog_OutOfProc : OutOfProcComponent
    {
        private const string MoveToNamespaceDialogId = "MoveToNamespaceDialog";

        public MoveToNamespaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        /// <summary>
        /// Verifies that the Move To Namespace dialog is currently open.
        /// </summary>
        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), MoveToNamespaceDialogId, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{MoveToNamespaceDialogId}' dialog to be open but it is not.");
            }

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }

        /// <summary>
        /// Verifies that the Move To Namespace dialog is currently closed.
        /// </summary>
        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), MoveToNamespaceDialogId, isOpen: false);

            if (dialog != null)
            {
                throw new InvalidOperationException($"Expected the '{MoveToNamespaceDialogId}' dialog to be closed but it is not.");
            }
        }

        public bool CloseWindow()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), MoveToNamespaceDialogId, isOpen: true, wait: false);
            if (dialog == null)
            {
                return false;
            }

            ClickCancel();
            return true;
        }

        public void SetNamespace(string @namespace)
        {
            DialogHelpers.SetElementValue(GetMainWindowHWnd(), MoveToNamespaceDialogId, "NamespaceBox", @namespace);
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the Move To Namespace operation to complete.
        /// </summary>
        public void ClickOK()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), MoveToNamespaceDialogId, "OkButton");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Move To Namespace operation to complete.
        /// </summary>
        public void ClickCancel()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), MoveToNamespaceDialogId, "CancelButton");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        private IntPtr GetMainWindowHWnd() => VisualStudioInstance.Shell.GetHWnd();
    }
}
