// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver
    {
        /// <summary>
        /// Used to represent state of processing of a <see cref="CompilationEvent"/>.
        /// </summary>
        private sealed class EventProcessedState
        {
            public static readonly EventProcessedState Processed = new EventProcessedState(EventProcessedStateKind.Processed);
            public static readonly EventProcessedState NotProcessed = new EventProcessedState(EventProcessedStateKind.NotProcessed);

            public EventProcessedStateKind Kind { get; }

            /// <summary>
            /// Subset of processed analyzers.
            /// NOTE: This property is only non-null for <see cref="EventProcessedStateKind.PartiallyProcessed"/>.
            /// </summary>
            public ImmutableArray<DiagnosticAnalyzer> SubsetProcessedAnalyzers { get; }

            private EventProcessedState(EventProcessedStateKind kind)
            {
                Kind = kind;
                SubsetProcessedAnalyzers = default;
            }

            private EventProcessedState(ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers)
            {
                SubsetProcessedAnalyzers = subsetProcessedAnalyzers;
                Kind = EventProcessedStateKind.PartiallyProcessed;
            }

            public static EventProcessedState CreatePartiallyProcessed(ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers)
            {
                return new EventProcessedState(subsetProcessedAnalyzers);
            }
        }

        private enum EventProcessedStateKind
        {
            Processed,
            NotProcessed,
            PartiallyProcessed
        }
    }
}
