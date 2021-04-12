﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class PreviewChangesDialog_OutOfProc : OutOfProcComponent
    {
        public PreviewChangesDialog_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
        }

        /// <summary>
        /// Verifies that the Preview Changes dialog is showing with the
        /// specified title. The dialog does not have an AutomationId and the 
        /// title can be changed by features, so callers of this method must
        /// specify a title.
        /// </summary>
        /// <param name="expectedTitle"></param>
        public void VerifyOpen(string expectedTitle, TimeSpan? timeout = null)
        {
            using (var cancellationTokenSource = timeout != null ? new CancellationTokenSource(timeout.Value) : null)
            {
                var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;
                DialogHelpers.FindDialogByName(GetMainWindowHWnd(), expectedTitle, isOpen: true, cancellationToken);

                // Wait for application idle to ensure the dialog is fully initialized
                VisualStudioInstance.WaitForApplicationIdle(cancellationToken);
            }
        }

        public void VerifyClosed(string expectedTitle)
            => DialogHelpers.FindDialogByName(GetMainWindowHWnd(), expectedTitle, isOpen: false, CancellationToken.None);

        public void ClickApplyAndWaitForFeature(string expectedTitle, string featureName)
        {
            DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), expectedTitle, "Apply");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, featureName);
        }

        public void ClickCancel(string expectedTitle)
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), expectedTitle, "Cancel");

        private IntPtr GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
