// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static partial class ConflictResolver
    {
        private sealed class MultipleSymbolsRenameSessions : Session
        {
            private static async Task InitializeRenamingMapsAsync(
                Solution solution,
                ImmutableDictionary<ISymbol, (SymbolicRenameLocations symbolicRenameLocations, string replacementText)> symbolToRenameInfo,
                PooledDictionary<ISymbol, ImmutableHashSet<DocumentId>> symbolToDocumentIdNeedsConflictCheck,
                PooledDictionary<ISymbol, string> symbolToReplacementTextMap,
                PooledDictionary<ISymbol, bool> symbolToReplacementTextValidMap,
                PooledDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationInfo,
                PooledDictionary<ISymbol, ImmutableArray<string>> symbolToPossibleNameConflicts,
                CancellationToken cancellationToken)
            {
                foreach (var (symbol, (symbolicRenameLocations, replacementText)) in symbolToRenameInfo)
                {
                    var originalText = symbolicRenameLocations.Symbol.Name;
                    // Find all the documents need to be visited for this symbol, and naming conflicts
                    var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync(
                        symbolicRenameLocations, replacementText, originalText, cancellationToken).ConfigureAwait(false);
                    var replacementTextValid = IsIdentifierValid_Worker(solution, replacementText, documentsIdsToBeCheckedForConflict.Select(docId => docId.ProjectId));
                    // Checks have been done before conflict resolution to make sure every renaming symbol has at least one in source location.
                    var declarationLocation = symbol.Locations.First(l => l.IsInSource);
                    var declarationDocumentId = solution.GetRequiredDocument(declarationLocation.SourceTree!).Id;

                    symbolToDeclarationInfo[symbol] = (declarationDocumentId, declarationLocation);
                    symbolToDocumentIdNeedsConflictCheck[symbol] = documentsIdsToBeCheckedForConflict;
                    symbolToPossibleNameConflicts[symbol] = possibleNameConflicts;
                    symbolToReplacementTextValidMap[symbol] = replacementTextValid;
                    symbolToReplacementTextMap[symbol] = replacementText;
                }
            }

            public static async Task<MultipleSymbolsRenameSessions> CreateAsync(
                Solution solution,
                ImmutableDictionary<ISymbol, (SymbolicRenameLocations symbolicRenameLocations, string replacementText)> symbolToSymbolRenameInfo,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                // Renaming symbol -> whether the replacement text is valid
                using var _1 = PooledDictionary<ISymbol, bool>.GetInstance(out var symbolToReplacementTextValidMap);
                // Renaming symbol -> replacement text
                using var _2 = PooledDictionary<ISymbol, string>.GetInstance(out var symbolToReplacementTextMap);
                // Renaming symbol -> one of the declaration location and the document contains the location
                using var _3 = PooledDictionary<ISymbol, (DocumentId declarationDocumentId, Location symbolDeclarationLocation)>.GetInstance(out var symbolToDeclarationInfo);
                // Renaming symbol -> all the documentIds need conflict check
                using var _4 = PooledDictionary<ISymbol, ImmutableHashSet<DocumentId>>.GetInstance(out var symbolToDocumentIdsNeedsConflict);
                // Renaming symbol -> the possible naming conflict strings
                using var _5 = PooledDictionary<ISymbol, ImmutableArray<string>>.GetInstance(out var symbolToPossibleNameConflicts);

                await InitializeRenamingMapsAsync(
                    solution,
                    symbolToSymbolRenameInfo,
                    symbolToDocumentIdsNeedsConflict,
                    symbolToReplacementTextMap,
                    symbolToReplacementTextValidMap,
                    symbolToDeclarationInfo,
                    symbolToPossibleNameConflicts,
                    cancellationToken).ConfigureAwait(false);

                using var _6 = PooledHashSet<RelatedLocation>.GetInstance(out var overlapRenameLocations);
                var documentIdToRenameInfoBuilder = PooledDictionary<DocumentId, DocumentRenameInfo.Builder>.GetInstance();
                try
                {
                    foreach (var (symbol, (symbolicRenameLocations, replacementText)) in symbolToSymbolRenameInfo)
                    {
                        var originalText = symbol.Name;
                        var documentIdsNeedConflictCheck = symbolToDocumentIdsNeedsConflict[symbol];
                        var renameLocations = symbolicRenameLocations.Locations;
                        var replacementTextValid = symbolToReplacementTextValidMap[symbol];
                        var documentIdToRenameLocations = renameLocations
                            .GroupBy(location => location.DocumentId)
                            .ToDictionary(grouping => grouping.Key);

                        foreach (var documentId in documentIdsNeedConflictCheck)
                        {
                            if (!documentIdToRenameInfoBuilder.TryGetValue(documentId, out var documentRenameInfoBuilder))
                            {
                                documentRenameInfoBuilder = new DocumentRenameInfo.Builder();
                                documentIdToRenameInfoBuilder[documentId] = documentRenameInfoBuilder;
                            }

                            // Conflict checking documents is a superset of the rename locations. In case this document is not a documents of rename locations,
                            // just passing an empty rename information to check for conflicts.
                            var renameLocationsInDocument = documentIdToRenameLocations.ContainsKey(documentId)
                                ? documentIdToRenameLocations[documentId].ToImmutableArray()
                                : ImmutableArray<RenameLocation>.Empty;

                            var locationRenameContexts = renameLocationsInDocument
                                .WhereAsArray(location => RenameUtilities.ShouldIncludeLocation(renameLocations, location))
                                .SelectAsArray(location => new LocationRenameContext(location, replacementTextValid, replacementText, originalText));

                            foreach (var locationRenameContext in locationRenameContexts)
                            {
                                if (documentRenameInfoBuilder.AddLocationRenameContext(locationRenameContext))
                                {
                                    overlapRenameLocations.Add(new RelatedLocation(
                                        locationRenameContext.RenameLocation.Location.SourceSpan,
                                        documentId,
                                        RelatedLocationType.OverlapRenameLocation,
                                        isReference: true));
                                }
                            }

                            // All textSpan in the document documentId, that requires rename in String or Comment
                            var stringAndCommentContexts = renameLocationsInDocument
                                .WhereAsArray(l => l.IsRenameInStringOrComment)
                                .SelectAsArray(location => new StringAndCommentRenameContext(location, replacementText));

                            foreach (var stringAndCommentContext in stringAndCommentContexts)
                            {
                                if (documentRenameInfoBuilder.AddStringAndCommentRenameContext(stringAndCommentContext))
                                {
                                    // This is a location in string/comments searched by regex, not a reference location.
                                    overlapRenameLocations.Add(
                                        new RelatedLocation(
                                           stringAndCommentContext.RenameLocation.Location.SourceSpan,
                                           documentId,
                                           RelatedLocationType.OverlapRenameLocation,
                                           isReference: false));
                                }
                            }

                            var allPossibleSymbolsInComplexName = ComplexNameSymbolVisitor.GetAllSymbolsInFullyQualifiedName(symbol);
                            foreach (var possibleSymbol in allPossibleSymbolsInComplexName)
                            {
                                if (symbolToSymbolRenameInfo.ContainsKey(possibleSymbol))
                                {
                                    documentRenameInfoBuilder.AddRenamedSymbol(
                                        possibleSymbol, symbolToReplacementTextMap[possibleSymbol], symbolToReplacementTextValidMap[possibleSymbol], symbolToPossibleNameConflicts[possibleSymbol]);
                                }
                            }
                        }
                    }

                    return new MultipleSymbolsRenameSessions(
                        solution,
                        symbolToSymbolRenameInfo.SelectAsArray(pair => pair.Value.symbolicRenameLocations),
                        nonConflictSymbolKeys,
                        ToImmutable(documentIdToRenameInfoBuilder),
                        symbolToReplacementTextMap.ToImmutableDictionary(),
                        symbolToReplacementTextValidMap.ToImmutableDictionary(),
                        symbolToDeclarationInfo.ToImmutableDictionary(),
                        overlapRenameLocations.ToImmutableHashSet(),
                        fallbackOptions,
                        cancellationToken);

                }
                finally
                {
                    foreach (var (_, builder) in documentIdToRenameInfoBuilder)
                    {
                        builder.Dispose();
                    }

                    documentIdToRenameInfoBuilder.Free();
                }

                static ImmutableDictionary<DocumentId, DocumentRenameInfo> ToImmutable(PooledDictionary<DocumentId, DocumentRenameInfo.Builder> builder)
                {
                    var dictionaryBuilder = ImmutableDictionary.CreateBuilder<DocumentId, DocumentRenameInfo>();
                    foreach (var (documentId, renameInfoBuilder) in builder)
                    {
                        dictionaryBuilder[documentId] = renameInfoBuilder.ToRenameInfo();
                    }

                    return dictionaryBuilder.ToImmutableDictionary();
                }
            }

            private MultipleSymbolsRenameSessions(
                Solution solution,
                ImmutableArray<SymbolicRenameLocations> symbolicRenameLocations,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                ImmutableDictionary<DocumentId, DocumentRenameInfo> documentIdToRenameInfo,
                ImmutableDictionary<ISymbol, string> symbolToReplacementText,
                ImmutableDictionary<ISymbol, bool> symbolToReplacementTextValid,
                ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationDocumentAndLocation,
                ImmutableHashSet<RelatedLocation> overlapRenameLocations,
                CodeCleanupOptionsProvider fallBackOptions,
                CancellationToken cancellationToken)
                 : base(solution,
                     symbolicRenameLocations,
                     nonConflictSymbolKeys,
                     documentIdToRenameInfo,
                     symbolToReplacementText,
                     symbolToReplacementTextValid,
                     symbolToDeclarationDocumentAndLocation,
                     overlapRenameLocations,
                     fallBackOptions,
                     cancellationToken)
            {
            }

            protected override bool HasConflictForMetadataReference(
                RenameActionAnnotation renameActionAnnotation,
                RenameDeclarationLocationReference renameDeclarationLocationReference,
                ISymbol newReferencedSymbol)
            {
                if (renameActionAnnotation.IsRenameLocation)
                {
                    var newMetadataName = newReferencedSymbol.ToDisplayString(s_metadataSymbolDisplayFormat);
                    var oldMetadataName = renameDeclarationLocationReference.Name;
                    return !HeuristicMetadataNameEquivalenceCheck(oldMetadataName, newMetadataName, renameActionAnnotation.OriginalText, renameActionAnnotation.ReplacementText);
                }
                else
                {
                    // We don't know the replacement & original text, always assume this has conflict to make sure the final result is correct.
                    return true;
                }
            }
        }
    }
}

