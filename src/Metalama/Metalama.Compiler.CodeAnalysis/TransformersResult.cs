using System;
using System.Collections.Immutable;
using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler
{

    internal record TransformersResult(
        Compilation AnnotatedInputCompilation,
        Compilation TransformedCompilation,
        ImmutableArray<SyntaxTreeTransformation> TransformedTrees,
        DiagnosticFilters DiagnosticFilters,
        ImmutableArray<ResourceDescription> AdditionalResources,
        AnalyzerConfigOptionsProvider MappedAnalyzerOptions )
    {
        public bool Success { get; private init; } = true;

        public static TransformersResult Empty(Compilation compilation, AnalyzerConfigOptionsProvider analyzerOptions )
            => new TransformersResult(
                compilation,
                compilation,
                ImmutableArray<SyntaxTreeTransformation>.Empty, 
                DiagnosticFilters.Empty,
                ImmutableArray<ResourceDescription>.Empty,
                analyzerOptions);

        public static TransformersResult Failure(Compilation compilation)
            => new TransformersResult(
                compilation,
                compilation,
                ImmutableArray<SyntaxTreeTransformation>.Empty,
                DiagnosticFilters.Empty,
                ImmutableArray<ResourceDescription>.Empty,
                CompilerAnalyzerConfigOptionsProvider.Empty) 
            {
                Success = false
            };
    }
}
