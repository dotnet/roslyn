﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
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
        // TODO https://github.com/microsoft/vs-streamjsonrpc/issues/789 
        internal interface ICallback // : IRemoteOptionsCallback<CodeCleanupOptions>
        {
            ValueTask<CodeCleanupOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Runs the entire rename operation OOP and returns the final result. More efficient (due to less back and
        /// forth marshaling) when the intermediary results of rename are not needed. To get the individual parts of
        /// rename remoted use <see cref="FindRenameLocationsAsync"/> and <see cref="ResolveConflictsAsync"/>.
        /// </summary>
        ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string replacementText,
            SymbolRenameOptions options,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken);

        ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SymbolRenameOptions options,
            CancellationToken cancellationToken);

        ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteRenamerService)), Shared]
    internal sealed class RemoteRenamerServiceCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteRenamerService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteRenamerServiceCallbackDispatcher()
        {
        }

        public ValueTask<CodeCleanupOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
            => ((RemoteOptionsProvider<CodeCleanupOptions>)GetCallback(callbackId)).GetOptionsAsync(language, cancellationToken);
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

    internal partial class LightweightRenameLocations
    {
        public SerializableRenameLocations Dehydrate()
            => new(
                Options,
                Locations.Select(SerializableRenameLocation.Dehydrate).ToArray(),
                _implicitLocations,
                _referencedSymbols);

        internal static async Task<LightweightRenameLocations?> TryRehydrateAsync(
            Solution solution, CodeCleanupOptionsProvider fallbackOptions, SerializableRenameLocations locations, CancellationToken cancellationToken)
        {
            if (locations == null)
                return null;

            Contract.ThrowIfNull(locations.Locations);

            using var _1 = ArrayBuilder<RenameLocation>.GetInstance(locations.Locations.Length, out var locBuilder);
            foreach (var loc in locations.Locations)
                locBuilder.Add(await loc.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

            return new LightweightRenameLocations(
                solution,
                locations.Options,
                fallbackOptions,
                locBuilder.ToImmutableHashSet(),
                locations.ImplicitLocations,
                locations.ReferencedSymbols);
        }
    }

    [DataContract]
    internal sealed class SerializableRenameLocations
    {
        [DataMember(Order = 0)]
        public readonly SymbolRenameOptions Options;

        // We use arrays so we can represent default immutable arrays.

        [DataMember(Order = 1)]
        public SerializableRenameLocation[] Locations;

        [DataMember(Order = 2)]
        public SerializableReferenceLocation[]? ImplicitLocations;

        [DataMember(Order = 3)]
        public SerializableSymbolAndProjectId[]? ReferencedSymbols;

        public SerializableRenameLocations(
            SymbolRenameOptions options,
            SerializableRenameLocation[] locations,
            SerializableReferenceLocation[]? implicitLocations,
            SerializableSymbolAndProjectId[]? referencedSymbols)
        {
            Options = options;
            Locations = locations;
            ImplicitLocations = implicitLocations;
            ReferencedSymbols = referencedSymbols;
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
            if (!IsSuccessful)
                return new SerializableConflictResolution(ErrorMessage, resolution: null);

            var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(OldSolution, _newSolutionWithoutRenamedDocument, cancellationToken).ConfigureAwait(false);
            return new SerializableConflictResolution(
                errorMessage: null,
                new SuccessfulConflictResolution(
                    ReplacementTextValid.Value,
                    _renamedDocument.Value,
                    DocumentIds,
                    RelatedLocations,
                    documentTextChanges,
                    _documentToModifiedSpansMap,
                    _documentToComplexifiedSpansMap,
                    _documentToRelatedLocationsMap));
        }
    }
}
