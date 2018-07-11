// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class PickMembersDialog_InProc2 : InProcComponent2
    {
        public PickMembersDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        internal async Task<PickMembersDialog> VerifyOpenAsync(CancellationToken cancellationToken)
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

        public async Task<bool> CloseWindowAsync()
        {
            if (await TryGetDialogAsync() is null)
            {
                return false;
            }

            await ClickCancelAsync();
            return true;
        }

        internal async Task<PickMembersDialog> GetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<PickMembersDialog>().Single();
        }

        internal async Task<PickMembersDialog> TryGetDialogAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return Application.Current.Windows.OfType<PickMembersDialog>().SingleOrDefault();
        }

        /// <summary>
        /// Clicks the "OK" button and waits for the related Code Action to complete.
        /// </summary>
        internal async Task ClickOkAsync()
        {
            await ClickAsync(testAccessor => testAccessor.OkButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the related Code Action to complete.
        /// </summary>
        internal async Task ClickCancelAsync()
        {
            await ClickAsync(testAccessor => testAccessor.CancelButton);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
        }

        internal async Task ClickDownAsync()
        {
            await ClickAsync(testAccessor => testAccessor.DownButton);
        }

        private async Task ClickAsync(Func<PickMembersDialog.TestAccessor, ButtonBase> buttonSelector)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = await GetDialogAsync();
            var button = buttonSelector(dialog.GetTestAccessor());
            Assert.True(button.IsEnabled, "The button must be enabled before it can be clicked.");
            Assert.True(button.IsVisible, "The button must be visible before it can be clicked.");
            Assert.True(await button.SimulateClickAsync(JoinableTaskFactory));
        }
    }
}
