// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class PreviewChangesDialog_InProc2 : InProcComponent2
    {
        public PreviewChangesDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        /// <summary>
        /// Verifies that the Preview Changes dialog is showing with the
        /// specified title. The dialog does not have an AutomationId and the 
        /// title can be changed by features, so callers of this method must
        /// specify a title.
        /// </summary>
        /// <param name="expectedTitle"></param>
        internal async Task<PreviewPane> VerifyOpenAsync(string expectedTitle, CancellationToken cancellationToken)
        {
            await TestServices.Dialog.VerifyOpenAsync(expectedTitle, cancellationToken);

            // Wait for application idle to ensure the dialog is fully initialized
            await WaitForApplicationIdleAsync(cancellationToken);

            return null;
        }

        public async Task VerifyClosedAsync(string expectedTitle, CancellationToken cancellationToken)
            => await TestServices.Dialog.VerifyClosedAsync(expectedTitle, cancellationToken);

        public async Task ClickApplyAndWaitForFeatureAsync(string expectedTitle, string featureName)
        {
            await TestServices.Dialog.ClickOKAsync(expectedTitle);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(featureName);
        }

        public async Task ClickCancelAsync(string expectedTitle)
            => await TestServices.Dialog.ClickCancelAsync(expectedTitle);
    }
}
