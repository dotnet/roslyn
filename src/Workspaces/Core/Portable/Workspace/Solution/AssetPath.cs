// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    /// Special instance, allowed only in tests/debug-asserts, that can do a full lookup across the entire checksum
    /// tree.  Should not be used in normal release-mode product code.
    /// </summary>
    public static readonly AssetPath FullLookupForTesting = AssetPathKind.SolutionCompilationState | AssetPathKind.SolutionState | AssetPathKind.Projects | AssetPathKind.Documents;

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

    public AssetPath(AssetPathKind kind, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        _kind = kind;
        ProjectId = projectId;
        DocumentId = documentId;
    }

    public AssetPath(AssetPathKind kind, ProjectId? projectId)
        : this(kind, projectId, documentId: null)
    {
    }

    public AssetPath(AssetPathKind kind, DocumentId? documentId)
        : this(kind, documentId?.ProjectId, documentId)
    {
    }

    public bool IncludeSolutionCompilationState => (_kind & AssetPathKind.SolutionCompilationState) != 0;
    public bool IncludeSolutionState => (_kind & AssetPathKind.SolutionState) != 0;
    public bool IncludeProjects => (_kind & AssetPathKind.Projects) != 0;
    public bool IncludeDocuments => (_kind & AssetPathKind.Documents) != 0;

    public bool IncludeSolutionCompilationStateChecksums => (_kind & AssetPathKind.SolutionCompilationStateChecksums) != 0;
    public bool IncludeSolutionSourceGeneratorExecutionVersionMap => (_kind & AssetPathKind.SolutionSourceGeneratorExecutionVersionMap) != 0;
    public bool IncludeSolutionFrozenSourceGeneratedDocumentIdentities => (_kind & AssetPathKind.SolutionFrozenSourceGeneratedDocumentIdentities) != 0;
    public bool IncludeSolutionFrozenSourceGeneratedDocumentText => (_kind & AssetPathKind.SolutionFrozenSourceGeneratedDocumentText) != 0;

    public bool IncludeSolutionStateChecksums => (_kind & AssetPathKind.SolutionStateChecksums) != 0;
    public bool IncludeSolutionAttributes => (_kind & AssetPathKind.SolutionAttributes) != 0;
    public bool IncludeSolutionAnalyzerReferences => (_kind & AssetPathKind.SolutionAnalyzerReferences) != 0;
    public bool IncludeSolutionFallbackAnalyzerOptions => (_kind & AssetPathKind.SolutionFallbackAnalyzerOptions) != 0;

    public bool IncludeProjectStateChecksums => (_kind & AssetPathKind.ProjectStateChecksums) != 0;
    public bool IncludeProjectAttributes => (_kind & AssetPathKind.ProjectAttributes) != 0;
    public bool IncludeProjectCompilationOptions => (_kind & AssetPathKind.ProjectCompilationOptions) != 0;
    public bool IncludeProjectParseOptions => (_kind & AssetPathKind.ProjectParseOptions) != 0;
    public bool IncludeProjectProjectReferences => (_kind & AssetPathKind.ProjectProjectReferences) != 0;
    public bool IncludeProjectMetadataReferences => (_kind & AssetPathKind.ProjectMetadataReferences) != 0;
    public bool IncludeProjectAnalyzerReferences => (_kind & AssetPathKind.ProjectAnalyzerReferences) != 0;

    public bool IncludeDocumentAttributes => (_kind & AssetPathKind.DocumentAttributes) != 0;
    public bool IncludeDocumentText => (_kind & AssetPathKind.DocumentText) != 0;

    public static implicit operator AssetPath(AssetPathKind kind) => new(kind);

    /// <summary>
    /// Searches only for information about this project.
    /// </summary>
    public static implicit operator AssetPath(ProjectId projectId) => new(AssetPathKind.Projects, projectId);

    /// <summary>
    /// Searches only for information about this document.
    /// </summary>
    public static implicit operator AssetPath(DocumentId documentId) => new(AssetPathKind.Documents, documentId);

    /// <summary>
    /// Searches the requested project, and all documents underneath it.  Used only in tests.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath SolutionAndProjectForTesting(ProjectId projectId)
        => new(AssetPathKind.SolutionCompilationState | AssetPathKind.SolutionState | AssetPathKind.Projects, projectId);

    /// <summary>
    /// Searches all documents within the specified project.
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns></returns>
    public static AssetPath DocumentsInProject(ProjectId projectId)
        => new(AssetPathKind.Documents, projectId);
}

[Flags]
internal enum AssetPathKind
{
    SolutionCompilationStateChecksums = 1 << 0,
    SolutionSourceGeneratorExecutionVersionMap = 1 << 2,
    SolutionFrozenSourceGeneratedDocumentIdentities = 1 << 3,
    SolutionFrozenSourceGeneratedDocumentText = 1 << 4,

    // Keep a gap so we can easily add more solution compilation state kinds
    SolutionStateChecksums = 1 << 10,
    SolutionAttributes = 1 << 11,
    SolutionAnalyzerReferences = 1 << 12,
    SolutionFallbackAnalyzerOptions = 1 << 13,

    // Keep a gap so we can easily add more solution kinds
    ProjectStateChecksums = 1 << 15,
    ProjectAttributes = 1 << 16,
    ProjectCompilationOptions = 1 << 17,
    ProjectParseOptions = 1 << 18,
    ProjectProjectReferences = 1 << 19,
    ProjectMetadataReferences = 1 << 20,
    ProjectAnalyzerReferences = 1 << 21,

    // Keep a gap so we can easily add more project kinds
    DocumentAttributes = 1 << 25,
    DocumentText = 1 << 26,

    /// <summary>
    /// Search solution-compilation-state level information.
    /// </summary>
    SolutionCompilationState = SolutionCompilationStateChecksums | SolutionSourceGeneratorExecutionVersionMap | SolutionFrozenSourceGeneratedDocumentIdentities | SolutionFrozenSourceGeneratedDocumentText,

    /// <summary>
    /// Search solution-state level information.
    /// </summary>
    SolutionState = SolutionStateChecksums | SolutionAttributes | SolutionAnalyzerReferences | SolutionFallbackAnalyzerOptions,

    /// <summary>
    /// Search projects for results.  All project-level information will be searched.
    /// </summary>
    Projects = ProjectStateChecksums | ProjectAttributes | ProjectCompilationOptions | ProjectParseOptions | ProjectProjectReferences | ProjectMetadataReferences | ProjectAnalyzerReferences,

    /// <summary>
    /// Search documents for results.
    /// </summary>
    Documents = DocumentAttributes | DocumentText,
}
