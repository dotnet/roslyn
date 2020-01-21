// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class PickMembersDialog_InProc : InProcComponent
    {
        private PickMembersDialog_InProc()
        {
        }

        public static PickMembersDialog_InProc Create()
            => new PickMembersDialog_InProc();

        public void VerifyOpen()
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
                        // Thread.Yield is insufficient; something in the light bulb must be relying on a UI thread
                        // message at lower priority than the Background priority used in testing.
                        WaitForApplicationIdle(Helper.HangMitigatingTimeout);
                        continue;
                    }

                    WaitForApplicationIdle(Helper.HangMitigatingTimeout);
                    return;
                }
            }
        }

        public void VerifyClosed()
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

        public void ClickDown()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.DownButton, cancellationTokenSource.Token));
            }
        }

        private async Task<PickMembersDialog> GetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<PickMembersDialog>().Single();
        }

        private async Task<PickMembersDialog> TryGetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<PickMembersDialog>().SingleOrDefault();
        }

        private async Task ClickAsync(Func<PickMembersDialog.TestAccessor, ButtonBase> buttonSelector, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            var dialog = await GetDialogAsync(cancellationToken);
            var button = buttonSelector(dialog.GetTestAccessor());
            Contract.ThrowIfFalse(await button.SimulateClickAsync(JoinableTaskFactory));
        }
    }
}
