// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Concurrent;
using System.Threading;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public sealed class CodeMetricsAnalysisContext
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelMap;
        private readonly Func<SyntaxTree, SemanticModel> _getSemanticModel;

        public CodeMetricsAnalysisContext(Compilation compilation, CancellationToken cancellationToken,
            Func<INamedTypeSymbol, bool>? isExcludedFromInheritanceCountFunc = null)
        {
            Compilation = compilation;
            WellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            CancellationToken = cancellationToken;
            _semanticModelMap = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
            IsExcludedFromInheritanceCountFunc = isExcludedFromInheritanceCountFunc ?? (x => false); // never excluded by default

            _getSemanticModel = tree => Compilation.GetSemanticModel(tree);
        }

        public Compilation Compilation { get; }
        public WellKnownTypeProvider WellKnownTypeProvider { get; }
        public CancellationToken CancellationToken { get; }
        public Func<INamedTypeSymbol, bool> IsExcludedFromInheritanceCountFunc { get; }

        internal SemanticModel GetSemanticModel(SyntaxNode node)
            => _semanticModelMap.GetOrAdd(node.SyntaxTree, _getSemanticModel);
    }
}

#endif
