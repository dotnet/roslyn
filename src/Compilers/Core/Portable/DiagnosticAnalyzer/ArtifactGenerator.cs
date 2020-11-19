// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for artifact producers that can programmatically generate additional non-code files during a
    /// compilation.  Artifact generators only run when a compiler is invoked with the <c>generatedfilesout</c>
    /// parameter.
    /// </summary>
    /// <remarks>
    /// Normally, an artifact generator will not need to report diagnostics.  However, it may sometimes be necessary if
    /// errors or other issues arise during generation.  If diagnostic reporting is needed, then the same mechanism are
    /// available as with normal analyzers.  Specifically, <see cref="SupportedDiagnostics"/> should be overridden to
    /// state which diagnostics may be produced, and the various context `ReportDiagnostic` calls should be used to
    /// report them.
    /// </remarks>
    public abstract class ArtifactProducer : DiagnosticAnalyzer
    {
        // By default artifact generators don't report diagnostics.  However, they are still allowed to if they run into
        // any issues.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty;

#pragma warning disable RS1025 // Configure generated code analysis
#pragma warning disable RS1026 // Enable concurrent execution
        public sealed override void Initialize(AnalysisContext context) { }
#pragma warning restore RS1026 // Enable concurrent execution
#pragma warning restore RS1025 // Configure generated code analysis

        /// <summary>
        /// Called once at session start to register actions in the <paramref name="analysisContext"/>.  The provided
        /// <paramref name="artifactContext"/> can be used to generate artifacts as appropriate on the desired compiler
        /// events.
        /// </summary>
        public abstract void Initialize(AnalysisContext analysisContext, ArtifactGenerationContext artifactContext);
    }
}
