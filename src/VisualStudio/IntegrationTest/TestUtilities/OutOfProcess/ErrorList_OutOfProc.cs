using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ErrorList_OutOfProc : OutOfProcComponent
    {
        private readonly ErrorList_InProc _inProc;

        public ErrorList_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<ErrorList_InProc>(visualStudioInstance);
        }

        public int ErrorListErrorCount
            => _inProc.ErrorListErrorCount;

        public void ShowErrorList()
            => _inProc.ShowErrorList();

        public void WaitForNoErrorsInErrorList()
            => _inProc.WaitForNoErrorsInErrorList();

        public int GetErrorListErrorCount()
            => _inProc.GetErrorCount();

        public ErrorListItem[] GetErrorListContents()
            => _inProc.GetErrorListContents();

        public void NavigateToErrorListItem(int itemIndex)
            => _inProc.NavigateToErrorListItem(itemIndex);
    }
}
