// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class Dialog_OutOfProc : OutOfProcComponent
    {
        public Dialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen(string dialogName)
        {
            using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);

            // FindDialog will wait until the dialog is open, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: true, cancellationTokenSource.Token);

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle(cancellationTokenSource.Token);
        }

        public void VerifyClosed(string dialogName)
        {
            using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);

            // FindDialog will wait until the dialog is closed, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: false, cancellationTokenSource.Token);
        }

        public void Click(string dialogName, string buttonName)
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), dialogName, buttonName);

        private IntPtr GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
