// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
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
                    dialog.CallSiteValueTextBox.Text = callSiteValue;
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
                    ((IntellisenseTextBox)dialog.NameContentControl.Content).Text = parameterName;
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
                    ((IntellisenseTextBox)dialog.TypeContentControl.Content).Text = typeName;
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
