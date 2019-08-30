using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    class MoveToNamespaceDialog_InProc : AbstractCodeRefactorDialog_InProc<MoveToNamespaceDialog, MoveToNamespaceDialog.TestAccessor>
    {
        private MoveToNamespaceDialog_InProc()
        {
        }

        public static MoveToNamespaceDialog_InProc Create()
            => new MoveToNamespaceDialog_InProc();

        protected override MoveToNamespaceDialog.TestAccessor GetAccessor(MoveToNamespaceDialog dialog) => dialog.GetTestAccessor();

        public bool CloseWindow()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                if (JoinableTaskFactory.Run(() => TryGetDialogAsync(cancellationTokenSource.Token)) is null)
                {
                    return false;
                }
            }

            ClickCancel();
            return true;
        }

        public void ClickOK()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.OKButton, cancellationTokenSource.Token));
            }
        }

        public void ClickCancel()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.CancelButton, cancellationTokenSource.Token));
            }
        }

        public void SetSetNamespace(string @namespace)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                var cancellationToken = cancellationTokenSource.Token;

                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                    var testAccessor = GetAccessor(await TryGetDialogAsync(cancellationToken));
                    var success = await testAccessor.NamespaceBox.SimulateSelectItemAsync(JoinableTaskFactory, @namespace, mustExist: false);
                    Contract.ThrowIfFalse(success);
                });
            }
        }
    }
}
