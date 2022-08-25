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
        private class SingleSymbolRenameSession : Session
        {
            private readonly string _originalText;
            private readonly string _replacementText;

            public static async Task<SingleSymbolRenameSession> CreateAsync(
                SymbolicRenameLocations renameLocationSet,
                Location renameSymbolDeclarationLocation,
                string replacementText,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                var originalText = renameLocationSet.Symbol.Name;
                var renameLocations = renameLocationSet.Locations;
                var symbol = renameLocationSet.Symbol;

                // only process documents which possibly contain the identifiers.
                var documentIdOfRenameSymbolDeclaration = renameLocationSet.Solution.GetRequiredDocument(renameSymbolDeclarationLocation.SourceTree!).Id;

                using var _ = PooledDictionary<DocumentId, DocumentRenameInfo>.GetInstance(out var documentIdToRenameInfoBuilder);
                var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync(
                    renameLocationSet,
                    replacementText,
                    originalText,
                    cancellationToken).ConfigureAwait(false);

                var replacementTextValid = IsIdentifierValid_Worker(
                    renameLocationSet.Solution,
                    replacementText,
                    documentsIdsToBeCheckedForConflict.Select(documentId => documentId.ProjectId).Distinct());

                var documentIdToRenameLocations = renameLocations
                    .GroupBy(location => location.DocumentId)
                    .ToDictionary(grouping => grouping.Key);

                foreach (var documentId in documentsIdsToBeCheckedForConflict)
                {
                    using var documentRenameInfoBuilder = new DocumentRenameInfo.Builder();

                    if (documentIdToRenameLocations.TryGetValue(documentId, out var renameLocationsInDocument))
                    {
                        var locationRenameContexts = renameLocationsInDocument
                            .Where(location => RenameUtilities.ShouldIncludeLocation(renameLocations, location))
                            .SelectAsArray(location => new LocationRenameContext(location, replacementTextValid, replacementText, originalText));
                        foreach (var locationRenameContext in locationRenameContexts)
                        {
                            var overlap = documentRenameInfoBuilder.AddLocationRenameContext(locationRenameContext);
                            // Here only one symbol is renamed, so each rename text span should never overlap.
                            RoslynDebug.Assert(!overlap);
                        }

                        // All textSpan in the document documentId, that requires rename in String or Comment
                        var stringAndCommentContexts = renameLocationsInDocument
                            .Where(l => l.IsRenameInStringOrComment)
                            .SelectAsArray(location => new StringAndCommentRenameContext(location, replacementText));
                        foreach (var stringAndCommentContext in stringAndCommentContexts)
                        {
                            var overlap = documentRenameInfoBuilder.AddStringAndCommentRenameContext(stringAndCommentContext);
                            // Here only one symbol is renamed, so each sub text span in string/comment should never overlap.
                            RoslynDebug.Assert(!overlap);
                        }
                    }

                    // Conflict checking documents is a superset of the rename locations. In case this document is not a documents of rename locations,
                    // just passing an empty rename information to check for conflicts.
                    documentRenameInfoBuilder.AddRenamedSymbol(symbol, replacementText, replacementTextValid, possibleNameConflicts);
                    documentIdToRenameInfoBuilder[documentId] = documentRenameInfoBuilder.ToRenameInfo();
                }

                return new SingleSymbolRenameSession(
                    renameLocationSet,
                    renameSymbolDeclarationLocation,
                    documentIdOfRenameSymbolDeclaration,
                    originalText,
                    replacementText,
                    replacementTextValid,
                    nonConflictSymbolKeys,
                    documentIdToRenameInfoBuilder.ToImmutableDictionary(),
                    fallbackOptions,
                    cancellationToken);
            }

            private SingleSymbolRenameSession(
                SymbolicRenameLocations renameLocationSet,
                Location renameSymbolDeclarationLocation,
                DocumentId documentIdOfRenameSymbolDeclaration,
                string originalText,
                string replacementText,
                bool replacementTextValid,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                ImmutableDictionary<DocumentId, DocumentRenameInfo> documentIdToDocumentRenameInfo,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken) : base(
                    solution: renameLocationSet.Solution,
                    ImmutableArray.Create(renameLocationSet),
                    nonConflictSymbolKeys,
                    documentIdToRenameInfo: documentIdToDocumentRenameInfo,
                    symbolToReplacementText: ImmutableDictionary<ISymbol, string>.Empty.Add(renameLocationSet.Symbol, replacementText),
                    symbolToReplacementTextValid: ImmutableDictionary<ISymbol, bool>.Empty.Add(renameLocationSet.Symbol, replacementTextValid),
                    symbolToDeclarationDocumentAndLocation: ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)>.Empty
                        .Add(renameLocationSet.Symbol, (documentIdOfRenameSymbolDeclaration, renameSymbolDeclarationLocation)),
                    overlapRenameLocations: ImmutableHashSet<RelatedLocation>.Empty,
                    fallBackOptions: fallbackOptions,
                    cancellationToken)
            {
                _originalText = originalText;
                _replacementText = replacementText;
            }

            protected override bool HasConflictForMetadataReference(
                RenameActionAnnotation renameActionAnnotation, RenameDeclarationLocationReference renameDeclarationLocationReference, ISymbol newReferencedSymbol)
            {
                var newMetadataName = newReferencedSymbol.ToDisplayString(s_metadataSymbolDisplayFormat);
                var oldMetadataName = renameDeclarationLocationReference.Name;
                // This is based on the assumption we only rename one symbol in the solution.
                return !HeuristicMetadataNameEquivalenceCheck(oldMetadataName, newMetadataName, _originalText, _replacementText);
            }
        }
    }
}
