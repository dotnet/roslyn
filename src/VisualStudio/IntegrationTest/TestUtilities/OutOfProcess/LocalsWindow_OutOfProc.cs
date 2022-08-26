// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class LocalsWindow_OutOfProc : OutOfProcComponent
    {
        public Verifier Verify { get; }

        private readonly LocalsWindow_InProc _localsWindowInProc;

        public LocalsWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _localsWindowInProc = CreateInProcComponent<LocalsWindow_InProc>(visualStudioInstance);
            Verify = new Verifier(this);
        }
    }
}
