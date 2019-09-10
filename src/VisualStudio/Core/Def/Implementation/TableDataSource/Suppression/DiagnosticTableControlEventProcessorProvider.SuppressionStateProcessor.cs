// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class DiagnosticTableControlEventProcessorProvider
    {
        private partial class SuppressionStateEventProcessor : EventProcessor
        {
            private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

            public SuppressionStateEventProcessor(VisualStudioDiagnosticListSuppressionStateService suppressionStateService)
            {
                _suppressionStateService = suppressionStateService;
            }

            public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
            {
                // Update the suppression state information for the new error list selection.
                _suppressionStateService.ProcessSelectionChanged(e);
            }
        }
    }
}
