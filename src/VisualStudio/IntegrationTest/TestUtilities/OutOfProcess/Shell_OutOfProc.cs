// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public IntPtr GetHWnd()
            => _inProc.GetHWnd();

        public bool IsUIContextActive(Guid context)
            => _inProc.IsUIContextActive(context);
    }
}
