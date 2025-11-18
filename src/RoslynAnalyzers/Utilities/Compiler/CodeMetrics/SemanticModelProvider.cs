// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal sealed class SemanticModelProvider
    {
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelMap;
        public SemanticModelProvider(Compilation compilation)
        {
            Compilation = compilation;
            _semanticModelMap = new ConcurrentDictionary<SyntaxTree, SemanticModel>();
        }

        public Compilation Compilation { get; }

        public SemanticModel GetSemanticModel(SyntaxNode node)
            => _semanticModelMap.GetOrAdd(node.SyntaxTree, tree => Compilation.GetSemanticModel(node.SyntaxTree));
    }
}

#endif
