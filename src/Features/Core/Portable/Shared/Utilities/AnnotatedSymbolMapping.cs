// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal readonly struct AnnotatedSymbolMapping
    {
        public Dictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; }
        public Solution AnnotatedSolution { get; }
        public ImmutableArray<DocumentId> DocumentIds { get; }
        public SyntaxAnnotation TypeNodeAnnotation { get; }

        public AnnotatedSymbolMapping(
                Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
                Solution annotatedSolution,
                ImmutableArray<DocumentId> documentIds,
                SyntaxAnnotation typeNodeAnnotation)
        {
            SymbolToDeclarationAnnotationMap = symbolToDeclarationAnnotationMap;
            AnnotatedSolution = annotatedSolution;
            DocumentIds = documentIds;
            TypeNodeAnnotation = typeNodeAnnotation;
        }

        public static async Task<AnnotatedSymbolMapping> CreateAsync(
                IEnumerable<ISymbol> symbols,
                Solution solution,
                SyntaxNode typeNode,
                CancellationToken cancellationToken)
        {
            var symbolToDeclarationAnnotationMap = new Dictionary<ISymbol, SyntaxAnnotation>();
            var currentRoots = new Dictionary<SyntaxTree, SyntaxNode>();
            using var _ = ArrayBuilder<DocumentId>.GetInstance(out var documentIds);

            var typeNodeRoot = await typeNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNodeAnnotation = new SyntaxAnnotation();
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));
            documentIds.Add(solution.GetRequiredDocument(typeNode.SyntaxTree).Id);

            foreach (var symbol in symbols)
            {
                var location = symbol.Locations.Single();
                var tree = location.SourceTree!;
                if (!currentRoots.TryGetValue(tree, out var root))
                {
                    root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    documentIds.Add(solution.GetRequiredDocument(tree).Id);
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();
                symbolToDeclarationAnnotationMap.Add(symbol, annotation);
                currentRoots[tree] = root.ReplaceToken(token, token.WithAdditionalAnnotations(annotation));
            }

            var annotatedSolution = solution;
            foreach (var root in currentRoots)
            {
                var document = annotatedSolution.GetRequiredDocument(root.Key);
                annotatedSolution = document.WithSyntaxRoot(root.Value).Project.Solution;
            }

            return new AnnotatedSymbolMapping(symbolToDeclarationAnnotationMap, annotatedSolution, documentIds.ToImmutable(), typeNodeAnnotation);
        }
    }
}
