// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class EditAndContinueDialog_OutOfProc : OutOfProcComponent
    {
        private const string ChangeSignatureDialogName = "Edit and Continue";

        public EditAndContinueDialog_OutOfProc(VisualStudioInstance visualStudioInstance) 
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen()
        {
            // FindDialog will wait until the dialog is open, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), ChangeSignatureDialogName, isOpen: true);
        }

        public void VerifyClosed()
        {
            // FindDialog will wait until the dialog is closed, so the return value is unused.
            DialogHelpers.FindDialogByName(GetMainWindowHWnd(), ChangeSignatureDialogName, isOpen: false);
        }

        public void ClickOK()
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), ChangeSignatureDialogName, "OK");

        private int GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}