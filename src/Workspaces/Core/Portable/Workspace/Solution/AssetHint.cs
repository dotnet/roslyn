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
    /// <summary>
    /// Instance that will only look up solution-level data when searching for checksums.
    /// </summary>
    public static AssetHint SolutionOnly = default;

    /// <summary>
    /// Instance that will only look up solution-level, as well as the top level nodes for projects when searching for
    /// checksums.  It will not descend into projects.
    /// </summary>
    public static AssetHint SolutionAndTopLevelProjectsOnly = new(projectId: null, documentId: null, topLevelProjects: true, isFullLookup_ForTestingPurposesOnly: false);

    /// <summary>
    /// Special instance, allowed only in tests, that can do a full lookup across the entire checksum tree.
    /// </summary>
    public static AssetHint FullLookupForTesting = new(projectId: null, documentId: null, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: true);

    [DataMember(Order = 0)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 1)]
    public readonly DocumentId? DocumentId;
    [DataMember(Order = 3)]
    public readonly bool TopLevelProjects;
    [DataMember(Order = 4)]
    public readonly bool IsFullLookup_ForTestingPurposesOnly;

    public bool IsSolutionOnly => !IsFullLookup_ForTestingPurposesOnly && ProjectId is null;

    private AssetHint(ProjectId? projectId, DocumentId? documentId, bool topLevelProjects, bool isFullLookup_ForTestingPurposesOnly)
    {
        ProjectId = projectId;
        DocumentId = documentId;
        TopLevelProjects = topLevelProjects;
        IsFullLookup_ForTestingPurposesOnly = isFullLookup_ForTestingPurposesOnly;
    }

    public static implicit operator AssetHint(ProjectId projectId) => new(projectId, documentId: null, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: false);
    public static implicit operator AssetHint(DocumentId documentId) => new(documentId.ProjectId, documentId, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: false);
}
