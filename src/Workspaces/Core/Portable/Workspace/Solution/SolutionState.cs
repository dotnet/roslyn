// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal readonly record struct StateChange(
    SolutionState NewSolutionState,
    ProjectState OldProjectState,
    ProjectState NewProjectState);

/// <summary>
/// Represents a set of projects and their source code documents.
///
/// this is a green node of Solution like ProjectState/DocumentState are for
/// Project and Document.
/// </summary>
internal sealed partial class SolutionState
{
    public static readonly IEqualityComparer<string> FilePathComparer = CachingFilePathComparer.Instance;

    // the version of the workspace this solution is from
    public int WorkspaceVersion { get; }
    public string? WorkspaceKind { get; }
    public SolutionServices Services { get; }
    public SolutionOptionSet Options { get; }
    public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

    /// <summary>
    /// Fallback analyzer config options by language. The set of languages does not need to match the set of langauges of projects included in the surrent solution snapshot.
    /// </summary>
    public ImmutableDictionary<string, StructuredAnalyzerConfigOptions> FallbackAnalyzerOptions { get; } = ImmutableDictionary<string, StructuredAnalyzerConfigOptions>.Empty;

    /// <summary>
    /// Number of projects in the solution of the given language.  The value is guaranteed to always be greater than zero.
    /// If the project count does ever hit zero then there simply is no key/value pair for that language in this map.
    /// </summary>
    internal ImmutableDictionary<string, int> ProjectCountByLanguage { get; } = ImmutableDictionary<string, int>.Empty;

    private readonly ProjectDependencyGraph _dependencyGraph;

    // holds on data calculated based on the AnalyzerReferences list
    private readonly Lazy<HostDiagnosticAnalyzers> _lazyAnalyzers;

    private ImmutableDictionary<string, ImmutableArray<DocumentId>> _lazyFilePathToRelatedDocumentIds = ImmutableDictionary<string, ImmutableArray<DocumentId>>.Empty.WithComparers(FilePathComparer);

    private SolutionState(
        string? workspaceKind,
        int workspaceVersion,
        SolutionServices services,
        SolutionInfo.SolutionAttributes solutionAttributes,
        IReadOnlyList<ProjectId> projectIds,
        SolutionOptionSet options,
        IReadOnlyList<AnalyzerReference> analyzerReferences,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions> fallbackAnalyzerOptions,
        ImmutableDictionary<string, int> projectCountByLanguage,
        ImmutableDictionary<ProjectId, ProjectState> idToProjectStateMap,
        ProjectDependencyGraph dependencyGraph,
        Lazy<HostDiagnosticAnalyzers>? lazyAnalyzers)
    {
        WorkspaceKind = workspaceKind;
        WorkspaceVersion = workspaceVersion;
        SolutionAttributes = solutionAttributes;
        Services = services;
        ProjectIds = projectIds;
        Options = options;
        AnalyzerReferences = analyzerReferences;
        FallbackAnalyzerOptions = fallbackAnalyzerOptions;
        ProjectCountByLanguage = projectCountByLanguage;
        ProjectStates = idToProjectStateMap;
        _dependencyGraph = dependencyGraph;
        _lazyAnalyzers = lazyAnalyzers ?? CreateLazyHostDiagnosticAnalyzers(analyzerReferences);

        // when solution state is changed, we recalculate its checksum
        _lazyChecksums = AsyncLazy.Create(static (self, c) =>
            self.ComputeChecksumsAsync(projectConeId: null, c),
            arg: this);

        CheckInvariants();

        // make sure we don't accidentally capture any state but the list of references:
        static Lazy<HostDiagnosticAnalyzers> CreateLazyHostDiagnosticAnalyzers(IReadOnlyList<AnalyzerReference> analyzerReferences)
            => new(() => new HostDiagnosticAnalyzers(analyzerReferences));
    }

    public SolutionState(
        string? workspaceKind,
        SolutionServices services,
        SolutionInfo.SolutionAttributes solutionAttributes,
        SolutionOptionSet options,
        IReadOnlyList<AnalyzerReference> analyzerReferences,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions> fallbackAnalyzerOptions)
        : this(
            workspaceKind,
            workspaceVersion: 0,
            services,
            solutionAttributes,
            projectIds: SpecializedCollections.EmptyBoxedImmutableArray<ProjectId>(),
            options,
            analyzerReferences,
            fallbackAnalyzerOptions,
            projectCountByLanguage: ImmutableDictionary<string, int>.Empty,
            idToProjectStateMap: ImmutableDictionary<ProjectId, ProjectState>.Empty,
            dependencyGraph: ProjectDependencyGraph.Empty,
            lazyAnalyzers: null)
    {
    }

    public HostDiagnosticAnalyzers Analyzers => _lazyAnalyzers.Value;

    public SolutionInfo.SolutionAttributes SolutionAttributes { get; }

