// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
            private static async Task InitializeSymbolRenameInfoAsync(
                Solution solution,
                ImmutableArray<(SymbolicRenameLocations symbolicRenameLocations, string replacementText)> renameInfo,
                PooledDictionary<ISymbol, ImmutableHashSet<DocumentId>> symbolToDocumentIdNeedsConflictCheck,
                PooledDictionary<ISymbol, string> symbolToReplacementTextMap,
                PooledDictionary<ISymbol, bool> symbolToReplacementTextValidMap,
                PooledDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationInfo,
                PooledDictionary<ISymbol, ImmutableArray<string>> symbolToPossibleNameConflicts,
                CancellationToken cancellationToken)
            {
                foreach (var (symbolicRenameLocations, replacementText) in renameInfo)
                {
                    var symbol = symbolicRenameLocations.Symbol;
                    var originalText = symbolicRenameLocations.Symbol.Name;
                    // Find all the documents need to be visited for this symbol, and naming conflicts
                    var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync(
                        symbolicRenameLocations, replacementText, originalText, cancellationToken).ConfigureAwait(false);
                    symbolToDocumentIdNeedsConflictCheck[symbol] = documentsIdsToBeCheckedForConflict;
                    symbolToPossibleNameConflicts[symbol] = possibleNameConflicts;
                    var replacementTextValid = IsIdentifierValid_Worker(solution, replacementText, documentsIdsToBeCheckedForConflict.Select(docId => docId.ProjectId));
                    symbolToReplacementTextValidMap[symbol] = replacementTextValid;
                    symbolToReplacementTextMap[symbol] = replacementText;
                    var declarationLocation = symbol.Locations.First(l => l.IsInSource);
                    var declarationDocumentId = solution.GetRequiredDocument(declarationLocation.SourceTree).Id;
                    symbolToDeclarationInfo[symbol] = (declarationDocumentId, declarationLocation);
                }
            }

            public static async Task<MultipleSymbolsRenameSessions> CreateAsync(
                Solution solution,
                ImmutableArray<(SymbolicRenameLocations symbolicRenameLocations, string replacementText)> symbolicRenameInfo,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                using var _1 = PooledHashSet<RenameLocation>.GetInstance(out var overlapRenameLocations);

                using var _2 = PooledDictionary<ISymbol, bool>.GetInstance(out var symbolToReplacementTextValidMap);
                using var _3 = PooledDictionary<ISymbol, string>.GetInstance(out var symbolToReplacementTextMap);
                using var _4 = PooledDictionary<ISymbol, (DocumentId declarationDocumentId, Location symbolDeclarationLocation)>.GetInstance(out var symbolToDeclarationInfo);
                using var _5 = PooledDictionary<ISymbol, ImmutableHashSet<DocumentId>>.GetInstance(out var symbolToDocumentIdsNeedsConflict);
                using var _6 = PooledDictionary<ISymbol, ImmutableArray<string>>.GetInstance(out var symbolToPossibleNameConflicts);
                await InitializeSymbolRenameInfoAsync(
                    solution,
                    symbolicRenameInfo,
                    symbolToDocumentIdsNeedsConflict,
                    symbolToReplacementTextMap,
                    symbolToReplacementTextValidMap,
                    symbolToDeclarationInfo,
                    symbolToPossibleNameConflicts,
                    cancellationToken).ConfigureAwait(false);

                var documentIdToRenameInfoBuilder = PooledDictionary<DocumentId, DocumentRenameInfo.Builder>.GetInstance();
                var symbolsToRenameInfo = symbolicRenameInfo.ToDictionary(
                    pair => pair.symbolicRenameLocations.Symbol,
                    pair => pair);

                try
                {
                    // foreach (var (symbolicRenameLocations, replacementText) in symbolicRenameInfo)
                    // {
                    //     var symbol = symbolicRenameLocations.Symbol;
                    //     var originalText = symbolicRenameLocations.Symbol.Name;
                    //     var renameLocations = symbolicRenameLocations.Locations;
                    //     // Find all the documents need to be visited for this symbol, and naming conflicts
                    //     var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync(
                    //         symbolicRenameLocations, replacementText, originalText, cancellationToken).ConfigureAwait(false);
                    //     var replacementTextValid = IsIdentifierValid_Worker(solution, replacementText, documentsIdsToBeCheckedForConflict.Select(docId => docId.ProjectId));
                    //     symbolToReplacementTextValidMapBuilder[symbol] = replacementTextValid;
                    //     symbolToReplacementTextBuilder[symbol] = replacementText;
                    //     var declarationLocation = symbol.Locations.First(l => l.IsInSource);
                    //     var declarationDocumentId = solution.GetRequiredDocument(declarationLocation.SourceTree).Id;
                    //     symbolToDeclarationInfoBuilder[symbol] = (declarationDocumentId, declarationLocation);
                    //
                    //     var documentIdToRenameLocations = renameLocations
                    //         .GroupBy(location => location.DocumentId)
                    //         .ToDictionary(grouping => grouping.Key);
                    //
                    //     // We need to build the rename information for each document need conflict check.
                    //     foreach (var documentId in documentsIdsToBeCheckedForConflict)
                    //     {
                    //         if (!documentIdToRenameInfoBuilder.TryGetValue(documentId, out var documentRenameInfoBuilder))
                    //         {
                    //             documentRenameInfoBuilder = new DocumentRenameInfo.Builder();
                    //             documentIdToRenameInfoBuilder[documentId] = documentRenameInfoBuilder;
                    //         }
                    //
                    //         // Conflict checking documents is a superset of the rename locations. In case this document is not a documents of rename locations,
                    //         // just passing an empty rename information to check for conflicts.
                    //         var renameLocationsInDocument = documentIdToRenameLocations.ContainsKey(documentId)
                    //             ? documentIdToRenameLocations[documentId].ToImmutableArray()
                    //             : ImmutableArray<RenameLocation>.Empty;
                    //
                    //         var locationRenameContexts = renameLocationsInDocument
                    //             .WhereAsArray(location => RenameUtilities.ShouldIncludeLocation(renameLocations, location))
                    //             .SelectAsArray(location => new LocationRenameContext(location, replacementTextValid, replacementText, originalText));
                    //
                    //         foreach (var locationRenameContext in locationRenameContexts)
                    //         {
                    //             if (documentRenameInfoBuilder.AddLocationRenameContext(locationRenameContext))
                    //             {
                    //                 overlapRenameLocations.Add(locationRenameContext.RenameLocation);
                    //             }
                    //         }
                    //
                    //         // All textSpan in the document documentId, that requires rename in String or Comment
                    //         var stringAndCommentContexts = renameLocationsInDocument
                    //             .WhereAsArray(l => l.IsRenameInStringOrComment)
                    //             .SelectAsArray(location => new StringAndCommentRenameContext(location, replacementText));
                    //         foreach (var stringAndCommentContext in stringAndCommentContexts)
                    //         {
                    //             if (documentRenameInfoBuilder.AddStringAndCommentRenameContext(stringAndCommentContext))
                    //             {
                    //                 overlapRenameLocations.Add(stringAndCommentContext.RenameLocation);
                    //             }
                    //         }
                    //
                    //         var allPossibleSymbolsInComplexName = ComplexNameSymbolVisitor.GetAllSymbolsInFullyQualifiedName(symbol);
                    //         foreach (var possibleSymbol in allPossibleSymbolsInComplexName)
                    //         {
                    //             if (symbolsToRenameInfo.TryGetValue(possibleSymbol, out var (possibleSymbolRenameLocations, replacementTextFor)))
                    //
                    //         }
                    //
                    //         // TODO: Call symbol visitor
                    //         documentRenameInfoBuilder.AddRenamedSymbol(symbol, replacementText, replacementTextValid, possibleNameConflicts);
                    //     }
                    }

                    return new MultipleSymbolsRenameSessions(
                        solution,
                        symbolicRenameInfo.SelectAsArray(pair => pair.symbolicRenameLocations),
                        nonConflictSymbolKeys,
                        ToImmutable(documentIdToRenameInfoBuilder),
                        symbolToReplacementTextBuilder.ToImmutableDictionary(),
                        symbolToReplacementTextValidMapBuilder.ToImmutableDictionary(),
                        symbolToDeclarationInfoBuilder.ToImmutableDictionary(),
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
                ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationDocumentAndLocation, CodeCleanupOptionsProvider fallBackOptions, CancellationToken cancellationToken)
                 : base(solution, symbolicRenameLocations, nonConflictSymbolKeys, documentIdToRenameInfo, symbolToReplacementText, symbolToReplacementTextValid, symbolToDeclarationDocumentAndLocation, fallBackOptions, cancellationToken)
            {
            }

            protected override bool HasConflictForMetadataReference(
                RenameDeclarationLocationReference renameDeclarationLocationReference,
                ISymbol newReferencedSymbol)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}

