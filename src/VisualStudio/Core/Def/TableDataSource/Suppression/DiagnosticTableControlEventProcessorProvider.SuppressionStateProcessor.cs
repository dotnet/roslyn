// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class DiagnosticTableControlEventProcessorProvider
    {
        private class SuppressionStateEventProcessor : EventProcessor
        {
            private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

            public SuppressionStateEventProcessor(VisualStudioDiagnosticListSuppressionStateService suppressionStateService)
                => _suppressionStateService = suppressionStateService;

            public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
            {
                // Update the suppression state information for the new error list selection.
                _suppressionStateService.ProcessSelectionChanged(e);
            }
        }
    }
}
