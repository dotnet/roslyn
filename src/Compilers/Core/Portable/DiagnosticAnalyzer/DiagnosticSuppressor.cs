// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for diagnostic suppressors that can programmatically suppress analyzer and/or compiler non-error diagnostics.
    /// </summary>
    public abstract class DiagnosticSuppressor : DiagnosticAnalyzer
    {
        // Disallow suppressors from reporting diagnostics or registering analysis actions.
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

#pragma warning disable RS1026
        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        }
#pragma warning restore RS1026

        /// <summary>
        /// Returns a set of descriptors for the suppressions that this suppressor is capable of producing.
        /// </summary>
        public abstract ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

        /// <summary>
        /// Suppress analyzer and/or compiler non-error diagnostics reported for the compilation.
        /// This may be a subset of the full set of reported diagnostics, as an optimization for
        /// supporting incremental and partial analysis scenarios.
        /// A diagnostic is considered suppressible by a DiagnosticSuppressor if *all* of the following conditions are met:
        ///     1. Diagnostic is not already suppressed in source via pragma/suppress message attribute.
        ///     2. Diagnostic's <see cref="Diagnostic.DefaultSeverity"/> is not <see cref="DiagnosticSeverity.Error"/>.
        ///     3. Diagnostic is not tagged with <see cref="WellKnownDiagnosticTags.NotConfigurable"/> custom tag.
        /// </summary>
        public abstract void ReportSuppressions(SuppressionAnalysisContext context);
    }
}