    public ImmutableDictionary<ProjectId, ProjectState> ProjectStates { get; }

    /// <summary>
    /// The Id of the solution. Multiple solution instances may share the same Id.
    /// </summary>
    public SolutionId Id => SolutionAttributes.Id;

    /// <summary>
    /// The path to the solution file or null if there is no solution file.
    /// </summary>
    public string? FilePath => SolutionAttributes.FilePath;

    /// <summary>
    /// The solution version. This equates to the solution file's version.
    /// </summary>
    public VersionStamp Version => SolutionAttributes.Version;

    /// <summary>
    /// A list of all the ids for all the projects contained by the solution.
    /// </summary>
    public IReadOnlyList<ProjectId> ProjectIds { get; }

    private void CheckInvariants()
    {
        // Run these quick checks all the time.  We need to know immediately if we violate these.
        Contract.ThrowIfFalse(ProjectStates.Count == ProjectIds.Count);
        Contract.ThrowIfFalse(ProjectStates.Count == _dependencyGraph.ProjectIds.Count);

        // Only run this in debug builds; even the .SetEquals() call across all projects can be expensive when there's a lot of them.
#if DEBUG
        // project ids must be the same:
        Debug.Assert(ProjectStates.Keys.SetEquals(ProjectIds));
        Debug.Assert(ProjectStates.Keys.SetEquals(_dependencyGraph.ProjectIds));
#endif
    }

    internal SolutionState Branch(
        ImmutableDictionary<string, int>? projectCountByLanguage = null,
        SolutionInfo.SolutionAttributes? solutionAttributes = null,
        IReadOnlyList<ProjectId>? projectIds = null,
        SolutionOptionSet? options = null,
        IReadOnlyList<AnalyzerReference>? analyzerReferences = null,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions>? fallbackAnalyzerOptions = null,
        ImmutableDictionary<ProjectId, ProjectState>? idToProjectStateMap = null,
        ProjectDependencyGraph? dependencyGraph = null)
    {
        solutionAttributes ??= SolutionAttributes;
        projectIds ??= ProjectIds;
        idToProjectStateMap ??= ProjectStates;
        options ??= Options;
        analyzerReferences ??= AnalyzerReferences;
        fallbackAnalyzerOptions ??= FallbackAnalyzerOptions;
        projectCountByLanguage ??= ProjectCountByLanguage;
        dependencyGraph ??= _dependencyGraph;

        var analyzerReferencesEqual = AnalyzerReferences.SequenceEqual(analyzerReferences);

        if (solutionAttributes == SolutionAttributes &&
            projectIds == ProjectIds &&
            options == Options &&
            analyzerReferencesEqual &&
            fallbackAnalyzerOptions == FallbackAnalyzerOptions &&
            projectCountByLanguage == ProjectCountByLanguage &&
            idToProjectStateMap == ProjectStates &&
            dependencyGraph == _dependencyGraph)
        {
            return this;
        }

        return new SolutionState(
            WorkspaceKind,
            WorkspaceVersion,
            Services,
            solutionAttributes,
            projectIds,
            options,
            analyzerReferences,
            fallbackAnalyzerOptions,
            projectCountByLanguage,
            idToProjectStateMap,
            dependencyGraph,
            analyzerReferencesEqual ? _lazyAnalyzers : null);
    }

    /// <summary>
    /// Updates the solution with specified workspace kind, workspace version and services.
    /// This implicitly also changes the value of <see cref="Solution.Workspace"/> for this solution,
    /// since that is extracted from <see cref="SolutionServices"/> for backwards compatibility.
    /// </summary>
    public SolutionState WithNewWorkspace(
        string? workspaceKind,
        int workspaceVersion,
        SolutionServices services)
    {
        if (workspaceKind == WorkspaceKind &&
            workspaceVersion == WorkspaceVersion &&
            services == Services)
        {
            return this;
        }

        // Note: this will potentially have problems if the workspace services are different, as some services
        // get locked-in by document states and project states when first constructed.
        return new SolutionState(
            workspaceKind,
            workspaceVersion,
            services,
            SolutionAttributes,
            ProjectIds,
            Options,
            AnalyzerReferences,
            FallbackAnalyzerOptions,
            ProjectCountByLanguage,
            ProjectStates,
            _dependencyGraph,
            _lazyAnalyzers);
    }

    /// <summary>
    /// The version of the most recently modified project.
    /// </summary>
    public VersionStamp GetLatestProjectVersion()
    {
        // this may produce a version that is out of sync with the actual Document versions.
        var latestVersion = VersionStamp.Default;
        foreach (var project in this.ProjectStates.Values)
        {
            latestVersion = project.Version.GetNewerVersion(latestVersion);
        }

        return latestVersion;
    }

    /// <summary>
    /// True if the solution contains a project with the specified project ID.
    /// </summary>
    public bool ContainsProject([NotNullWhen(returnValue: true)] ProjectId? projectId)
        => projectId != null && ProjectStates.ContainsKey(projectId);

