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
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            _inProc.ShowErrorList();
        }

        public void WaitForNoErrorsInErrorList()
            => _inProc.WaitForNoErrorsInErrorList();

        public int GetErrorListErrorCount()
            => _inProc.GetErrorCount();

        public ErrorListItem[] GetErrorListContents()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            return _inProc.GetErrorListContents();
        }

        public void NavigateToErrorListItem(int itemIndex)
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            _inProc.NavigateToErrorListItem(itemIndex);
        }
    }
}
