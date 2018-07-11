// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
    }
}
