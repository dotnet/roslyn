// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

internal interface IRemoteRenamerService
{
    /// <summary>
    /// Runs the entire rename operation OOP and returns the final result. More efficient (due to less back and
    /// forth marshaling) when the intermediary results of rename are not needed. To get the individual parts of
    /// rename remoted use <see cref="FindRenameLocationsAsync"/> and <see cref="ResolveConflictsAsync"/>.
    /// </summary>
    ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
        Checksum solutionChecksum,
        SerializableSymbolAndProjectId symbolAndProjectId,
        string replacementText,
        SymbolRenameOptions options,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken);

    ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
        Checksum solutionChecksum,
        SerializableSymbolAndProjectId symbolAndProjectId,
        SymbolRenameOptions options,
        CancellationToken cancellationToken);

    ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
        Checksum solutionChecksum,
        SerializableSymbolAndProjectId symbolAndProjectId,
        SerializableRenameLocations renameLocationSet,
        string replacementText,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken);
}

[DataContract]
internal readonly struct SerializableRenameLocation(
    TextSpan location,
    DocumentId documentId,
    CandidateReason candidateReason,
    bool isRenamableAliasUsage,
    bool isRenamableAccessor,
    TextSpan containingLocationForStringOrComment,
    bool isWrittenTo)
{
    [DataMember(Order = 0)]
    public readonly TextSpan Location = location;

    [DataMember(Order = 1)]
    public readonly DocumentId DocumentId = documentId;

    [DataMember(Order = 2)]
    public readonly CandidateReason CandidateReason = candidateReason;

    [DataMember(Order = 3)]
    public readonly bool IsRenamableAliasUsage = isRenamableAliasUsage;

    [DataMember(Order = 4)]
    public readonly bool IsRenamableAccessor = isRenamableAccessor;

    [DataMember(Order = 5)]
    public readonly TextSpan ContainingLocationForStringOrComment = containingLocationForStringOrComment;

    [DataMember(Order = 6)]
    public readonly bool IsWrittenTo = isWrittenTo;

    public static SerializableRenameLocation Dehydrate(RenameLocation location)
        => new(location.Location.SourceSpan,
               location.DocumentId,
               location.CandidateReason,
               location.IsRenamableAliasUsage,
               location.IsRenamableAccessor,
               location.ContainingLocationForStringOrComment,
               location.IsWrittenTo);

    public async ValueTask<RenameLocation> RehydrateAsync(Solution solution, CancellationToken cancellation)
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
            Locations.SelectAsArray(SerializableRenameLocation.Dehydrate),
            _implicitLocations,
            _referencedSymbols);
}

internal partial class SymbolicRenameLocations
{
    internal static async Task<SymbolicRenameLocations?> TryRehydrateAsync(
        ISymbol symbol, Solution solution, SerializableRenameLocations serializableLocations, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(serializableLocations);

        var locations = await serializableLocations.Locations.SelectAsArrayAsync(
            selector: static (loc, solution, cancellationToken) => loc.RehydrateAsync(solution, cancellationToken), arg: solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        var implicitLocations = await serializableLocations.ImplicitLocations.SelectAsArrayAsync(
        selector: static (loc, solution, cancellationToken) => loc.RehydrateAsync(solution, cancellationToken), arg: solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        var referencedSymbols = await serializableLocations.ReferencedSymbols.SelectAsArrayAsync(
            selector: static (sym, solution, cancellationToken) => sym.TryRehydrateAsync(solution, cancellationToken), arg: solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (referencedSymbols.Any(s => s is null))
            return null;

        return new SymbolicRenameLocations(
            symbol,
            solution,
            serializableLocations.Options,
            locations,
            implicitLocations,
            referencedSymbols);
    }
}

[DataContract]
internal sealed class SerializableRenameLocations(
    SymbolRenameOptions options,
    ImmutableArray<SerializableRenameLocation> locations,
    ImmutableArray<SerializableReferenceLocation> implicitLocations,
    ImmutableArray<SerializableSymbolAndProjectId> referencedSymbols)
{
    [DataMember(Order = 0)]
    public readonly SymbolRenameOptions Options = options;

    [DataMember(Order = 1)]
    public readonly ImmutableArray<SerializableRenameLocation> Locations = locations;

    [DataMember(Order = 2)]
    public readonly ImmutableArray<SerializableReferenceLocation> ImplicitLocations = implicitLocations;

    [DataMember(Order = 3)]
    public readonly ImmutableArray<SerializableSymbolAndProjectId> ReferencedSymbols = referencedSymbols;

    public async ValueTask<ImmutableArray<RenameLocation>> RehydrateLocationsAsync(
        Solution solution, CancellationToken cancellationToken)
    {
        var locBuilder = new FixedSizeArrayBuilder<RenameLocation>(this.Locations.Length);
        foreach (var loc in this.Locations)
            locBuilder.Add(await loc.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

        return locBuilder.MoveToImmutable();
    }
}

[DataContract]
internal sealed class SerializableConflictResolution(string? errorMessage, SuccessfulConflictResolution? resolution)
{
    [DataMember(Order = 0)]
    public readonly string? ErrorMessage = errorMessage;

    [DataMember(Order = 1)]
    public readonly SuccessfulConflictResolution? Resolution = resolution;

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
internal sealed class SuccessfulConflictResolution(
    bool replacementTextValid,
    (DocumentId documentId, string newName) renamedDocument,
    ImmutableArray<DocumentId> documentIds,
    ImmutableArray<RelatedLocation> relatedLocations,
    ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> documentTextChanges,
    ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> documentToModifiedSpansMap,
    ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> documentToComplexifiedSpansMap,
    ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> documentToRelatedLocationsMap)
{
    [DataMember(Order = 0)]
    public readonly bool ReplacementTextValid = replacementTextValid;

    [DataMember(Order = 1)]
    public readonly (DocumentId documentId, string newName) RenamedDocument = renamedDocument;

    [DataMember(Order = 2)]
    public readonly ImmutableArray<DocumentId> DocumentIds = documentIds;

    [DataMember(Order = 3)]
    public readonly ImmutableArray<RelatedLocation> RelatedLocations = relatedLocations;

    [DataMember(Order = 4)]
    public readonly ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> DocumentTextChanges = documentTextChanges;

    [DataMember(Order = 5)]
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)>> DocumentToModifiedSpansMap = documentToModifiedSpansMap;

    [DataMember(Order = 6)]
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<ComplexifiedSpan>> DocumentToComplexifiedSpansMap = documentToComplexifiedSpansMap;

    [DataMember(Order = 7)]
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<RelatedLocation>> DocumentToRelatedLocationsMap = documentToRelatedLocationsMap;
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
                ReplacementTextValid,
                _renamedDocument.Value,
                DocumentIds,
                RelatedLocations,
                documentTextChanges,
                _documentToModifiedSpansMap,
                _documentToComplexifiedSpansMap,
                _documentToRelatedLocationsMap));
    }
}
