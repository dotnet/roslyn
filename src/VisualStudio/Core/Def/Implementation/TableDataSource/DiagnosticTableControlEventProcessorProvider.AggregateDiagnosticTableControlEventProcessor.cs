// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                => _additionalEventProcessors = additionalEventProcessors.ToImmutableArray();

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
