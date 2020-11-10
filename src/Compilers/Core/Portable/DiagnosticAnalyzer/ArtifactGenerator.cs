// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for artifact generators that can programmatically produce additional files for a compilation.  All
    /// implementations of this must support concurrent execution of the various GenerateArtifacts overloads.
    /// </summary>
    /// <remarks>
    /// Normally, an artifact generator will not need to report diagnostics.  However, it may sometimes be necessary if
    /// errors or other issues arise during generation.  If diagnostic reporting is needed, then the same mechanism are
    /// available as with normal analyzers.  Specifically, <see cref="SupportedDiagnostics"/> should be overridden to
    /// state which diagnostics may be produced, and the various context `ReportDiagnostic` calls should be used to
    /// report them.
    /// </remarks>
    public abstract class ArtifactGenerator : DiagnosticAnalyzer
    {
        // By default artifact generators don't report diagnostics.  However, they are still allowed to if they run into
        // any issues.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty;

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationAction(
                context =>
                {
                    if (context._artifactCallback != null)
                        this.GenerateArtifacts(new CompilationArtifactGenerationContext(context, context._artifactCallback));
                });

            context.RegisterSemanticModelAction(
                context =>
                {
                    if (context._artifactCallback != null)
                        this.GenerateArtifacts(new SemanticModelArtifactGenerationContext(context, context._artifactCallback));
                });

            context.RegisterSyntaxTreeAction(
                context =>
                {
                    if (context._artifactCallback != null)
                        this.GenerateArtifacts(new SyntaxTreeArtifactGenerationContext(context, context._artifactCallback));
                });

            context.RegisterAdditionalFileAction(
                context =>
                {
                    if (context._artifactCallback != null)
                        this.GenerateArtifacts(new AdditionalFileArtifactGenerationContext(context, context._artifactCallback));
                });
        }

        /// <summary>
        /// Override to support generating artifacts for an entire compilation.
        /// </summary>
        public virtual void GenerateArtifacts(CompilationArtifactGenerationContext context)
        {
        }

        /// <summary>
        /// Override to support generating artifacts for a particular semantic model.
        /// </summary>
        public virtual void GenerateArtifacts(SemanticModelArtifactGenerationContext context)
        {
        }

        /// <summary>
        /// Override to support generating artifacts for a particular syntax tree.
        /// </summary>
        public virtual void GenerateArtifacts(SyntaxTreeArtifactGenerationContext context)
        {
        }

        /// <summary>
        /// Override to support generating artifacts for a particular syntax tree.
        /// </summary>
        public virtual void GenerateArtifacts(AdditionalFileArtifactGenerationContext context)
        {
        }
    }
}
