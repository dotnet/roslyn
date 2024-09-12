// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// Optional information passed with an asset synchronization request to allow the request to be scoped down to a
/// particular <see cref="Project"/> or <see cref="Document"/>.
/// </summary>
[DataContract]
internal readonly struct AssetHint
{
    public static readonly AssetHint None = default;

    [DataMember(Order = 0)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 1)]
    public readonly DocumentId? DocumentId;

    private AssetHint(ProjectId? projectId, DocumentId? documentId)
    {
        ProjectId = projectId;
        DocumentId = documentId;
    }

    public static implicit operator AssetHint(ProjectId projectId) => new(projectId, documentId: null);
    public static implicit operator AssetHint(DocumentId documentId) => new(documentId.ProjectId, documentId);
}
