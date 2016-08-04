// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService)), Shared]
    internal sealed class CodeLensReferenceService : ICodeLensReferencesService
    {
        public async Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken,
            int maxSearchResults = 0)
        {
            if (solution == null || documentId == null || syntaxNode == null)
            {
                return null;
            }

            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol == null)
            {
                return null;
            }

            using (var cappingCancellationTokenSource = new CancellationTokenSource())
            {
                var aggregateCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cappingCancellationTokenSource.Token, cancellationToken);

                var progress = new CodeLensFindReferencesProgress(
                    symbol, syntaxNode, cappingCancellationTokenSource, maxSearchResults);

                try
                {
                    await
                        SymbolFinder.FindReferencesAsync(symbol, solution, progress, null,
                            aggregateCancellationTokenSource.Token).ConfigureAwait(false);

                    return
                        new ReferenceCount(
                            progress.SearchCap > 0
                                ? Math.Min(progress.ReferencesCount, progress.SearchCap)
                                : progress.ReferencesCount, progress.SearchCapReached);
                }
                catch (OperationCanceledException)
                {
                    if (progress.SearchCapReached)
                    {
                        // search was cancelled, and it was cancelled by us because a cap was reached.
                        return new ReferenceCount(progress.SearchCap, true);
                    }

                    // search was cancelled, but not because of cap.
                    // this always throws.
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool IsQueriedDefinition(ReferencedSymbol reference, ISymbol queriedDefinition)
        {
            return reference.Definition.Locations.Intersect(
                queriedDefinition.Locations, LocationComparer.Instance).Any();
        }

        private static async Task<IEnumerable<Tuple<ReferencedSymbol, bool>>> GetReferences(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            // Exclude the following kind of symbols:
            //  (a) Implicitly declared symbols (such as implicit fields backing properties)
            //  (b) Symbols that can't be referenced by name (such as property getters and setters).
            //  (c) Metadata only symbols, i.e. symbols with no location in source.
            IEnumerable<ReferencedSymbol> filteredReferences = from reference in references
                                                               let symbolDef = reference.Definition
                                                               where FilteringHelpers.FilterReference(symbolDef, reference)
                                                               select reference;

            return filteredReferences.Select(reference => Tuple.Create(reference, IsQueriedDefinition(reference, symbol)));
        }

        private static async Task<Tuple<SyntaxNode, IEnumerable<Tuple<ReferencedSymbol, bool>>>> FindReferencesAsync(Solution solution, Document document, SyntaxNode syntaxNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol == null)
            {
                return null;
            }

            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            // Exclude the following kind of symbols:
            //  (a) Implicitly declared symbols (such as implicit fields backing properties)
            //  (b) Symbols that can't be referenced by name (such as property getters and setters).
            //  (c) Metadata only symbols, i.e. symbols with no location in source.
            IEnumerable<ReferencedSymbol> filteredReferences = from reference in references
                                                               let symbolDef = reference.Definition
                                                               where FilteringHelpers.FilterReference(symbolDef, reference)
                                                               select reference;

            var allReferences =
                filteredReferences.Select(reference => Tuple.Create(reference, IsQueriedDefinition(reference, symbol)));

            // Search through all other projects to see if we can a matching symbol in other projects
            foreach (var additionalProject in document.Project.Solution.Projects)
            {
                if (additionalProject == document.Project)
                {
                    continue;
                }

                var additionalDocument = additionalProject.Documents.FirstOrDefault(d => document.FilePath.Equals(d.FilePath, StringComparison.OrdinalIgnoreCase));
                if (additionalDocument == null)
                {
                    continue;
                }

                var additionalSemanticModel = await additionalDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var token = (await additionalSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false)).FindToken(syntaxNode.Span.Start);
                var node = token.Parent.AncestorsAndSelf().FirstOrDefault(n => n.Span == syntaxNode.Span);

                if (node == null)
                {
                    continue;
                }

                var additionalSymbol = additionalSemanticModel.GetDeclaredSymbol(node, cancellationToken);

                // Sanity check that the symbol is the same
                if (additionalSymbol != null && additionalSymbol.Kind == symbol.Kind && additionalSymbol.Name == symbol.Name)
                {
                    allReferences = allReferences.Concat(await GetReferences(additionalSymbol, solution, cancellationToken).ConfigureAwait(false));
                }
            }

            return Tuple.Create(syntaxNode, allReferences);
        }

        /// <summary>
        /// Returns partial symbol locations whose node does not match the given syntaxNode
        /// </summary>
        /// <param name="symbol">Symbol whose locations are queried</param>
        /// <param name="syntaxNode">Syntax node to compare against to exclude location - actual location being queried</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Partial locations</returns>
        private static IEnumerable<Location> GetPartialLocations(ISymbol symbol, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            // Returns nodes from source not equal to actual location
            IEnumerable<SyntaxNode> syntaxNodes = from syntaxReference in symbol.DeclaringSyntaxReferences
                                                  let candidateSyntaxNode = syntaxReference.GetSyntax(cancellationToken)
                                                  where !(syntaxNode.Span == candidateSyntaxNode.Span &&
                                                          syntaxNode.SyntaxTree.FilePath.Equals(candidateSyntaxNode.SyntaxTree.FilePath, StringComparison.OrdinalIgnoreCase))
                                                  select candidateSyntaxNode;

            // This matches the definition locations to syntax references, ignores metadata locations
            IEnumerable<Location> partialLocations = from currentSyntaxNode in syntaxNodes
                                                     from sourceLocation in symbol.Locations
                                                     where !sourceLocation.IsInMetadata
                                                           && currentSyntaxNode.SyntaxTree.Equals(sourceLocation.SourceTree)
                                                           && currentSyntaxNode.Span.Contains(sourceLocation.SourceSpan)
                                                     select sourceLocation;

            return partialLocations;
        }

        private static IEnumerable<Location> ExtractLocations(Tuple<SyntaxNode, IEnumerable<Tuple<ReferencedSymbol, bool>>> referenceInfo, CancellationToken cancellationToken)
        {
            SyntaxNode definitionSyntaxNode = referenceInfo.Item1;
            IEnumerable<Tuple<ReferencedSymbol, bool>> referencingSymbols = referenceInfo.Item2;

            // Take reference locations from all definitions
            IEnumerable<Location> referenceLocations = from referencedSymbol in referencingSymbols
                                                       from location in referencedSymbol.Item1.Locations
                                                       select location.Location;

            // Exclude the definition we queried for - base references should be 0
            IEnumerable<ISymbol> definitions = from referencedSymbol in referencingSymbols
                                               where !referencedSymbol.Item2    // Item2 indicates if this was the queried symbol definition
                                               select referencedSymbol.Item1.Definition;

            IEnumerable<Location> definitionLocations = definitions.SelectMany(def => def.Locations);

            // Partial types can have more than one declaring syntax references.
            // Add remote locations for all the syntax references except the queried syntax node.
            // To query for the partial locations, filter definition locations that occur in source whose span is part of
            // span of any syntax node from Definition.DeclaringSyntaxReferences except for the queried syntax node.
            IEnumerable<Location> additionalSyntaxLocations = from referencedSymbol in referencingSymbols
                                                              let symbolReference = referencedSymbol.Item1
                                                              let isQueriedDefinition = referencedSymbol.Item2
                                                              where isQueriedDefinition
                                                              from partialLocation in GetPartialLocations(symbolReference.Definition, definitionSyntaxNode, cancellationToken)
                                                              select partialLocation;

            return referenceLocations.Concat(definitionLocations).Concat(additionalSyntaxLocations);
        }

        private static IEnumerable<Location> FilterLocations(IEnumerable<Location> locations)
        {
            // Exclude references from metadata
            IEnumerable<Location> sourceLocations = from location in locations
                                                    where location.Kind != LocationKind.MetadataFile && location.Kind != LocationKind.None
                                                    select location;

            // Strip out duplicate locations
            IEnumerable<Location> uniqueLocations = sourceLocations.Distinct(LocationComparer.Instance);

            return uniqueLocations;
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (solution == null || documentId == null || syntaxNode == null)
            {
                return null;
            }

            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            Tuple<SyntaxNode, IEnumerable<Tuple<ReferencedSymbol, bool>>> referencingSymbols =
                await FindReferencesAsync(solution, document, syntaxNode, semanticModel, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<Location> locations = ExtractLocations(referencingSymbols, cancellationToken);
            IEnumerable<Location> filteredLocations = FilterLocations(locations);
            return
                filteredLocations.Select(
                    location =>
                        new ReferenceLocationDescriptor(solution, location, DisplayInfoProvider.GetDisplayInfoOfEnclosingSymbol(document, semanticModel,
                            location.SourceSpan.Start)));
        }
    }
}
