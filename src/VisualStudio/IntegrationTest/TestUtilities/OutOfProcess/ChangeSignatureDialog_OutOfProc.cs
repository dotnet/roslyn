// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ChangeSignatureDialog_OutOfProc : OutOfProcComponent
    {
        private readonly ChangeSignatureDialog_InProc _inProc;

        public ChangeSignatureDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<ChangeSignatureDialog_InProc>(visualStudioInstance);
        }

        public void VerifyOpen()
            => _inProc.VerifyOpen();

        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public bool CloseWindow()
            => _inProc.CloseWindow();

        public void Invoke()
            => VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.V, ShiftState.Ctrl));

        public void ClickOK()
            => _inProc.ClickOK();

        public void ClickCancel()
            => _inProc.ClickCancel();

        public void ClickDownButton()
            => _inProc.ClickDownButton();

        public void ClickUpButton()
            => _inProc.ClickUpButton();

        public void ClickAddButton()
            => _inProc.ClickAddButton();

        public void ClickRemoveButton()
            => _inProc.ClickRemoveButton();

        public void ClickRestoreButton()
            => _inProc.ClickRestoreButton();

        public void SelectParameter(string parameterName)
            => _inProc.SelectParameter(parameterName);
    }
}
