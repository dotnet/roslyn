// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class AnnotatedSymbolMapping(
        ImmutableDictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
        Solution annotatedSolution,
        ImmutableDictionary<DocumentId, ImmutableArray<ISymbol>> documentIdsToSymbolMap,
        SyntaxAnnotation typeNodeAnnotation)
    {
        /// <summary>
        /// Used to map a symbol to the annotation that was added at the beginning of it's definition. Use the 
        /// annotation the symbol declaration again across edits.
        /// </summary>
        public ImmutableDictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; } = symbolToDeclarationAnnotationMap;

        /// <summary>
        /// The original solution that modifications made to annotate the symbol declarations
        /// </summary>
        public Solution AnnotatedSolution { get; } = annotatedSolution;

        /// <summary>
        /// A map of the document ids that were used and what symbols are in them. 
        /// </summary>
        public ImmutableDictionary<DocumentId, ImmutableArray<ISymbol>> DocumentIdsToSymbolMap { get; } = documentIdsToSymbolMap;

        /// <summary>
        /// The annotation added to the type declaration that was passed in to create the mapping
        /// </summary>
        public SyntaxAnnotation TypeNodeAnnotation { get; } = typeNodeAnnotation;

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
            using var _2 = PooledDictionary<DocumentId, List<ISymbol>>.GetInstance(out var documentIdToSymbolsMap);

            var typeNodeRoot = await typeNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNodeAnnotation = new SyntaxAnnotation();

            // CurrentRoots will keep track of the root node with annotations added
            currentRoots[typeNode.SyntaxTree] = typeNodeRoot.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(typeNodeAnnotation));

            // DocumentIds will track all of the documents where annotations were added since 
            // it's not guaranteed that all of the symbols are in the same document
            documentIdToSymbolsMap.Add(solution.GetRequiredDocument(typeNode.SyntaxTree).Id, new List<ISymbol>());

            foreach (var symbol in symbols)
            {
                var location = symbol.Locations.Single();
                var tree = location.SourceTree!;
                var id = solution.GetRequiredDocument(tree).Id;

                // If there's not currently an entry for this tree then make sure to add it
                if (!currentRoots.TryGetValue(tree, out var root))
                {
                    root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    documentIdToSymbolsMap.Add(id, new List<ISymbol>());
                }

                var token = root.FindToken(location.SourceSpan.Start);

                var annotation = new SyntaxAnnotation();

                // Add the instance of the annotation used to annotate this symbol so it
                // can be retrieved later
                symbolToDeclarationAnnotationMap.Add(symbol, annotation);

                // Add the symbol to list of symbols contained in the document
                var symbolsInDocument = documentIdToSymbolsMap[id];
                symbolsInDocument.Add(symbol);

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

            var immutableDocumentIdToSymbolsMap = documentIdToSymbolsMap.ToImmutableDictionary(
                keySelector: (kvp) => kvp.Key,
                elementSelector: (kvp) => kvp.Value.ToImmutableArray());
            return new AnnotatedSymbolMapping(symbolToDeclarationAnnotationMap.ToImmutableDictionary(), annotatedSolution, immutableDocumentIdToSymbolsMap, typeNodeAnnotation);
        }
    }
}
