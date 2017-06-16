// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
