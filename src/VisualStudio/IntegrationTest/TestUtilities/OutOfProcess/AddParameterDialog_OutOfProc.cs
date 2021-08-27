// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class AddParameterDialog_OutOfProc : OutOfProcComponent
    {
        private readonly AddParameterDialog_InProc _inProc;

        public AddParameterDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<AddParameterDialog_InProc>(visualStudioInstance);
        }

        public void VerifyOpen()
            => _inProc.VerifyOpen();

        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public bool CloseWindow()
            => _inProc.CloseWindow();

        public void ClickOK()
            => _inProc.ClickOK();

        public void ClickCancel()
            => _inProc.ClickCancel();

        public void FillCallSiteField(string callsiteValue)
            => _inProc.FillCallSiteField(callsiteValue);

        public void FillNameField(string parameterName)
            => _inProc.FillNameField(parameterName);

        public void FillTypeField(string typeName)
            => _inProc.FillTypeField(typeName);

        public void SetCallSiteTodo()
            => _inProc.SetCallSiteTodo();
    }
}