    /// <summary>
    /// True if the solution contains the document in one of its projects
    /// </summary>
    public bool ContainsDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
    {
        return
            documentId != null &&
            this.ContainsProject(documentId.ProjectId) &&
            this.GetProjectState(documentId.ProjectId)!.DocumentStates.Contains(documentId);
    }

    /// <summary>
    /// True if the solution contains the additional document in one of its projects
    /// </summary>
    public bool ContainsAdditionalDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
    {
        return
            documentId != null &&
            this.ContainsProject(documentId.ProjectId) &&
            this.GetProjectState(documentId.ProjectId)!.AdditionalDocumentStates.Contains(documentId);
    }

    /// <summary>
    /// True if the solution contains the analyzer config document in one of its projects
    /// </summary>
    public bool ContainsAnalyzerConfigDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
    {
        return
            documentId != null &&
            this.ContainsProject(documentId.ProjectId) &&
            this.GetProjectState(documentId.ProjectId)!.AnalyzerConfigDocumentStates.Contains(documentId);
    }

    internal DocumentState GetRequiredDocumentState(DocumentId documentId)
        => GetRequiredProjectState(documentId.ProjectId).DocumentStates.GetRequiredState(documentId);

    private AdditionalDocumentState GetRequiredAdditionalDocumentState(DocumentId documentId)
        => GetRequiredProjectState(documentId.ProjectId).AdditionalDocumentStates.GetRequiredState(documentId);

    private AnalyzerConfigDocumentState GetRequiredAnalyzerConfigDocumentState(DocumentId documentId)
        => GetRequiredProjectState(documentId.ProjectId).AnalyzerConfigDocumentStates.GetRequiredState(documentId);

    public ProjectState? GetProjectState(ProjectId projectId)
        => ProjectStates.TryGetValue(projectId, out var state) ? state : null;

    public ProjectState GetRequiredProjectState(ProjectId projectId)
    {
        var result = GetProjectState(projectId);
        Contract.ThrowIfNull(result);
        return result;
    }

    /// <summary>
    /// Create a new solution instance that includes projects with the specified project information.
    /// </summary>
    public SolutionState AddProjects(ArrayBuilder<ProjectInfo> projectInfos)
    {
        Contract.ThrowIfTrue(projectInfos.HasDuplicates(static p => p.Id), "Duplicate ProjectId provided");

        if (projectInfos.Count == 0)
            return this;

        var langaugeCountDeltas = new TemporaryArray<(string language, int count)>();

        using var _ = ArrayBuilder<ProjectState>.GetInstance(projectInfos.Count, out var projectStates);
        foreach (var projectInfo in projectInfos)
            projectStates.Add(CreateProjectState(projectInfo));

        return AddProjects(projectStates);

        ProjectState CreateProjectState(ProjectInfo projectInfo)
        {
            if (projectInfo == null)
                throw new ArgumentNullException(nameof(projectInfo));

            var projectId = projectInfo.Id;

            var language = projectInfo.Language;
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            var displayName = projectInfo.Name;
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));

            CheckNotContainsProject(projectId);

            var languageServices = Services.GetLanguageServices(language);
            if (languageServices == null)
                throw new ArgumentException(string.Format(WorkspacesResources.The_language_0_is_not_supported, language));

            if (!FallbackAnalyzerOptions.TryGetValue(language, out var fallbackAnalyzerOptions))
            {
                fallbackAnalyzerOptions = StructuredAnalyzerConfigOptions.Empty;
            }

            AddLanguageCountDelta(ref langaugeCountDeltas, language, amount: +1);

