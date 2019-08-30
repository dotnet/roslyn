// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
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

        public int ErrorListErrorCount
            => _inProc.ErrorListErrorCount;

        public void ShowErrorList()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorList);
            _inProc.ShowErrorList();
        }

        public void WaitForNoErrorsInErrorList(TimeSpan timeout)
            => _inProc.WaitForNoErrorsInErrorList(timeout);

        public int GetErrorListErrorCount()
            => _inProc.GetErrorCount();

        public ErrorListItem[] GetErrorListContents()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorList);
            return _inProc.GetErrorListContents();
        }

        public ErrorListItem NavigateToErrorListItem(int itemIndex)
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ErrorList);
            return _inProc.NavigateToErrorListItem(itemIndex);
        }
    }
}
