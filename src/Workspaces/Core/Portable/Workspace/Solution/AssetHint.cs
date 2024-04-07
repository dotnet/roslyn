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
    /// Instance that will only look up solution-level, as well as the top level nodes for projects when searching for
    /// checksums.  It will not descend into projects.
    /// </summary>
    public static readonly AssetPath SolutionAndTopLevelProjectsOnly = new(AssetPathKind.Solution | AssetPathKind.TopLevelProjects);

    /// <summary>
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = new(AssetPathKind.Solution | AssetPathKind.TopLevelProjects | AssetPathKind.Projects | AssetPathKind.Documents | AssetPathKind.Testing);

    [DataMember(Order = 0)]
    private readonly AssetPathKind _kind;
    [DataMember(Order = 1)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 2)]
    public readonly DocumentId? DocumentId;

    private AssetPath(AssetPathKind kind, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        _kind = kind;
        ProjectId = projectId;
        DocumentId = documentId;

        // If this isn't a test lookup, and we're searching into projects or documents, then we must have at least a
        // projectId to limit the search.  If we don't, that risks very expensive searches where we look into *every*
        // project in the solution for matches.
        if ((kind & AssetPathKind.Testing) == 0)
        {
            if (IncludeProjects || IncludeDocuments)
                Contract.ThrowIfNull(projectId);
        }
    }

    public bool IncludeSolution => (_kind & AssetPathKind.Solution) == AssetPathKind.Solution;
    public bool IncludeTopLevelProjects => (_kind & AssetPathKind.TopLevelProjects) == AssetPathKind.TopLevelProjects;
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
        => new(AssetPathKind.Solution | AssetPathKind.Projects | AssetPathKind.Testing, projectId);

    /// <summary>
    /// Searches the requested project, and all documents underneath it.  used during normal sync when bulk syncing a
    /// project.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath ProjectAndDocuments(ProjectId projectId)
        => new(AssetPathKind.Projects | AssetPathKind.Documents, projectId);

    [Flags]
    private enum AssetPathKind
    {
        /// <summary>
        /// Search solution-level information.
        /// </summary>
        Solution = 1 << 0,

        /// <summary>
        /// Search projects, without descending into them. In effect, only finding direct ProjectStateChecksum children
        /// of the solution.
        /// </summary>
        TopLevelProjects = 1 << 1,

        /// <summary>
        /// Search projects for results.
        /// </summary>
        Projects = 1 << 2,

        /// <summary>
        /// Search documents for results.
        /// </summary>
        Documents = 1 << 3,

        /// <summary>
        /// Indicates that this is a special search performed during testing.  These searches are allowed to search
        /// everything for expediency purposes.
        /// </summary>
        Testing = 1 << 4,
    }
}
