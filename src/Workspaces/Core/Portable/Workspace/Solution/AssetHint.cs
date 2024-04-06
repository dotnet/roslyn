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
    public static readonly AssetPath SolutionOnly = new(kind: AssetPathKind.Solution);

    /// <summary>
    /// Instance that will only look up solution-level, as well as the top level nodes for projects when searching for
    /// checksums.  It will not descend into projects.
    /// </summary>
    public static readonly AssetPath SolutionAndTopLevelProjectsOnly = new(kind: AssetPathKind.SolutionAndTopLevelProjects);

    /// <summary>
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = new(kind: AssetPathKind.FullLookupForTests);

    [DataMember(Order = 0)]
    private readonly AssetPathKind _kind;
    [DataMember(Order = 1)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 2)]
    public readonly DocumentId? DocumentId;

    public bool TopLevelProjects => _kind == AssetPathKind.SolutionAndTopLevelProjects;
    public bool IsFullLookup_ForTestingPurposesOnly => _kind == AssetPathKind.FullLookupForTests;
    public bool IsSolutionOnly => _kind == AssetPathKind.Solution;

    private AssetPath(AssetPathKind kind, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        _kind = kind;
        ProjectId = projectId;
        DocumentId = documentId;
    }

    public static implicit operator AssetPath(ProjectId projectId) => new(kind: AssetPathKind.ProjectOrDocument, projectId, documentId: null);
    public static implicit operator AssetPath(DocumentId documentId) => new(kind: AssetPathKind.ProjectOrDocument, documentId.ProjectId, documentId);

    private enum AssetPathKind
    {
        Solution = 0,
        SolutionAndTopLevelProjects = 1,
        FullLookupForTests = 2,
        ProjectOrDocument = 3,
    }
}
