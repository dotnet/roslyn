// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ChangeSignatureDialog_InProc : AbstractCodeRefactorDialog_InProc<ChangeSignatureDialog, ChangeSignatureDialog.TestAccessor>
    {
        private ChangeSignatureDialog_InProc()
        {
        }

        public static ChangeSignatureDialog_InProc Create()
            => new ChangeSignatureDialog_InProc();

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

        protected override ChangeSignatureDialog.TestAccessor GetAccessor(ChangeSignatureDialog dialog) => dialog.GetTestAccessor();
    }
}
