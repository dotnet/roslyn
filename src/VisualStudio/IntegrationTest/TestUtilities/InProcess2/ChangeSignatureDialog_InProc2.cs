// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class ChangeSignatureDialog_InProc2 : InProcComponent2
    {
        public ChangeSignatureDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        private Editor_InProc2 Editor => TestServices.Editor;

        internal async Task<ChangeSignatureDialog> VerifyOpenAsync()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                while (true)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var window = await TryGetDialogAsync();
                    if (window is null)
                    {
                        await Task.Yield();
                        continue;
                    }

                    await WaitForApplicationIdleAsync(cancellationTokenSource.Token);
                    return window;
                }
            }
        }

        internal async Task VerifyClosedAsync()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                while (true)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var window = await TryGetDialogAsync();
                    if (window is null)
                    {
                        return;
                    }

                    await Task.Yield();
                }
            }
        }

        internal async Task<ChangeSignatureDialog> GetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<ChangeSignatureDialog>().Single();
        }

        internal async Task<ChangeSignatureDialog> TryGetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<ChangeSignatureDialog>().SingleOrDefault();
        }

        public async Task<bool> CloseWindowAsync()
        {
            if (await TryGetDialogAsync() is null)
            {
                return false;
            }

            await ClickCancelAsync();
            return true;
        }

        public async Task InvokeAsync()
            => await Editor.SendKeysAsync(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.V, ShiftState.Ctrl));

        public async Task ClickOkAsync()
            => await ClickAsync(testAccessor => testAccessor.OKButton);

        public async Task ClickCancelAsync()
            => await ClickAsync(testAccessor => testAccessor.CancelButton);

        public async Task ClickDownAsync()
            => await ClickAsync(testAccessor => testAccessor.DownButton);

        public async Task ClickUpAsync()
            => await ClickAsync(testAccessor => testAccessor.UpButton);

        public async Task ClickRemoveAsync()
            => await ClickAsync(testAccessor => testAccessor.RemoveButton);

        public async Task ClickRestoreAsync()
            => await ClickAsync(testAccessor => testAccessor.RestoreButton);

        private async Task ClickAsync(Func<ChangeSignatureDialog.TestAccessor, ButtonBase> buttonSelector)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            var button = buttonSelector(dialog.GetTestAccessor());
            Assert.True(button.SimulateClick());
        }

        public async Task SelectParameterAsync(string parameterName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            var members = dialog.GetTestAccessor().Members;
            members.SelectedItem = dialog.GetTestAccessor().ViewModel.AllParameters.Single(p => p.ParameterAutomationText == parameterName);
        }
    }
}
