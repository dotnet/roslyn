using System;
using System.Collections.Immutable;
using Metalama.Compiler;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{

    internal record TransformersResult(Compilation AnnotatedInputCompilation, Compilation TransformedCompilation,
        ImmutableArray<SyntaxTreeTransformation> TransformedTrees,
        DiagnosticFilters DiagnosticFilters,
        ImmutableArray<ResourceDescription> AdditionalResources)
    {
        public bool Success { get; private init; } = true;

        public static TransformersResult Empty(Compilation compilation)
            => new TransformersResult(compilation, compilation,
                ImmutableArray<SyntaxTreeTransformation>.Empty, DiagnosticFilters.Empty,
                ImmutableArray<ResourceDescription>.Empty);

        public static TransformersResult Failure(Compilation compilation)
            => new TransformersResult(compilation, compilation,
                ImmutableArray<SyntaxTreeTransformation>.Empty, DiagnosticFilters.Empty,
                ImmutableArray<ResourceDescription>.Empty) { Success = false };
    }
}
