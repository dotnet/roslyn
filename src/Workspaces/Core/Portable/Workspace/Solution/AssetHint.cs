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
    public static readonly AssetPath SolutionOnly = new(AssetPathKind.Solution, forTesting: false);

    /// <summary>
    /// Instance that will only look up solution-level, as well as the top level nodes for projects when searching for
    /// checksums.  It will not descend into projects.
    /// </summary>
    public static readonly AssetPath SolutionAndTopLevelProjectsOnly = new(AssetPathKind.Solution | AssetPathKind.TopLevelProjects, forTesting: false);

    /// <summary>
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = new(AssetPathKind.Solution | AssetPathKind.TopLevelProjects | AssetPathKind.Projects | AssetPathKind.Documents, forTesting: true);

    [DataMember(Order = 0)]
    private readonly AssetPathKind _kind;
#pragma warning disable IDE0052 // Remove unread private members
    [DataMember(Order = 1)]
    private readonly bool _forTesting;
#pragma warning restore IDE0052 // Remove unread private members
    [DataMember(Order = 2)]
    public readonly ProjectId? ProjectId;
    [DataMember(Order = 3)]
    public readonly DocumentId? DocumentId;

    private AssetPath(AssetPathKind kind, bool forTesting, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        _kind = kind;
        _forTesting = forTesting;
        ProjectId = projectId;
        DocumentId = documentId;

        if (forTesting)
        {
            // Only tests are allowed to search everything.  And, in that case, they don't pass any doc/project along.
            Contract.ThrowIfTrue(projectId != null);
            Contract.ThrowIfTrue(documentId != null);
        }
        else
        {
            // Otherwise, if not in testing, if we say we're searching projects or documents, we have to supply those IDs as well.
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
    public static implicit operator AssetPath(ProjectId projectId) => new(AssetPathKind.Projects, forTesting: false, projectId, documentId: null);

    /// <summary>
    /// Searches only for information about this document.
    /// </summary>
    public static implicit operator AssetPath(DocumentId documentId) => new(AssetPathKind.Documents, forTesting: false, documentId.ProjectId, documentId);

    /// <summary>
    /// Searches the requested project, and all documents underneath it.  Used only in tests.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath SolutionAndProjectForTesting(ProjectId projectId)
        => new(AssetPathKind.Solution | AssetPathKind.Projects, forTesting: true, projectId);

    /// <summary>
    /// Searches the requested project, and all documents underneath it.  used during normal sync when bulk syncing a
    /// project.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath ProjectAndDocuments(ProjectId projectId)
        => new(AssetPathKind.Projects | AssetPathKind.Documents, forTesting: false, projectId);

    [Flags]
    private enum AssetPathKind
    {
        Solution = 1 << 0,
        TopLevelProjects = 1 << 1,
        Projects = 1 << 2,
        Documents = 1 << 3,
    }
}
