// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class Shell_OutOfProc : OutOfProcComponent
    {
        private readonly Shell_InProc _inProc;

        public Shell_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<Shell_InProc>(visualStudioInstance);
        }

        public string GetVersion()
            => _inProc.GetVersion();

        public string GetActiveWindowCaption()
            => _inProc.GetActiveWindowCaption();

        public IntPtr GetHWnd()
            => _inProc.GetHWnd();

        public bool IsActiveTabProvisional()
            => _inProc.IsActiveTabProvisional();
    }
}
