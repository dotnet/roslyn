// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ChangeSignatureDialog_InProc : InProcComponent
    {
        private ChangeSignatureDialog_InProc()
        {
        }

        public static ChangeSignatureDialog_InProc Create()
            => new ChangeSignatureDialog_InProc();

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
                        Thread.Yield();
                        continue;
                    }

                    WaitForApplicationIdle();
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

        public void ClickDownButton()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.DownButton, cancellationTokenSource.Token));
            }
        }

        public void ClickUpButton()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.UpButton, cancellationTokenSource.Token));
            }
        }

        public void ClickRemoveButton()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.RemoveButton, cancellationTokenSource.Token));
            }
        }

        public void ClickRestoreButton()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.RestoreButton, cancellationTokenSource.Token));
            }
        }

        public void SelectParameter(string parameterName)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    var members = dialog.GetTestAccessor().Members;
                    members.SelectedItem = dialog.GetTestAccessor().ViewModel.AllParameters.Single(p => p.ParameterAutomationText == parameterName);
                });
            }
        }

        private async Task<ChangeSignatureDialog> GetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<ChangeSignatureDialog>().Single();
        }

        private async Task<ChangeSignatureDialog> TryGetDialogAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            return Application.Current.Windows.OfType<ChangeSignatureDialog>().SingleOrDefault();
        }

        private async Task ClickAsync(Func<ChangeSignatureDialog.TestAccessor, ButtonBase> buttonSelector, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            var dialog = await GetDialogAsync(cancellationToken);
            var button = buttonSelector(dialog.GetTestAccessor());
            Contract.ThrowIfFalse(await button.SimulateClickAsync(JoinableTaskFactory));
        }
    }
}
