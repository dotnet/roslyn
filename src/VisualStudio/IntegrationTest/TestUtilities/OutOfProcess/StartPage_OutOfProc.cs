// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class StartPage_OutOfProc : OutOfProcComponent
    {
        private readonly StartPage_InProc _inProc;

        public StartPage_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<StartPage_InProc>(visualStudioInstance);
        }

        public void SetEnabled(bool enabled)
            => _inProc.SetEnabled(enabled);

        public bool CloseWindow()
            => _inProc.CloseWindow();
    }
}
