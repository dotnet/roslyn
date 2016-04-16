// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class DiagnosticTableControlEventProcessorProvider
    {
        private class AggregateDiagnosticTableControlEventProcessor : EventProcessor
        {
            private readonly ImmutableArray<EventProcessor> _additionalEventProcessors;
            public AggregateDiagnosticTableControlEventProcessor(params EventProcessor[] additionalEventProcessors)
            {
                _additionalEventProcessors = additionalEventProcessors.ToImmutableArray();
            }

            public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
            {
                base.PostprocessSelectionChanged(e);
                foreach (var processor in _additionalEventProcessors)
                {
                    processor.PostprocessSelectionChanged(e);
                }
            }
        }
    }
}
