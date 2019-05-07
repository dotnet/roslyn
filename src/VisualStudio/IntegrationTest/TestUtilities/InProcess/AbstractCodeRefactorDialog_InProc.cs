using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal abstract class AbstractCodeRefactorDialog_InProc<DialogType, AccessorType> : InProcComponent
    {
        public virtual void VerifyOpen()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                var cancellationToken = cancellationTokenSource.Token;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var window = JoinableTaskFactory.Run(() => TryGetDialogAsync(cancellationToken));
                    if (window is null)
                    {
                        Thread.Yield();
                        continue;
                    }

                    WaitForApplicationIdle(Helper.HangMitigatingTimeout);
                    return;
                }
            }
        }

        public virtual void VerifyClosed()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                var cancellationToken = cancellationTokenSource.Token;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var window = JoinableTaskFactory.Run(() => TryGetDialogAsync(cancellationToken));
                    if (window is null)
                    {
                        return;
                    }

                    Thread.Yield();
                }
            }
        }


        protected virtual async Task<DialogType> GetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<DialogType>().Single();
        }

        protected virtual async Task<DialogType> TryGetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<DialogType>().SingleOrDefault();
        }

        protected virtual async Task ClickAsync(Func<AccessorType, ButtonBase> buttonSelector, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            var dialog = await GetDialogAsync(cancellationToken);
            var button = buttonSelector(GetAccessor(dialog));
            var result = await button.SimulateClickAsync(JoinableTaskFactory);
            Contract.ThrowIfFalse(result);
        }

        protected abstract AccessorType GetAccessor(DialogType dialog);
    }
}
