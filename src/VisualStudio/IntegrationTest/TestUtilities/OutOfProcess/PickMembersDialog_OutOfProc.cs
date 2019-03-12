// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Handles interaction with the Pick Members Dialog.
    /// </summary>
    public class PickMembersDialog_OutOfProc : OutOfProcComponent
    {
        private const string PickMembersDialogID = "PickMembersDialog";

        public PickMembersDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public bool CloseWindow()
        {
            var dialog = DialogHelpers.FindDialogByAutomationId(GetMainWindowHWnd(), PickMembersDialogID, isOpen: true, wait: false);
            if (dialog == null)
            {
                return false;
            }

            ClickCancel();
            return true;
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Pick Members operation to complete.
        /// </summary>
        public void ClickCancel()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), PickMembersDialogID, "CancelButton");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        private IntPtr GetMainWindowHWnd()
        {
            return VisualStudioInstance.Shell.GetHWnd();
        }
    }
}
