// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SourceGeneration;

[DataContract]
internal readonly record struct SourceGeneratedDocumentInfo(
    [property: DataMember(Order = 0)] SourceGeneratedDocumentIdentity DocumentIdentity,
    [property: DataMember(Order = 1)] SourceGeneratedDocumentContentIdentity ContentIdentity,
    [property: DataMember(Order = 2)] DateTime GenerationDateTime);

internal interface IRemoteSourceGenerationService
{
    /// <summary>
    /// Given a particular project in the remote solution snapshot, return information about all the generated documents
    /// in that project.  The information includes the <see cref="SourceGeneratedDocumentIdentity"/> identity
    /// information about the document, as well as its text <see cref="Checksum"/>.  The local workspace can then
    /// compare that to the prior generated documents it has to see if it can reuse those directly, or if it needs to
    /// remove any documents no longer around, add any new documents, or change the contents of any existing documents.
    /// </summary>
    /// <param name="withFrozenSourceGeneratedDocuments">Controls if the caller wants frozen source generator documents
    /// included in the result, or if only the most underlying generated documents (produced by the real compiler <see
    /// cref="GeneratorDriver"/> should be included.</param>
    ValueTask<ImmutableArray<SourceGeneratedDocumentInfo>> GetSourceGeneratedDocumentInfoAsync(
        Checksum solutionChecksum, ProjectId projectId, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken);

    /// <summary>
    /// Given a particular set of generated document ids, returns the fully generated content for those documents.
    /// Should only be called by the host for documents it does not know about, or documents whose checksum contents are
    /// different than the last time the document was queried.
    /// </summary>
    /// <param name="withFrozenSourceGeneratedDocuments">Controls if the caller wants frozen source generator documents
    /// included in the result, or if only the most underlying generated documents (produced by the real compiler <see
    /// cref="GeneratorDriver"/> should be included.</param>
    ValueTask<ImmutableArray<string>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken);

    /// <summary>
    /// Whether or not the specified analyzer references have source generators or not.
    /// </summary>
    ValueTask<bool> HasGeneratorsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<Checksum> analyzerReferenceChecksums, string language, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the identities for all source generators found in the <see cref="AnalyzerReference"/> with <see
    /// cref="AnalyzerFileReference.FullPath"/> equal to <paramref name="analyzerReferenceFullPath"/>.
    ValueTask<ImmutableArray<SourceGeneratorIdentity>> GetSourceGeneratorIdentitiesAsync(
        Checksum solutionChecksum, ProjectId projectId, string analyzerReferenceFullPath, CancellationToken cancellationToken);
}

/// <summary>
/// Information that uniquely identifies the content of a source-generated document and ensures the remote and local
/// hosts are in agreement on them.
/// </summary>
/// <param name="OriginalSourceTextContentHash">Checksum originally produced from <see cref="SourceText.GetChecksum"/> on
/// the server side.  This may technically not be the same checksum that is produced on the client side once the
/// SourceText is hydrated there.  See comments on <see
/// cref="SourceGeneratedDocumentState.GetOriginalSourceTextContentHash"/> for more details on when this happens.</param>
/// <param name="EncodingName">Result of <see cref="SourceText.Encoding"/>'s <see cref="Encoding.WebName"/>.</param>
/// <param name="ChecksumAlgorithm">Result of <see cref="SourceText.ChecksumAlgorithm"/>.</param>
[DataContract]
internal readonly record struct SourceGeneratedDocumentContentIdentity(
    [property: DataMember(Order = 0)] Checksum OriginalSourceTextContentHash,
    [property: DataMember(Order = 1)] string? EncodingName,
    [property: DataMember(Order = 2)] SourceHashAlgorithm ChecksumAlgorithm);
