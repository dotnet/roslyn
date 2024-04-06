// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// Required information passed with an asset synchronization request to tell the host where to scope the request to. In
/// particular, this is often used to scope to a particular <see cref="Project"/> or <see cref="Document"/> to avoid
/// having to search the entire solution.
/// </summary>
[DataContract]
internal readonly struct AssetPath
{
    /// <summary>
    /// Instance that will only look up solution-level data when searching for checksums.
    /// </summary>
    public static readonly AssetPath SolutionOnly = default;

    /// <summary>
    /// Instance that will only look up solution-level, as well as the top level nodes for projects when searching for
    /// checksums.  It will not descend into projects.
    /// </summary>
    public static readonly AssetPath SolutionAndTopLevelProjectsOnly = new(projectId: null, documentId: null, topLevelProjects: true, isFullLookup_ForTestingPurposesOnly: false);

    /// <summary>
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = new(projectId: null, documentId: null, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: true);

    [DataMember(Order = 0)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 1)]
    public readonly DocumentId? DocumentId;
    [DataMember(Order = 3)]
    public readonly bool TopLevelProjects;
    [DataMember(Order = 4)]
    public readonly bool IsFullLookup_ForTestingPurposesOnly;

    public bool IsSolutionOnly => !IsFullLookup_ForTestingPurposesOnly && ProjectId is null;

    private AssetPath(ProjectId? projectId, DocumentId? documentId, bool topLevelProjects, bool isFullLookup_ForTestingPurposesOnly)
    {
        ProjectId = projectId;
        DocumentId = documentId;
        TopLevelProjects = topLevelProjects;
        IsFullLookup_ForTestingPurposesOnly = isFullLookup_ForTestingPurposesOnly;
    }

    public static implicit operator AssetPath(ProjectId projectId) => new(projectId, documentId: null, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: false);
    public static implicit operator AssetPath(DocumentId documentId) => new(documentId.ProjectId, documentId, topLevelProjects: false, isFullLookup_ForTestingPurposesOnly: false);
}
