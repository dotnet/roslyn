// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            // FindDialog will wait until the dialog is open, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: true, CancellationToken.None);

            // Wait for application idle to ensure the dialog is fully initialized
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }

        public void VerifyClosed(string dialogName)
        {
            // FindDialog will wait until the dialog is closed, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: false, CancellationToken.None);
        }

        public void Click(string dialogName, string buttonName)
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), dialogName, buttonName);

        private IntPtr GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
