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
    internal class AnnotatedSymbolMapping
    {
        public ImmutableDictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; }
        public Solution AnnotatedSolution { get; }
        public ImmutableArray<DocumentId> DocumentIds { get; }
        public SyntaxAnnotation TypeNodeAnnotation { get; }

        public AnnotatedSymbolMapping(
            ImmutableDictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            Solution annotatedSolution,
            ImmutableArray<DocumentId> documentIds,
            SyntaxAnnotation typeNodeAnnotation)
        {
            SymbolToDeclarationAnnotationMap = symbolToDeclarationAnnotationMap;
            AnnotatedSolution = annotatedSolution;
            DocumentIds = documentIds;
            TypeNodeAnnotation = typeNodeAnnotation;
        }

        /// <summary>
        /// Creates a <see cref="AnnotatedSymbolMapping"/> where the first token of each symbol is annotated
        /// and added to a map to keep track. This allows modification of the trees and later lookup of symbols
        /// based on the original annotations added. Assumes each symbol only has one location.
        /// </summary>
        public static async Task<AnnotatedSymbolMapping> CreateAsync(
            IEnumerable<ISymbol> symbols,
            Solution solution,
            SyntaxNode typeNode,
            CancellationToken cancellationToken)
        {
            using var _ = PooledDictionary<ISymbol, SyntaxAnnotation>.GetInstance(out var symbolToDeclarationAnnotationMap);
            using var _1 = PooledDictionary<SyntaxTree, SyntaxNode>.GetInstance(out var currentRoots);
            using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var documentIds);

            var typeNodeRoot = await typeNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNodeAnnotation = new SyntaxAnnotation();

            // CurrentRoots will keep track of the root node with annotations added
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));

            // DocumentIds will track all of the documents where annotations were added since 
            // it's not guaranteed that all of the symbols are in the same document
            documentIds.Add(solution.GetRequiredDocument(typeNode.SyntaxTree).Id);

            foreach (var symbol in symbols)
            {
                var location = symbol.Locations.Single();
                var tree = location.SourceTree!;

                // If there's not currently an entry for this tree then make sure to add it
                if (!currentRoots.TryGetValue(tree, out var root))
                {
                    root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    documentIds.Add(solution.GetRequiredDocument(tree).Id);
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();

                // Add the instance of the annotation used to annotate this symbol so it
                // can be retrieved later
                symbolToDeclarationAnnotationMap.Add(symbol, annotation);

                // Store the updated root node with the annotation added
                currentRoots[tree] = root.ReplaceToken(token, token.WithAdditionalAnnotations(annotation));
            }

            // Make sure each document is updated with the annotated root
            var annotatedSolution = solution;
            foreach (var root in currentRoots)
            {
                var document = annotatedSolution.GetRequiredDocument(root.Key);
                annotatedSolution = document.WithSyntaxRoot(root.Value).Project.Solution;
            }

            return new AnnotatedSymbolMapping(symbolToDeclarationAnnotationMap.ToImmutableDictionary(), annotatedSolution, documentIds.ToImmutable(), typeNodeAnnotation);
        }
    }
}
