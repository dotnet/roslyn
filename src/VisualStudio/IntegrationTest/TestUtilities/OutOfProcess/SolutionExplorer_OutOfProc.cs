// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

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
