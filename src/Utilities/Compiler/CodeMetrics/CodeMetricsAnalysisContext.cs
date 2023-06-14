// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public sealed class CodeMetricsAnalysisContext(Compilation compilation, CancellationToken cancellationToken,
        Func<INamedTypeSymbol, bool>? isExcludedFromInheritanceCountFunc = null)
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelMap = new();

        public Compilation Compilation { get; } = compilation;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public Func<INamedTypeSymbol, bool> IsExcludedFromInheritanceCountFunc { get; } = isExcludedFromInheritanceCountFunc ?? (x => false); // never excluded by default

        internal SemanticModel GetSemanticModel(SyntaxNode node)
            => _semanticModelMap.GetOrAdd(node.SyntaxTree, tree => Compilation.GetSemanticModel(node.SyntaxTree));
    }
}

#endif
