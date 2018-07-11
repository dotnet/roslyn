// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class GenerateTypeDialog_InProc2 : InProcComponent2
    {
        public GenerateTypeDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        internal async Task<GenerateTypeDialog> VerifyOpenAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var window = await TryGetDialogAsync();
                if (window is null)
                {
                    // Task.Yield is insufficient; something in the light bulb must be relying on a UI thread
                    // message at lower priority than the Background priority used in testing.
                    await WaitForApplicationIdleAsync(cancellationToken);
                    continue;
                }

                await WaitForApplicationIdleAsync(cancellationToken);
                return window;
            }
        }

        internal async Task VerifyClosedAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var window = await TryGetDialogAsync();
                if (window is null)
                {
                    return;
                }

                await Task.Yield();
            }
        }

        internal async Task<GenerateTypeDialog> GetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<GenerateTypeDialog>().Single();
        }

        internal async Task<GenerateTypeDialog> TryGetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<GenerateTypeDialog>().SingleOrDefault();
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

        public async Task SetAccessibilityAsync(string accessibility)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();

            Assert.Contains(accessibility, dialog.GetTestAccessor().AccessListComboBox.ItemsSource.Cast<string>());
            Assert.True(dialog.GetTestAccessor().AccessListComboBox.IsEnabled);
            Assert.True(dialog.GetTestAccessor().AccessListComboBox.IsVisible);

            dialog.GetTestAccessor().AccessListComboBox.SelectedItem = accessibility;

            // Wait for changes to propagate
            await Task.Yield();

            Assert.Equal(accessibility, dialog.GetTestAccessor().AccessListComboBox.SelectedItem);
        }

        public async Task SetKindAsync(string kind)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();

            Assert.Contains(kind, dialog.GetTestAccessor().KindListComboBox.ItemsSource.Cast<string>());
            Assert.True(dialog.GetTestAccessor().KindListComboBox.IsEnabled);
            Assert.True(dialog.GetTestAccessor().KindListComboBox.IsVisible);

            dialog.GetTestAccessor().KindListComboBox.SelectedItem = kind;

            // Wait for changes to propagate
            await Task.Yield();

            Assert.Equal(kind, dialog.GetTestAccessor().KindListComboBox.SelectedItem);
        }

        public async Task SetTargetProjectAsync(string projectName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();

            var itemSource = (List<GenerateTypeDialogViewModel.ProjectSelectItem>)dialog.GetTestAccessor().ProjectListComboBox.ItemsSource;
            var index = itemSource.FindIndex(item => item.Name == projectName);
            Assert.True(index >= 0, $"Failed to find project '{projectName}'");
            Assert.True(dialog.GetTestAccessor().ProjectListComboBox.IsEnabled);
            Assert.True(dialog.GetTestAccessor().ProjectListComboBox.IsVisible);

            dialog.GetTestAccessor().ProjectListComboBox.SelectedIndex = index;

            // Wait for changes to propagate
            await Task.Yield();

            Assert.Equal(projectName, ((GenerateTypeDialogViewModel.ProjectSelectItem)dialog.GetTestAccessor().ProjectListComboBox.SelectedItem).Name);
        }

        public async Task SetTargetFileToNewNameAsync(string newFileName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            dialog.GetTestAccessor().CreateNewFileRadioButton.SimulateClick();

            // Wait for changes to propagate
            await Task.Yield();

            Assert.True(dialog.GetTestAccessor().CreateNewFileRadioButton.IsChecked);

            var newFileItems = (IList<string>)dialog.GetTestAccessor().CreateNewFileComboBox.ItemsSource;
            var index = newFileItems.IndexOf(newFileName);
            if (index < 0)
            {
                Assert.True(dialog.GetTestAccessor().CreateNewFileComboBox.IsEditable);
                dialog.GetTestAccessor().CreateNewFileComboBox.Text = newFileName;
            }
            else
            {
                dialog.GetTestAccessor().CreateNewFileComboBox.SelectedIndex = index;
            }

            // Wait for changes to propagate
            await Task.Yield();
        }

        public async Task SetTargetFileToExistingAsync(string existingFileName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            dialog.GetTestAccessor().AddToExistingFileRadioButton.SimulateClick();

            // Wait for changes to propagate
            await Task.Yield();

            dialog.GetTestAccessor().AddToExistingFileComboBox.SelectedItem = existingFileName;

            // Wait for changes to propagate
            await Task.Yield();
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the related Code Action to complete.
        /// </summary>
        public async Task ClickOkAsync()
        {
            await ClickAsync(testAccessor => testAccessor.OKButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the related Code Action to complete.
        /// </summary>
        public async Task ClickCancelAsync()
        {
            await ClickAsync(testAccessor => testAccessor.CancelButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        private async Task ClickAsync(Func<GenerateTypeDialog.TestAccessor, ButtonBase> buttonSelector)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            var button = buttonSelector(dialog.GetTestAccessor());
            Assert.True(button.SimulateClick());
        }

        public async Task<string[]> GetNewFileComboBoxItemsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = await GetDialogAsync();
            return dialog.GetTestAccessor().CreateNewFileComboBox.Items.Cast<string>().ToArray();
        }
    }
}
