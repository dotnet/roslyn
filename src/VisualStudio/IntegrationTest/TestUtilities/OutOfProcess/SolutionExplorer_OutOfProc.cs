// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        private readonly SolutionExplorer_InProc _inProc;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
        }

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();
    }
}
