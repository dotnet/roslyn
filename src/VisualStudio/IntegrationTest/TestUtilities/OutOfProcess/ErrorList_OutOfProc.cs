// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ErrorList_OutOfProc : OutOfProcComponent
    {
        private readonly ErrorList_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        public Verifier Verify { get; }

        public ErrorList_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<ErrorList_InProc>(visualStudioInstance);
            Verify = new Verifier(this, _instance);
        }

        public void ShowErrorList()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorList);
            _inProc.ShowErrorList();
        }

        public int GetErrorListErrorCount()
            => _inProc.GetErrorCount();
    }
}
