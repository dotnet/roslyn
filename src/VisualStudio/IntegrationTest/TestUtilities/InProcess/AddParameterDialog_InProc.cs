// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class AddParameterDialog_InProc : AbstractCodeRefactorDialog_InProc<AddParameterDialog, AddParameterDialog.TestAccessor>
    {
        private AddParameterDialog_InProc()
        {
        }

        public static AddParameterDialog_InProc Create()
            => new AddParameterDialog_InProc();

        public void FillCallSiteField(string callSiteValue)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    dialog.CallsiteValueTextBox.Focus();
                    dialog.CallsiteValueTextBox.Text = callSiteValue;
                });
            }
        }

        public void FillNameField(string parameterName)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    dialog.NameContentControl.Focus();
                    dialog.NameContentControl.Text = parameterName;
                });
            }
        }

        public void SetCallSiteTodo()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    dialog.IntroduceErrorRadioButton.Focus();
                    dialog.IntroduceErrorRadioButton.IsChecked = true;
                });
            }
        }

        public void FillTypeField(string typeName)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    dialog.TypeContentControl.Focus();
                    dialog.TypeContentControl.Text = typeName;
                });
            }
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

        protected override AddParameterDialog.TestAccessor GetAccessor(AddParameterDialog dialog) => dialog.GetTestAccessor();
    }
}
