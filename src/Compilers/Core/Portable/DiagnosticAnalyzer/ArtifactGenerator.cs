// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for artifact generators that can programmatically produce additional files for a compilation.
    /// </summary>
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
                context => this.GenerateArtifacts(new CompilationArtifactGenerationContext(context)));

            context.RegisterSemanticModelAction(
                context => this.GenerateArtifacts(new SemanticModelArtifactGenerationContext(context)));

            context.RegisterSyntaxTreeAction(
                context => this.GenerateArtifacts(new SyntaxTreeArtifactGenerationContext(context)));

            context.RegisterAdditionalFileAction(
                context => this.GenerateArtifacts(new AdditionalFileArtifactGenerationContext(context)));
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
