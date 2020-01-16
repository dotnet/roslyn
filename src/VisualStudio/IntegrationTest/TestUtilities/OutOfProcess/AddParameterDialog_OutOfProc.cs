// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

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

        public void FillCallsiteField(string callsiteValue)
            => _inProc.FillCallsiteField(callsiteValue);

        public void FillNameField(string parameterName)
            => _inProc.FillNameField(parameterName);

        public void FillTypeField(string typeName)
            => _inProc.FillTypeField(typeName);
    }
}
