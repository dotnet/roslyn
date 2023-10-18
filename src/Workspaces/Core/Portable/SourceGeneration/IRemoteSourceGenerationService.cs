﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SourceGeneration;

internal interface IRemoteSourceGenerationService
{
    /// <summary>
    /// Given a particular project in the remote solution snapshot, return information about all the generated documents
    /// in that project.  The information includes the <see cref="SourceGeneratedDocumentIdentity"/> identity
    /// information about the document, as well as its text <see cref="Checksum"/>.  The local workspace can then
    /// compare that to the prior generated documents it has to see if it can reuse those directly, or if it needs to
    /// remove any documents no longer around, add any new documents, or change the contents of any existing documents.
    /// </summary>
    ValueTask<ImmutableArray<(SourceGeneratedDocumentIdentity documentIdentity, SourceGeneratedDocumentContentIdentity contentIdentity)>> GetSourceGenerationInfoAsync(
        Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Given a particular set of generated document ids, returns the fully generated content for those documents.
    /// Should only be called by the host for documents it does not know about, or documents whose checksum contents are
    /// different than the last time the document was queried.
    /// </summary>
    ValueTask<ImmutableArray<string>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
}

/// <summary>
/// Information that uniquely identifies the content of a source-generated document and ensures the remote and local
/// hosts are in agreement on them.
/// </summary>
/// <param name="Checksum">Checksum produced from <see cref="SourceText.GetChecksum"/>.</param>
/// <param name="EncodingName">Result of <see cref="SourceText.Encoding"/>'s <see cref="Encoding.WebName"/>.</param>
/// <param name="ChecksumAlgorithm">Result of <see cref="SourceText.ChecksumAlgorithm"/>.</param>
[DataContract]
internal readonly record struct SourceGeneratedDocumentContentIdentity(
    [property: DataMember(Order = 0)] Checksum Checksum,
    [property: DataMember(Order = 1)] string? EncodingName,
    [property: DataMember(Order = 2)] SourceHashAlgorithm ChecksumAlgorithm);
