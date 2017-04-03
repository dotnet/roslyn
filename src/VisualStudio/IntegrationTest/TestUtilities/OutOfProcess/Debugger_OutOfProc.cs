// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class Debugger_OutOfProc : OutOfProcComponent
    {
        private readonly Debugger_InProc _inProc;

        public Debugger_OutOfProc(VisualStudioInstance visualStudioInstance)
            :base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<Debugger_InProc>(visualStudioInstance);
        }

        public void StartDebugging(bool waitForBreakMode)
            => _inProc.StartDebugging(waitForBreakMode);

        public void Continue(bool waitForBreakMode)
            => _inProc.Continue(waitForBreakMode);
    }
}