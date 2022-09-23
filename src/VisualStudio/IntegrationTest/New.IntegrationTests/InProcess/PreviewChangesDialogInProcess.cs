// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class PreviewChangesDialogInProcess
    {
        /// <summary>
        /// Verifies that the Preview Changes dialog is showing with the
        /// specified title. The dialog does not have an AutomationId and the 
        /// title can be changed by features, so callers of this method must
        /// specify a title.
        /// </summary>
        /// <param name="expectedTitle"></param>
        public async Task VerifyOpenAsync(string expectedTitle, CancellationToken cancellationToken)
        {
            await DialogHelpers.FindDialogByNameAsync(JoinableTaskFactory, await TestServices.Shell.GetMainWindowAsync(cancellationToken), expectedTitle, isOpen: true, cancellationToken);

            // Wait for application idle to ensure the dialog is fully initialized
            await WaitForApplicationIdleAsync(cancellationToken);
        }

        public async Task VerifyClosedAsync(string expectedTitle, CancellationToken cancellationToken)
            => await DialogHelpers.FindDialogByNameAsync(JoinableTaskFactory, await TestServices.Shell.GetMainWindowAsync(cancellationToken), expectedTitle, isOpen: false, cancellationToken);

        public async Task ClickApplyAndWaitForFeatureAsync(string expectedTitle, string featureName, CancellationToken cancellationToken)
        {
            await DialogHelpers.PressButtonWithNameFromDialogWithNameAsync(JoinableTaskFactory, await TestServices.Shell.GetMainWindowAsync(cancellationToken), expectedTitle, "Apply", cancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(featureName, cancellationToken);
        }

        public async Task ClickCancelAsync(string expectedTitle, CancellationToken cancellationToken)
            => await DialogHelpers.PressButtonWithNameFromDialogWithNameAsync(JoinableTaskFactory, await TestServices.Shell.GetMainWindowAsync(cancellationToken), expectedTitle, "Cancel", cancellationToken);
    }
}
