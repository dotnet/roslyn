// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal interface IRemoteRenamerService
    {
        /// <summary>
        /// Runs the entire rename operation OOP and returns the final result. More efficient (due to less back and
        /// forth marshaling) when the intermediary results of rename are not needed. To get the individual parts of
        /// rename remoted use <see cref="FindRenameLocationsAsync"/> and <see cref="ResolveConflictsAsync"/>.
        /// </summary>
        ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string replacementText,
            SymbolRenameOptions options,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken);

        ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SymbolRenameOptions options,
            CancellationToken cancellationToken);

        ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken);
    }

    [DataContract]
    internal class SerializableSearchResult
    {
        // We use arrays so we can represent default immutable arrays.

        [DataMember(Order = 0)]
        public SerializableRenameLocation[]? Locations;

        [DataMember(Order = 1)]
        public SerializableReferenceLocation[]? ImplicitLocations;

        [DataMember(Order = 2)]
        public SerializableSymbolAndProjectId[]? ReferencedSymbols;

        [return: NotNullIfNotNull("result")]
        public static SerializableSearchResult? Dehydrate(Solution solution, RenameLocations.SearchResult? result, CancellationToken cancellationToken)
            => result == null ? null : new SerializableSearchResult
            {
                Locations = result.Locations.Select(loc => SerializableRenameLocation.Dehydrate(loc)).ToArray(),
                ImplicitLocations = result.ImplicitLocations.IsDefault ? null : result.ImplicitLocations.Select(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)).ToArray(),
                ReferencedSymbols = result.ReferencedSymbols.IsDefault ? null : result.ReferencedSymbols.Select(s => SerializableSymbolAndProjectId.Dehydrate(solution, s, cancellationToken)).ToArray(),
            };

        public async Task<RenameLocations.SearchResult> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            ImmutableArray<ReferenceLocation> implicitLocations = default;
            ImmutableArray<ISymbol> referencedSymbols = default;

            Contract.ThrowIfNull(Locations);

            using var _1 = ArrayBuilder<RenameLocation>.GetInstance(Locations.Length, out var locBuilder);
            foreach (var loc in Locations)
                locBuilder.Add(await loc.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

            var locations = locBuilder.ToImmutableHashSet();

            if (ImplicitLocations != null)
            {
                using var _2 = ArrayBuilder<ReferenceLocation>.GetInstance(ImplicitLocations.Length, out var builder);
                foreach (var loc in ImplicitLocations)
                    builder.Add(await loc.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

                implicitLocations = builder.ToImmutable();
            }

            if (ReferencedSymbols != null)
            {
                using var _3 = ArrayBuilder<ISymbol>.GetInstance(ReferencedSymbols.Length, out var builder);
                foreach (var symbol in ReferencedSymbols)
                    builder.AddIfNotNull(await symbol.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

                referencedSymbols = builder.ToImmutable();
            }

            return new RenameLocations.SearchResult(locations, implicitLocations, referencedSymbols);
        }
    }

    [DataContract]
    internal readonly struct SerializableRenameLocation
    {
        [DataMember(Order = 0)]
        public readonly TextSpan Location;

        [DataMember(Order = 1)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 2)]
        public readonly CandidateReason CandidateReason;

        [DataMember(Order = 3)]
        public readonly bool IsRenamableAliasUsage;

        [DataMember(Order = 4)]
        public readonly bool IsRenamableAccessor;

        [DataMember(Order = 5)]
        public readonly TextSpan ContainingLocationForStringOrComment;

        [DataMember(Order = 6)]
        public readonly bool IsWrittenTo;

        public SerializableRenameLocation(
            TextSpan location,
            DocumentId documentId,
            CandidateReason candidateReason,
            bool isRenamableAliasUsage,
            bool isRenamableAccessor,
            TextSpan containingLocationForStringOrComment,
            bool isWrittenTo)
        {
            Location = location;
            DocumentId = documentId;
            CandidateReason = candidateReason;
            IsRenamableAliasUsage = isRenamableAliasUsage;
            IsRenamableAccessor = isRenamableAccessor;
            ContainingLocationForStringOrComment = containingLocationForStringOrComment;
            IsWrittenTo = isWrittenTo;
        }

        public static SerializableRenameLocation Dehydrate(RenameLocation location)
            => new(location.Location.SourceSpan,
                   location.DocumentId,
                   location.CandidateReason,
                   location.IsRenamableAliasUsage,
                   location.IsRenamableAccessor,
                   location.ContainingLocationForStringOrComment,
                   location.IsWrittenTo);

        public async Task<RenameLocation> RehydrateAsync(Solution solution, CancellationToken cancellation)
        {
            var document = solution.GetRequiredDocument(DocumentId);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellation).ConfigureAwait(false);

            return new RenameLocation(
                CodeAnalysis.Location.Create(tree, Location),
                DocumentId,
                CandidateReason,
                IsRenamableAliasUsage,
                IsRenamableAccessor,
                IsWrittenTo,
                ContainingLocationForStringOrComment);
        }
    }

    internal partial class RenameLocations
    {
        public SerializableRenameLocations Dehydrate(Solution solution, CancellationToken cancellationToken)
            => new(
                SerializableSymbolAndProjectId.Dehydrate(solution, Symbol, cancellationToken),
                Options,
                SerializableSearchResult.Dehydrate(solution, _result, cancellationToken));

        internal static async Task<RenameLocations?> TryRehydrateAsync(Solution solution, SerializableRenameLocations locations, CancellationToken cancellationToken)
        {
            if (locations == null)
                return null;

            if (locations.Symbol == null)
                return null;

            var symbol = await locations.Symbol.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            if (symbol == null)
                return null;

            Contract.ThrowIfNull(locations.Result);

            return new RenameLocations(
                symbol,
                solution,
                locations.Options,
                await locations.Result.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));
        }
    }

    [DataContract]
    internal sealed class SerializableRenameLocations
    {
        [DataMember(Order = 0)]
        public readonly SerializableSymbolAndProjectId? Symbol;

        [DataMember(Order = 1)]
        public readonly SymbolRenameOptions Options;

        [DataMember(Order = 2)]
        public readonly SerializableSearchResult? Result;

        public SerializableRenameLocations(SerializableSymbolAndProjectId? symbol, SymbolRenameOptions options, SerializableSearchResult? result)
        {
            Symbol = symbol;
            Options = options;
            Result = result;
        }
    }

    [DataContract]
    internal sealed class SerializableConflictResolution
    {
        [DataMember(Order = 0)]
        public readonly string? ErrorMessage;

        [DataMember(Order = 1)]
        public readonly SuccessfulConflictResolution? Resolution;

        public SerializableConflictResolution(string? errorMessage, SuccessfulConflictResolution? resolution)
        {
            ErrorMessage = errorMessage;
            Resolution = resolution;
        }

        public async Task<ConflictResolution> RehydrateAsync(Solution oldSolution, CancellationToken cancellationToken)
        {
            if (ErrorMessage != null)
                return new ConflictResolution(ErrorMessage);

            Contract.ThrowIfNull(Resolution);

            var newSolutionWithoutRenamedDocument = await RemoteUtilities.UpdateSolutionAsync(
                oldSolution, Resolution.DocumentTextChanges, cancellationToken).ConfigureAwait(false);

            return new ConflictResolution(
                oldSolution,
                newSolutionWithoutRenamedDocument,
                Resolution.ReplacementTextValid,
                Resolution.RenamedDocument,
                Resolution.DocumentIds,
                Resolution.RelatedLocations,
                Resolution.DocumentToModifiedSpansMap,
                Resolution.DocumentToComplexifiedSpansMap,
                Resolution.DocumentToRelatedLocationsMap);
        }
    }

    [DataContract]
    internal sealed class SuccessfulConflictResolution
    {
        [DataMember(Order = 0)]
        public readonly bool ReplacementTextValid;

        [DataMember(Order = 1)]
        public readonly (DocumentId documentId, string newName) RenamedDocument;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<DocumentId> DocumentIds;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<RelatedLocation> RelatedLocations;

        [DataMember(Order = 4)]
        public readonly ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> DocumentTextChanges;

        [DataMember(Order = 5)]
        public readonly ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> DocumentToModifiedSpansMap;

        [DataMember(Order = 6)]
        public readonly ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> DocumentToComplexifiedSpansMap;

        [DataMember(Order = 7)]
        public readonly ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> DocumentToRelatedLocationsMap;

        public SuccessfulConflictResolution(
            bool replacementTextValid,
            (DocumentId documentId, string newName) renamedDocument,
            ImmutableArray<DocumentId> documentIds,
            ImmutableArray<RelatedLocation> relatedLocations,
            ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> documentTextChanges,
            ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> documentToModifiedSpansMap,
            ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> documentToComplexifiedSpansMap,
            ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> documentToRelatedLocationsMap)
        {
            ReplacementTextValid = replacementTextValid;
            RenamedDocument = renamedDocument;
            DocumentIds = documentIds;
            RelatedLocations = relatedLocations;
            DocumentTextChanges = documentTextChanges;
            DocumentToModifiedSpansMap = documentToModifiedSpansMap;
            DocumentToComplexifiedSpansMap = documentToComplexifiedSpansMap;
            DocumentToRelatedLocationsMap = documentToRelatedLocationsMap;
        }
    }

    internal partial struct ConflictResolution
    {
        public async Task<SerializableConflictResolution> DehydrateAsync(CancellationToken cancellationToken)
        {
            if (ErrorMessage != null)
                return new SerializableConflictResolution(ErrorMessage, resolution: null);

            var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(OldSolution, _newSolutionWithoutRenamedDocument, cancellationToken).ConfigureAwait(false);
            return new SerializableConflictResolution(
                errorMessage: null,
                new SuccessfulConflictResolution(
                    ReplacementTextValid,
                    _renamedDocument,
                    DocumentIds,
                    RelatedLocations,
                    documentTextChanges,
                    _documentToModifiedSpansMap,
                    _documentToComplexifiedSpansMap,
                    _documentToRelatedLocationsMap));
        }
    }
}