            var newProject = new ProjectState(languageServices, projectInfo, fallbackAnalyzerOptions);
            return newProject;
        }

        SolutionState AddProjects(ArrayBuilder<ProjectState> projectStates)
        {
            // changed project list so, increment version.
            var newSolutionAttributes = SolutionAttributes.With(version: Version.GetNewerVersion());

            using var _1 = ArrayBuilder<ProjectId>.GetInstance(ProjectIds.Count + projectStates.Count, out var newProjectIdsBuilder);
            using var _2 = PooledHashSet<ProjectId>.GetInstance(out var addedProjectIds);
            var newStateMapBuilder = ProjectStates.ToBuilder();

            newProjectIdsBuilder.AddRange(ProjectIds);

            foreach (var projectState in projectStates)
            {
                addedProjectIds.Add(projectState.Id);
                newProjectIdsBuilder.Add(projectState.Id);
                newStateMapBuilder.Add(projectState.Id, projectState);
            }

            var newProjectIds = newProjectIdsBuilder.ToBoxedImmutableArray();
            var newStateMap = newStateMapBuilder.ToImmutable();

            // TODO: it would be nice to update these graphs without so much forking.
            var newDependencyGraph = _dependencyGraph;
            foreach (var projectState in projectStates)
            {
                var projectId = projectState.Id;
                newDependencyGraph = newDependencyGraph
                    .WithAdditionalProject(projectId)
                    .WithAdditionalProjectReferences(projectId, projectState.ProjectReferences);
            }

            // It's possible that another project already in newStateMap has a reference to this project that we're adding,
            // since we allow dangling references like that. If so, we'll need to link those in too.
            foreach (var (projectId, newState) in newStateMap)
            {
                foreach (var projectReference in newState.ProjectReferences)
                {
                    if (addedProjectIds.Contains(projectReference.ProjectId))
                        newDependencyGraph = newDependencyGraph.WithAdditionalProjectReferences(projectId, [projectReference]);
                }
            }

            return Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                projectCountByLanguage: AddLanguageCounts(ProjectCountByLanguage, langaugeCountDeltas),
                dependencyGraph: newDependencyGraph);
        }
    }

    /// <summary>
    /// Create a new solution instance without the projects specified.
    /// </summary>
    public SolutionState RemoveProjects(ArrayBuilder<ProjectId> projectIds)
    {
        Contract.ThrowIfTrue(projectIds.HasDuplicates(), "Duplicate ProjectId provided");

        if (projectIds.Count == 0)
            return this;

        foreach (var projectId in projectIds)
            CheckContainsProject(projectId);

        // changed project list so, increment version.
        var newSolutionAttributes = SolutionAttributes.With(version: this.Version.GetNewerVersion());

        using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectIdsSet);
        projectIdsSet.AddRange(projectIds);

        var newProjectIds = ProjectIds.Where(p => !projectIdsSet.Contains(p)).ToBoxedImmutableArray();

        var newStateMapBuilder = ProjectStates.ToBuilder();
        foreach (var projectId in projectIds)
            newStateMapBuilder.Remove(projectId);
        var newStateMap = newStateMapBuilder.ToImmutable();

        // Note: it would be nice to not cause N forks of the dependency graph here.
        var newDependencyGraph = _dependencyGraph;
        foreach (var projectId in projectIds)
            newDependencyGraph = newDependencyGraph.WithProjectRemoved(projectId);

        var languageCountDeltas = new TemporaryArray<(string language, int count)>();
        foreach (var projectId in projectIds)
        {
            AddLanguageCountDelta(ref languageCountDeltas, ProjectStates[projectId].Language, amount: -1);
        }

        return this.Branch(
            solutionAttributes: newSolutionAttributes,
            projectIds: newProjectIds,
            idToProjectStateMap: newStateMap,
            projectCountByLanguage: AddLanguageCounts(ProjectCountByLanguage, languageCountDeltas),
            dependencyGraph: newDependencyGraph);
    }

    private static void AddLanguageCountDelta(ref TemporaryArray<(string language, int count)> languageCountDeltas, string language, int amount)
    {
        Contract.ThrowIfFalse(amount is -1 or +1);

        var index = languageCountDeltas.IndexOf(static (c, language) => c.language == language, language);
        if (index < 0)
        {
            languageCountDeltas.Add((language, amount));
        }
        else
        {
            languageCountDeltas[index] = (language, languageCountDeltas[index].count + amount);
        }
    }

    private static ImmutableDictionary<string, int> AddLanguageCounts(ImmutableDictionary<string, int> projectCountByLanguage, in TemporaryArray<(string language, int count)> languageCountDeltas)
    {
        foreach (var (language, delta) in languageCountDeltas)
        {
            if (!projectCountByLanguage.TryGetValue(language, out var currentCount))
            {
                currentCount = 0;
            }

            var newCount = currentCount + delta;
            if (newCount > 0)
            {
                projectCountByLanguage = projectCountByLanguage.SetItem(language, newCount);
            }
            else
            {
                Contract.ThrowIfFalse(newCount == 0);
                projectCountByLanguage = projectCountByLanguage.Remove(language);
            }
        }

        return projectCountByLanguage;
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the new
    /// assembly name.
    /// </summary>
    public StateChange WithProjectAssemblyName(ProjectId projectId, string assemblyName)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithAssemblyName(assemblyName);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the output file path.
    /// </summary>
    public StateChange WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithOutputFilePath(outputFilePath);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the output file path.
    /// </summary>
    public StateChange WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithOutputRefFilePath(outputRefFilePath);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the compiler output file path.
    /// </summary>
    public StateChange WithProjectCompilationOutputInfo(ProjectId projectId, in CompilationOutputInfo info)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithCompilationOutputInfo(info);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the default namespace.
    /// </summary>
    public StateChange WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithDefaultNamespace(defaultNamespace);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the name.
    /// </summary>
    public StateChange WithProjectChecksumAlgorithm(ProjectId projectId, SourceHashAlgorithm checksumAlgorithm)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithChecksumAlgorithm(checksumAlgorithm);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the name.
    /// </summary>
    public StateChange WithProjectName(ProjectId projectId, string name)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithName(name);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the project file path.
    /// </summary>
    public StateChange WithProjectFilePath(ProjectId projectId, string? filePath)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithFilePath(filePath);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified compilation options.
    /// </summary>
    public StateChange WithProjectCompilationOptions(ProjectId projectId, CompilationOptions options)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithCompilationOptions(options);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified parse options.
    /// </summary>
    public StateChange WithProjectParseOptions(ProjectId projectId, ParseOptions options)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithParseOptions(options);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified hasAllInformation.
    /// </summary>
    public StateChange WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithHasAllInformation(hasAllInformation);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        // fork without any change on compilation.
        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified runAnalyzers.
    /// </summary>
    public StateChange WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithRunAnalyzers(runAnalyzers);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        // fork without any change on compilation.
        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to include
    /// the specified project references.
    /// </summary>
    public StateChange AddProjectReferences(ProjectId projectId, IReadOnlyCollection<ProjectReference> projectReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        if (projectReferences.Count == 0)
        {
            return new(this, oldProject, oldProject);
        }

        var oldReferences = oldProject.ProjectReferences.ToImmutableArray();
        var newReferences = oldReferences.AddRange(projectReferences);

        var newProject = oldProject.WithProjectReferences(newReferences);
        var newDependencyGraph = _dependencyGraph.WithAdditionalProjectReferences(projectId, projectReferences);

        return ForkProject(oldProject, newProject, newDependencyGraph: newDependencyGraph);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to no longer
    /// include the specified project reference.
    /// </summary>
    public StateChange RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var oldReferences = oldProject.ProjectReferences.ToImmutableArray();

        // Note: uses ProjectReference equality to compare references.
        var newReferences = oldReferences.Remove(projectReference);

        if (oldReferences == newReferences)
        {
            return new(this, oldProject, oldProject);
        }

        var newProject = oldProject.WithProjectReferences(newReferences);

        ProjectDependencyGraph newDependencyGraph;
        if (newProject.ContainsReferenceToProject(projectReference.ProjectId) ||
            !ProjectStates.ContainsKey(projectReference.ProjectId))
        {
            // Two cases:
            // 1) The project contained multiple non-equivalent references to the project,
            // and not all of them were removed. The dependency graph doesn't change.
            // Note that there might be two references to the same project, one with
            // extern alias and the other without. These are not considered duplicates.
            // 2) The referenced project is not part of the solution and hence not included
            // in the dependency graph.
            newDependencyGraph = _dependencyGraph;
        }
        else
        {
            newDependencyGraph = _dependencyGraph.WithProjectReferenceRemoved(projectId, projectReference.ProjectId);
        }

        return ForkProject(oldProject, newProject, newDependencyGraph: newDependencyGraph);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to contain
    /// the specified list of project references.
    /// </summary>
    public StateChange WithProjectReferences(ProjectId projectId, IReadOnlyList<ProjectReference> projectReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithProjectReferences(projectReferences);
        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        var newDependencyGraph = _dependencyGraph.WithProjectReferences(projectId, projectReferences);
        return ForkProject(oldProject, newProject, newDependencyGraph: newDependencyGraph);
    }

    /// <summary>
    /// Creates a new solution instance with the project documents in the order by the specified document ids.
    /// The specified document ids must be the same as what is already in the project; no adding or removing is allowed.
    /// </summary>
    public StateChange WithProjectDocumentsOrder(ProjectId projectId, ImmutableList<DocumentId> documentIds)
    {
        var oldProject = GetRequiredProjectState(projectId);

        if (documentIds.Count != oldProject.DocumentStates.Count)
        {
            throw new ArgumentException($"The specified documents do not equal the project document count.", nameof(documentIds));
        }

        foreach (var id in documentIds)
        {
            if (!oldProject.DocumentStates.Contains(id))
            {
                throw new InvalidOperationException($"The document '{id}' does not exist in the project.");
            }
        }

        var newProject = oldProject.UpdateDocumentsOrder(documentIds);

        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to include the
    /// specified metadata references.
    /// </summary>
    public StateChange AddMetadataReferences(ProjectId projectId, IReadOnlyCollection<MetadataReference> metadataReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        if (metadataReferences.Count == 0)
        {
            return new(this, oldProject, oldProject);
        }

        var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
        var newReferences = oldReferences.AddRange(metadataReferences);

        return ForkProject(oldProject, oldProject.WithMetadataReferences(newReferences));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to no longer include
    /// the specified metadata reference.
    /// </summary>
    public StateChange RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
        var newReferences = oldReferences.Remove(metadataReference);
        if (oldReferences == newReferences)
        {
            return new(this, oldProject, oldProject);
        }

        return ForkProject(oldProject, oldProject.WithMetadataReferences(newReferences));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to include only the
    /// specified metadata references.
    /// </summary>
    public StateChange WithProjectMetadataReferences(ProjectId projectId, IReadOnlyList<MetadataReference> metadataReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithMetadataReferences(metadataReferences);
        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to include the
    /// specified analyzer references.
    /// </summary>
    public StateChange AddAnalyzerReferences(ProjectId projectId, ImmutableArray<AnalyzerReference> analyzerReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        if (analyzerReferences.Length == 0)
        {
            return new(this, oldProject, oldProject);
        }

        var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
        var newReferences = oldReferences.AddRange(analyzerReferences);

        return ForkProject(oldProject, oldProject.WithAnalyzerReferences(newReferences));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to no longer include
    /// the specified analyzer reference.
    /// </summary>
    public StateChange RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
        var newReferences = oldReferences.Remove(analyzerReference);
        if (oldReferences == newReferences)
        {
            return new(this, oldProject, oldProject);
        }

        return ForkProject(oldProject, oldProject.WithAnalyzerReferences(newReferences));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to include only the
    /// specified analyzer references.
    /// </summary>
    public StateChange WithProjectAnalyzerReferences(ProjectId projectId, IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        var oldProject = GetRequiredProjectState(projectId);
        var newProject = oldProject.WithAnalyzerReferences(analyzerReferences);
        if (oldProject == newProject)
        {
            return new(this, oldProject, newProject);
        }

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new solution instance with updated analyzer fallback options.
    /// </summary>
    public SolutionState WithFallbackAnalyzerOptions(ImmutableDictionary<string, StructuredAnalyzerConfigOptions> options)
    {
        if (FallbackAnalyzerOptions == options)
        {
            return this;
        }

        var newProjectStatesMap = ProjectStates.ToImmutableDictionary(
            keySelector: static entry => entry.Key,
            elementSelector: entry =>
            {
                // If the new options are specified for the project language we use them,
                // otherwise we clear the options for the project.
                if (!options.TryGetValue(entry.Value.Language, out var languageOptions))
                {
                    languageOptions = StructuredAnalyzerConfigOptions.Empty;
                }

                return entry.Value.WithFallbackAnalyzerOptions(languageOptions);
            });

        return Branch(
            fallbackAnalyzerOptions: options,
            idToProjectStateMap: newProjectStatesMap);
    }

    /// <summary>
    /// Creates a new solution instance with an attribute of the document updated, if its value has changed.
    /// </summary>
    public StateChange WithDocumentAttributes<TArg>(
        DocumentId documentId,
        TArg arg,
        Func<DocumentInfo.DocumentAttributes, TArg, DocumentInfo.DocumentAttributes> updateAttributes)
    {
        var oldDocument = GetRequiredDocumentState(documentId);

        var newDocument = oldDocument.WithAttributes(updateAttributes(oldDocument.Attributes, arg));
        if (ReferenceEquals(oldDocument, newDocument))
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateDocumentState(newDocument);
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the text
    /// specified.
    /// </summary>
    public StateChange WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredDocumentState(documentId);
        if (oldDocument.TryGetText(out var oldText) && text == oldText)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateDocumentState(oldDocument.UpdateText(text, mode));
    }

    public StateChange WithDocumentState(DocumentState newDocument)
    {
        var oldDocument = GetRequiredDocumentState(newDocument.Id);
        if (oldDocument == newDocument)
        {
            var oldProject = GetRequiredProjectState(newDocument.Id.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateDocumentState(newDocument);
    }

    /// <summary>
    /// Creates a new solution instance with the additional document specified updated to have the text
    /// specified.
    /// </summary>
    public StateChange WithAdditionalDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredAdditionalDocumentState(documentId);
        if (oldDocument.TryGetText(out var oldText) && text == oldText)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateAdditionalDocumentState(oldDocument.UpdateText(text, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the text
    /// specified.
    /// </summary>
    public StateChange WithAnalyzerConfigDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
        if (oldDocument.TryGetText(out var oldText) && text == oldText)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(text, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the text
    /// and version specified.
    /// </summary>
    public StateChange WithDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredDocumentState(documentId);
        if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateDocumentState(oldDocument.UpdateText(textAndVersion, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the additional document specified updated to have the text
    /// and version specified.
    /// </summary>
    public StateChange WithAdditionalDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredAdditionalDocumentState(documentId);
        if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateAdditionalDocumentState(oldDocument.UpdateText(textAndVersion, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the analyzer config document specified updated to have the text
    /// and version specified.
    /// </summary>
    public StateChange WithAnalyzerConfigDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
    {
        var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
        if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(textAndVersion, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the source
    /// code kind specified.
    /// </summary>
    public StateChange WithDocumentSourceCodeKind(DocumentId documentId, SourceCodeKind sourceCodeKind)
    {
        var oldDocument = GetRequiredDocumentState(documentId);
        if (oldDocument.SourceCodeKind == sourceCodeKind)
        {
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            return new(this, oldProject, oldProject);
        }

        return UpdateDocumentState(oldDocument.UpdateSourceCodeKind(sourceCodeKind));
    }

    public StateChange UpdateDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var oldDocument = GetRequiredDocumentState(documentId);

        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateDocumentState(oldDocument.UpdateText(loader, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the additional document specified updated to have the text
    /// supplied by the text loader.
    /// </summary>
    public StateChange UpdateAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var oldDocument = GetRequiredAdditionalDocumentState(documentId);

        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateAdditionalDocumentState(oldDocument.UpdateText(loader, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the analyzer config document specified updated to have the text
    /// supplied by the text loader.
    /// </summary>
    public StateChange UpdateAnalyzerConfigDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);

        // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
        // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
        return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(loader, mode));
    }

    private StateChange UpdateDocumentState(DocumentState newDocument)
    {
        var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
        var newProject = oldProject.UpdateDocument(newDocument);

        // This method shouldn't have been called if the document has not changed.
        Debug.Assert(oldProject != newProject);

        return ForkProject(
            oldProject,
            newProject);
    }

    private StateChange UpdateAdditionalDocumentState(AdditionalDocumentState newDocument)
    {
        var oldProject = GetRequiredProjectState(newDocument.Id.ProjectId);
        var newProject = oldProject.UpdateAdditionalDocument(newDocument);

        // This method shouldn't have been called if the document has not changed.
        Debug.Assert(oldProject != newProject);

        return ForkProject(oldProject, newProject);
    }

    private StateChange UpdateAnalyzerConfigDocumentState(AnalyzerConfigDocumentState newDocument)
    {
        var oldProject = GetRequiredProjectState(newDocument.Id.ProjectId);
        var newProject = oldProject.UpdateAnalyzerConfigDocument(newDocument);

        // This method shouldn't have been called if the document has not changed.
        Debug.Assert(oldProject != newProject);

        return ForkProject(oldProject, newProject);
    }

    /// <summary>
    /// Creates a new snapshot with an updated project and an action that will produce a new
    /// compilation matching the new project out of an old compilation. All dependent projects
    /// are fixed-up if the change to the new project affects its public metadata, and old
    /// dependent compilations are forgotten.
    /// </summary>
    public StateChange ForkProject(
        ProjectState oldProjectState,
        ProjectState newProjectState,
        ProjectDependencyGraph? newDependencyGraph = null)
    {
        var projectId = newProjectState.Id;

        Contract.ThrowIfFalse(ProjectStates.ContainsKey(projectId));
        var newStateMap = ProjectStates.SetItem(projectId, newProjectState);

        newDependencyGraph ??= _dependencyGraph;

        var newSolutionState = this.Branch(
            idToProjectStateMap: newStateMap,
            dependencyGraph: newDependencyGraph);

        return new(newSolutionState, oldProjectState, newProjectState);
    }

    /// <summary>
    /// Gets the set of <see cref="DocumentId"/>s in this <see cref="Solution"/> with a
    /// <see cref="TextDocument.FilePath"/> that matches the given file path.
    /// </summary>
    public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];

        return ImmutableInterlocked.GetOrAdd(
            ref _lazyFilePathToRelatedDocumentIds,
            filePath,
            static (filePath, @this) => ComputeDocumentIdsWithFilePath(@this, filePath),
            this);

        static ImmutableArray<DocumentId> ComputeDocumentIdsWithFilePath(SolutionState @this, string filePath)
        {
            using var result = TemporaryArray<DocumentId>.Empty;
            foreach (var (projectId, projectState) in @this.ProjectStates)
                projectState.AddDocumentIdsWithFilePath(ref result.AsRef(), filePath);

            return result.ToImmutableAndClear();
        }
    }

    public static ProjectDependencyGraph CreateDependencyGraph(
        IReadOnlyList<ProjectId> projectIds,
        ImmutableDictionary<ProjectId, ProjectState> projectStates)
    {
        var map = projectStates.Values.Select(state => KeyValuePairUtil.Create(
                state.Id,
                state.ProjectReferences.Where(pr => projectStates.ContainsKey(pr.ProjectId)).Select(pr => pr.ProjectId).ToImmutableHashSet()))
                .ToImmutableDictionary();

        return new ProjectDependencyGraph([.. projectIds], map);
    }

    public SolutionState WithOptions(SolutionOptionSet options)
        => Branch(options: options);

    public SolutionState AddAnalyzerReferences(IReadOnlyCollection<AnalyzerReference> analyzerReferences)
    {
        if (analyzerReferences.Count == 0)
        {
            return this;
        }

        var oldReferences = AnalyzerReferences.ToImmutableArray();
        var newReferences = oldReferences.AddRange(analyzerReferences);
        return Branch(analyzerReferences: newReferences);
    }

    public SolutionState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
    {
        var oldReferences = AnalyzerReferences.ToImmutableArray();
        var newReferences = oldReferences.Remove(analyzerReference);
        if (oldReferences == newReferences)
        {
            return this;
        }

        return Branch(analyzerReferences: newReferences);
    }

    public SolutionState WithAnalyzerReferences(IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        if (analyzerReferences == AnalyzerReferences)
        {
            return this;
        }

        return Branch(analyzerReferences: analyzerReferences);
    }

    public DocumentId? GetFirstRelatedDocumentId(DocumentId documentId, ProjectId? relatedProjectIdHint)
    {
        Contract.ThrowIfTrue(documentId.ProjectId == relatedProjectIdHint);

        var projectState = this.GetProjectState(documentId.ProjectId);
        if (projectState is null)
            return null;

        var documentState = projectState.DocumentStates.GetState(documentId);
        if (documentState is null)
            return null;

        var filePath = documentState.FilePath;
        if (string.IsNullOrEmpty(filePath))
            return null;

        // Do a quick check if the full info for that path has already been computed and cached.
        var fileMap = _lazyFilePathToRelatedDocumentIds;
        if (fileMap != null && fileMap.TryGetValue(filePath, out var relatedDocumentIds))
        {
            foreach (var relatedDocumentId in relatedDocumentIds)
            {
                if (relatedDocumentId != documentId)
                    return relatedDocumentId;
            }

            return null;
        }

        var relatedProject = relatedProjectIdHint is null ? null : this.ProjectStates[relatedProjectIdHint];
        Contract.ThrowIfTrue(relatedProject == projectState);
        if (relatedProject != null)
        {
            var siblingDocumentId = relatedProject.GetFirstDocumentIdWithFilePath(filePath);
            if (siblingDocumentId is not null)
                return siblingDocumentId;
        }

        // Wasn't in cache, do the linear search.
        foreach (var (_, siblingProjectState) in this.ProjectStates)
        {
            // Don't want to search the same project that document already came from, or from the related-project we had a hint for.
            if (siblingProjectState == projectState || siblingProjectState == relatedProject)
                continue;

            var siblingDocumentId = siblingProjectState.GetFirstDocumentIdWithFilePath(filePath);
            if (siblingDocumentId is not null)
                return siblingDocumentId;
        }

        return null;
    }

    public ImmutableArray<DocumentId> GetRelatedDocumentIds(DocumentId documentId)
    {
        var projectState = this.GetProjectState(documentId.ProjectId);
        if (projectState == null)
        {
            // this document no longer exist
            return [];
        }

        var documentState = projectState.DocumentStates.GetState(documentId);
        if (documentState == null)
        {
            // this document no longer exist
            return [];
        }

        var filePath = documentState.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            // this document can't have any related document. only related document is itself.
            return [documentId];
        }

        var documentIds = GetDocumentIdsWithFilePath(filePath);
        return documentIds.WhereAsArray(
            static (documentId, args) =>
            {
                var projectState = args.solution.GetProjectState(documentId.ProjectId);
                if (projectState == null)
                {
                    // this document no longer exist
                    // I'm adding this ReportAndCatch to see if this does happen in the wild; it's not clear to me under what scenario that could happen since all the IDs of all document types
                    // should be removed when a project is removed.
                    FatalError.ReportAndCatch(new Exception("GetDocumentIdsWithFilePath returned a document in a project that does not exist."));
                    return false;
                }

                if (projectState.ProjectInfo.Language != args.Language)
                    return false;

                // GetDocumentIdsWithFilePath may return DocumentIds for other types of documents (like additional files), so filter to normal documents
                return projectState.DocumentStates.Contains(documentId);
            },
            (solution: this, projectState.Language));
    }

    /// <summary>
    /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
    /// </summary>
    public ProjectDependencyGraph GetProjectDependencyGraph()
        => _dependencyGraph;

    private void CheckNotContainsProject(ProjectId projectId)
    {
        if (this.ContainsProject(projectId))
        {
            throw new InvalidOperationException(WorkspacesResources.The_solution_already_contains_the_specified_project);
        }
    }

    internal void CheckContainsProject(ProjectId projectId)
    {
        if (!this.ContainsProject(projectId))
        {
            throw new InvalidOperationException(WorkspacesResources.The_solution_does_not_contain_the_specified_project);
        }
    }

    internal bool ContainsProjectReference(ProjectId projectId, ProjectReference projectReference)
        => GetRequiredProjectState(projectId).ProjectReferences.Contains(projectReference);

    internal bool ContainsMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        => GetRequiredProjectState(projectId).MetadataReferences.Contains(metadataReference);

    internal bool ContainsAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        => GetRequiredProjectState(projectId).AnalyzerReferences.Contains(analyzerReference);

    internal bool ContainsTransitiveReference(ProjectId fromProjectId, ProjectId toProjectId)
        => _dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(fromProjectId).Contains(toProjectId);
}
