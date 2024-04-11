// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Roslyn.Utilities;

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
    public static readonly AssetPath SolutionOnly = new(AssetPathKind.Solution);

    /// <summary>
    /// Instance that will only look up solution-level, as well and projects when searching for checksums.  It will not
    /// descend into documents.
    /// </summary>
    public static readonly AssetPath SolutionAndProjects = new(AssetPathKind.Solution | AssetPathKind.Projects);

    /// <summary>
    /// Only search at the project level when searching for checksums.
    /// </summary>
    public static readonly AssetPath ProjectsOnly = new(AssetPathKind.Projects);

    /// <summary>
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = new(AssetPathKind.Solution | AssetPathKind.Projects | AssetPathKind.Documents);

    [DataMember(Order = 0)]
    private readonly AssetPathKind _kind;

    /// <summary>
    /// If not null, the search should only descend into the single project with this id.
    /// </summary>
    [DataMember(Order = 1)]
    public readonly ProjectId? ProjectId;

    /// <summary>
    /// If not null, the search should only descend into the single document with this id.
    /// </summary>
    [DataMember(Order = 2)]
    public readonly DocumentId? DocumentId;

    private AssetPath(AssetPathKind kind, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        _kind = kind;
        ProjectId = projectId;
        DocumentId = documentId;
    }

    public bool IncludeSolution => (_kind & AssetPathKind.Solution) == AssetPathKind.Solution;
    public bool IncludeProjects => (_kind & AssetPathKind.Projects) == AssetPathKind.Projects;
    public bool IncludeDocuments => (_kind & AssetPathKind.Documents) == AssetPathKind.Documents;

    /// <summary>
    /// Searches only for information about this project.
    /// </summary>
    public static implicit operator AssetPath(ProjectId projectId) => new(AssetPathKind.Projects, projectId, documentId: null);

    /// <summary>
    /// Searches only for information about this document.
    /// </summary>
    public static implicit operator AssetPath(DocumentId documentId) => new(AssetPathKind.Documents, documentId.ProjectId, documentId);

    /// <summary>
    /// Searches the requested project, and all documents underneath it.  Used only in tests.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath SolutionAndProjectForTesting(ProjectId projectId)
        => new(AssetPathKind.Solution | AssetPathKind.Projects, projectId);

    /// <summary>
    /// Searches all documents within the specified project.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath DocumentsInProject(ProjectId projectId)
        => new(AssetPathKind.Documents, projectId);

    [Flags]
    private enum AssetPathKind
    {
        /// <summary>
        /// Search solution-level information.
        /// </summary>
        Solution = 1 << 0,

        /// <summary>
        /// Search projects for results.
        /// </summary>
        Projects = 1 << 1,

        /// <summary>
        /// Search documents for results.
        /// </summary>
        Documents = 1 << 2,
    }
}
