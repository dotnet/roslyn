// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ExtractInterfaceDialog_OutOfProc : OutOfProcComponent
    {
        private const string ExtractInterfaceDialogID = "ExtractInterfaceDialog";

        public ExtractInterfaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void VerifyOpen()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: true);

            if (dialog == null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be open but it is not.");
            }
        }

        public void VerifyClosed()
        {
            var dialog = DialogHelpers.FindDialog(GetMainWindowHWnd(), ExtractInterfaceDialogID, isOpen: false);

            if (dialog != null)
            {
                throw new InvalidOperationException($"Expected the '{ExtractInterfaceDialogID}' dialog to be closed but it is not.");
            }
        }

        public void ClickOK()
        {
            DialogHelpers.PressButton(GetMainWindowHWnd(), ExtractInterfaceDialogID, "OkButton");
            VisualStudioInstance.VisualStudioWorkspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }

        private int GetMainWindowHWnd()
        {
            return VisualStudioInstance.Shell.GetHWnd();
        }
    }
}
