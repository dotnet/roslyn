// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class ExtractInterfaceDialog_InProc2 : InProcComponent2
    {
        public ExtractInterfaceDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently open.
        /// </summary>
        internal async Task<ExtractInterfaceDialog> VerifyOpenAsync()
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                while (true)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var window = await TryGetDialogAsync();
                    if (window is null)
                    {
                        // Task.Yield is insufficient; something in the light bulb must be relying on a UI thread
                        // message at lower priority than the Background priority used in testing.
                        await WaitForApplicationIdleAsync(cancellationTokenSource.Token);
                        continue;
                    }

                    await WaitForApplicationIdleAsync(cancellationTokenSource.Token);
                    return window;
                }
            }
        }

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently closed.
        /// </summary>
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

        public async Task<bool> CloseWindowAsync()
        {
            if (await TryGetDialogAsync() is null)
            {
                return false;
            }

            await ClickCancelAsync();
            return true;
        }

        internal async Task<ExtractInterfaceDialog> GetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<ExtractInterfaceDialog>().Single();
        }

        internal async Task<ExtractInterfaceDialog> TryGetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<ExtractInterfaceDialog>().SingleOrDefault();
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public async Task ClickOkAsync()
        {
            await ClickAsync(testAccessor => testAccessor.OKButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public async Task ClickCancelAsync()
        {
            await ClickAsync(testAccessor => testAccessor.CancelButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        /// <summary>
        ///  Clicks the "Select All" button.
        /// </summary>
        public async Task ClickSelectAllAsync()
            => await ClickAsync(testAccessor => testAccessor.SelectAllButton);

        /// <summary>
        /// Clicks the "Deselect All" button.
        /// </summary>
        public async Task ClickDeselectAllAsync()
            => await ClickAsync(testAccessor => testAccessor.DeselectAllButton);

        /// <summary>
        /// Returns the name of the generated file that will contain the interface.
        /// </summary>
        public async Task<string> GetTargetFileNameAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = await GetDialogAsync();

            return dialog.fileNameTextBox.Text;
        }

        /// <summary>
        /// Gets the set of members that are currently checked.
        /// </summary>
        public async Task<string[]> GetSelectedItemsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = await GetDialogAsync();

            var memberSelectionList = dialog.GetTestAccessor().Members;
            var comListItems = memberSelectionList.Items;
            var listItems = Enumerable.Range(0, comListItems.Count).Select(comListItems.GetItemAt);

            return listItems.Cast<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>()
                .Where(viewModel => viewModel.IsChecked)
                .Select(viewModel => viewModel.MemberName)
                .ToArray();
        }

        /// <summary>
        /// Clicks the checkbox on the given item, cycling it from on to off or from off to on.
        /// </summary>
        /// <param name="item"></param>
        public async Task ToggleItemAsync(string item)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = await GetDialogAsync();

            var memberSelectionList = dialog.GetTestAccessor().Members;
            var items = memberSelectionList.Items.Cast<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>().ToArray();
            var itemViewModel = items.Single(x => x.MemberName == item);
            itemViewModel.IsChecked = !itemViewModel.IsChecked;

            // Wait for changes to propagate
            await Task.Yield();
        }

        private async Task ClickAsync(Func<ExtractInterfaceDialog.TestAccessor, ButtonBase> buttonSelector)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            var button = buttonSelector(dialog.GetTestAccessor());
            Assert.True(await button.SimulateClickAsync(JoinableTaskFactory));
        }
    }
}
