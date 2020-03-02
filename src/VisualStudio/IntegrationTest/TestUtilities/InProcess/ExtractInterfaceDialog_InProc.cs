// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ExtractInterfaceDialog_InProc : AbstractCodeRefactorDialog_InProc<MoveMembersDialog, MoveMembersDialog.TestAccessor>
    {
        private ExtractInterfaceDialog_InProc()
        {
        }

        public static ExtractInterfaceDialog_InProc Create()
            => new ExtractInterfaceDialog_InProc();

        public override void VerifyOpen()
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

        public void SetAllSelected(bool allSelected)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await ClickAsync(testAccessor => testAccessor.SelectAllCheckBox, cancellationTokenSource.Token);
                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    var testAccessor = GetAccessor(dialog);
                    if (testAccessor.SelectAllCheckBox.IsChecked != allSelected)
                    {
                        await ClickAsync(testAccessor => testAccessor.SelectAllCheckBox, cancellationTokenSource.Token);
                    }
                });
            }
        }
        public void ClickSelectAll()
        {
            SetAllSelected(true);
        }

        public void ClickDeselectAll()
        {
            SetAllSelected(false);
        }

        public void SelectSameFile()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(() => ClickAsync(testAccessor => testAccessor.DestinationCurrentFileSelectionRadioButton, cancellationTokenSource.Token));
            }
        }

        public string GetTargetFileName()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                return JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);
                    var testAccessor = GetAccessor(dialog);
                    return testAccessor.MoveToNewTypeControl.fileNameTextBox.Text;
                });
            }
        }

        public string[] GetSelectedItems()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                return JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);

                    var memberSelectionList = GetAccessor(dialog).MemberSelectionGrid;
                    var comListItems = memberSelectionList.Items;
                    var listItems = Enumerable.Range(0, comListItems.Count).Select(comListItems.GetItemAt);

                    return listItems.Cast<MoveMembersSymbolViewModel>()
                        .Where(viewModel => viewModel.IsChecked)
                        .Select(viewModel => viewModel.SymbolName)
                        .ToArray();
                });
            }
        }

        public void ToggleItem(string item)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                    var dialog = await GetDialogAsync(cancellationTokenSource.Token);

                    var memberSelectionList = GetAccessor(dialog).MemberSelectionGrid;
                    var items = memberSelectionList.Items.Cast<MoveMembersSymbolViewModel>().ToArray();
                    var itemViewModel = items.Single(x => x.SymbolName == item);
                    itemViewModel.IsChecked = !itemViewModel.IsChecked;

                    // Wait for changes to propagate
                    await Task.Yield();
                });
            }
        }

        protected override MoveMembersDialog.TestAccessor GetAccessor(MoveMembersDialog dialog) => dialog.GetTestAccessor();
    }
}
