// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    internal interface IRemoteRenamer
    {
        /// <summary>
        /// Runs the entire rename operation OOP and returns the final result. More efficient (due to less back and
        /// forth marshaling) when the intermediary results of rename are not needed. To get the individual parts of
        /// rename remoted use <see cref="FindRenameLocationsAsync"/> and <see cref="ResolveConflictsAsync"/>.
        /// </summary>
        Task<SerializableConflictResolution?> RenameSymbolAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string replacementText,
            SerializableRenameOptionSet options,
            SerializableSymbolAndProjectId[] nonConflictSymbolIds,
            CancellationToken cancellationToken);

        Task<SerializableRenameLocations?> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameOptionSet options,
            CancellationToken cancellationToken);

        Task<SerializableConflictResolution?> ResolveConflictsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            SerializableSymbolAndProjectId[] nonConflictSymbolIds,
            CancellationToken cancellationToken);
    }

    internal struct SerializableRenameOptionSet
    {
        public bool RenameOverloads;
        public bool RenameInStrings;
        public bool RenameInComments;
        public bool RenameFile;

        public static SerializableRenameOptionSet Dehydrate(RenameOptionSet optionSet)
            => new SerializableRenameOptionSet
            {
                RenameOverloads = optionSet.RenameOverloads,
                RenameInStrings = optionSet.RenameInStrings,
                RenameInComments = optionSet.RenameInComments,
                RenameFile = optionSet.RenameFile,
            };

        public RenameOptionSet Rehydrate()
            => new RenameOptionSet(RenameOverloads, RenameInStrings, RenameInComments, RenameFile);
    }

    internal class SerializableSearchResult
    {
        // We use arrays so we can represent default immutable arrays.

        public SerializableRenameLocation[]? Locations;
        public SerializableReferenceLocation[]? ImplicitLocations;
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

    internal struct SerializableRenameLocation
    {
        public TextSpan Location;
        public DocumentId DocumentId;
        public CandidateReason CandidateReason;
        public bool IsRenamableAliasUsage;
        public bool IsRenamableAccessor;
        public TextSpan ContainingLocationForStringOrComment;
        public bool IsWrittenTo;

        public static SerializableRenameLocation Dehydrate(RenameLocation location)
            => new SerializableRenameLocation
            {
                Location = location.Location.SourceSpan,
                DocumentId = location.DocumentId,
                CandidateReason = location.CandidateReason,
                IsRenamableAliasUsage = location.IsRenamableAliasUsage,
                IsRenamableAccessor = location.IsRenamableAccessor,
                ContainingLocationForStringOrComment = location.ContainingLocationForStringOrComment,
                IsWrittenTo = location.IsWrittenTo,
            };

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
            => new SerializableRenameLocations
            {
                Symbol = SerializableSymbolAndProjectId.Dehydrate(solution, Symbol, cancellationToken),
                Options = SerializableRenameOptionSet.Dehydrate(Options),
                Result = SerializableSearchResult.Dehydrate(solution, _result, cancellationToken),
            };

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
                locations.Options.Rehydrate(),
                await locations.Result.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));
        }
    }

    internal class SerializableRenameLocations
    {
        public SerializableSymbolAndProjectId? Symbol;
        public SerializableRenameOptionSet Options;
        public SerializableSearchResult? Result;
    }

    internal class SerializableComplexifiedSpan
    {
        public TextSpan OriginalSpan;
        public TextSpan NewSpan;
        public ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> ModifiedSubSpans;

        public ComplexifiedSpan Rehydrate()
            => new ComplexifiedSpan(OriginalSpan, NewSpan, ModifiedSubSpans);

        public static SerializableComplexifiedSpan Dehydrate(ComplexifiedSpan span)
            => new SerializableComplexifiedSpan
            {
                OriginalSpan = span.OriginalSpan,
                NewSpan = span.NewSpan,
                ModifiedSubSpans = span.ModifiedSubSpans,
            };
    }

    internal class SerializableRelatedLocation
    {
        public TextSpan ConflictCheckSpan;
        public RelatedLocationType Type;
        public bool IsReference;
        public DocumentId? DocumentId;
        public TextSpan ComplexifiedTargetSpan;

        public RelatedLocation Rehydrate()
            => new RelatedLocation(ConflictCheckSpan, DocumentId, Type, IsReference, ComplexifiedTargetSpan);

        public static SerializableRelatedLocation Dehydrate(RelatedLocation location)
            => new SerializableRelatedLocation
            {
                ConflictCheckSpan = location.ConflictCheckSpan,
                Type = location.Type,
                IsReference = location.IsReference,
                DocumentId = location.DocumentId,
                ComplexifiedTargetSpan = location.ComplexifiedTargetSpan,
            };
    }

    internal class SerializableConflictResolution
    {
        public string? ErrorMessage;

        public bool ReplacementTextValid;

        public (DocumentId documentId, string newName) RenamedDocument;

        // Note: arrays are used (instead of ImmutableArray) as jsonrpc can't marshal null values to/from those types.
        //
        // We also flatten dictionaries into key/value tuples because jsonrpc only supports dictionaries with string keys.

        public DocumentId[]? DocumentIds;
        public SerializableRelatedLocation[]? RelatedLocations;
        public (DocumentId, TextChange[])[]? DocumentTextChanges;
        public (DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>)[]? DocumentToModifiedSpansMap;
        public (DocumentId, ImmutableArray<SerializableComplexifiedSpan>)[]? DocumentToComplexifiedSpansMap;
        public (DocumentId, ImmutableArray<SerializableRelatedLocation>)[]? DocumentToRelatedLocationsMap;

        public async Task<ConflictResolution> RehydrateAsync(Solution oldSolution, CancellationToken cancellationToken)
        {
            if (ErrorMessage != null)
                return new ConflictResolution(ErrorMessage);

            var newSolutionWithoutRenamedDocument = await RemoteUtilities.UpdateSolutionAsync(
                oldSolution, DocumentTextChanges, cancellationToken).ConfigureAwait(false);
            return new ConflictResolution(
                oldSolution,
                newSolutionWithoutRenamedDocument,
                ReplacementTextValid,
                RenamedDocument,
                DocumentIds.ToImmutableArrayOrEmpty(),
                RelatedLocations.SelectAsArray(loc => loc.Rehydrate()),
                DocumentToModifiedSpansMap.ToImmutableDictionaryOrEmpty(t => t.Item1, t => t.Item2),
                DocumentToComplexifiedSpansMap.ToImmutableDictionaryOrEmpty(t => t.Item1, t => t.Item2.SelectAsArray(c => c.Rehydrate())),
                DocumentToRelatedLocationsMap.ToImmutableDictionaryOrEmpty(t => t.Item1, t => t.Item2.SelectAsArray(c => c.Rehydrate())));
        }
    }

    internal partial struct ConflictResolution
    {
        public async Task<SerializableConflictResolution> DehydrateAsync(CancellationToken cancellationToken)
        {
            if (ErrorMessage != null)
                return new SerializableConflictResolution { ErrorMessage = ErrorMessage };

            var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(OldSolution, _newSolutionWithoutRenamedDocument, cancellationToken).ConfigureAwait(false);
            return new SerializableConflictResolution
            {
                ReplacementTextValid = ReplacementTextValid,
                RenamedDocument = _renamedDocument,
                DocumentIds = DocumentIds.ToArray(),
                RelatedLocations = RelatedLocations.Select(loc => SerializableRelatedLocation.Dehydrate(loc)).ToArray(),
                DocumentTextChanges = documentTextChanges,
                DocumentToModifiedSpansMap = _documentToModifiedSpansMap.Select(kvp => (kvp.Key, kvp.Value)).ToArray(),
                DocumentToComplexifiedSpansMap = _documentToComplexifiedSpansMap.Select(kvp => (kvp.Key, kvp.Value.SelectAsArray(s => SerializableComplexifiedSpan.Dehydrate(s)))).ToArray(),
                DocumentToRelatedLocationsMap = _documentToRelatedLocationsMap.Select(kvp => (kvp.Key, kvp.Value.SelectAsArray(s => SerializableRelatedLocation.Dehydrate(s)))).ToArray(),
            };
        }
    }
}
