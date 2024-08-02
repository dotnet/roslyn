// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    /// <summary>
    /// Symbols need to be either <see cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<ISymbol, ProjectId> s_assemblyOrModuleSymbolToProjectMap = new();

    /// <summary>
    /// Green version of the information about this Solution instance.  Responsible for non-semantic information
    /// about the solution structure.  Specifically, the set of green <see cref="ProjectState"/>s, with all their
    /// green <see cref="DocumentState"/>s.  Contains the attributes, options and relationships between projects.
    /// Effectively, everything specified in a project file.  Does not contain anything related to <see
    /// cref="Compilation"/>s or semantics.
    /// </summary>
    public SolutionState SolutionState { get; }

    public bool PartialSemanticsEnabled { get; }
    public TextDocumentStates<SourceGeneratedDocumentState>? FrozenSourceGeneratedDocumentStates { get; }

    // Values for all these are created on demand.
    private ImmutableSegmentedDictionary<ProjectId, ICompilationTracker> _projectIdToTrackerMap;

    /// <summary>
    /// Map from each project to the <see cref="SourceGeneratorExecutionVersion"/> it is currently at. Loosely, the
    /// execution version allows us to have the generated documents for a project get fixed at some point in the past
    /// when they were generated, up until events happen in the host that cause a need for them to be brought up to
    /// date.  This is ambient, compilation-level, information about our projects, which is why it is stored at this
    /// compilation-state level.  When syncing to our OOP process, this information is included, allowing the oop side
    /// to move its own generators forward when a host changes these versions.
    /// </summary>
    /// <remarks>
    /// Contains information for all projects, even non-C#/VB ones.  Though this will have no meaning for those project
    /// types.
    /// </remarks>
    private readonly SourceGeneratorExecutionVersionMap _sourceGeneratorExecutionVersionMap;

    /// <summary>
    /// Cache we use to map between unrooted symbols (i.e. assembly, module and dynamic symbols) and the project
    /// they came from.  That way if we are asked about many symbols from the same assembly/module we can answer the
    /// question quickly after computing for the first one.  Created on demand.
    /// </summary>
    private ConditionalWeakTable<ISymbol, OriginatingProjectInfo?>? _unrootedSymbolToProjectId;
    private static readonly Func<ConditionalWeakTable<ISymbol, OriginatingProjectInfo?>> s_createTable = () => new ConditionalWeakTable<ISymbol, OriginatingProjectInfo?>();

    private readonly AsyncLazy<SolutionCompilationState> _cachedFrozenSnapshot;

    private SolutionCompilationState(
        SolutionState solution,
        bool partialSemanticsEnabled,
        ImmutableSegmentedDictionary<ProjectId, ICompilationTracker> projectIdToTrackerMap,
        SourceGeneratorExecutionVersionMap sourceGeneratorExecutionVersionMap,
        TextDocumentStates<SourceGeneratedDocumentState>? frozenSourceGeneratedDocumentStates,
        AsyncLazy<SolutionCompilationState>? cachedFrozenSnapshot = null)
    {
        SolutionState = solution;
        PartialSemanticsEnabled = partialSemanticsEnabled;
        _projectIdToTrackerMap = projectIdToTrackerMap;
        _sourceGeneratorExecutionVersionMap = sourceGeneratorExecutionVersionMap;
        FrozenSourceGeneratedDocumentStates = frozenSourceGeneratedDocumentStates;

        // when solution state is changed, we recalculate its checksum
        _lazyChecksums = AsyncLazy.Create(static async (self, cancellationToken) =>
        {
            var (checksums, projectCone) = await self.ComputeChecksumsAsync(projectId: null, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(projectCone != null);
            return checksums;
        }, arg: this);
        _cachedFrozenSnapshot = cachedFrozenSnapshot ??
            AsyncLazy.Create(synchronousComputeFunction: static (self, c) =>
                self.ComputeFrozenSnapshot(c),
                arg: this);

        CheckInvariants();
    }

    public SolutionCompilationState(
        SolutionState solution,
        bool partialSemanticsEnabled)
        : this(
              solution,
              partialSemanticsEnabled,
              projectIdToTrackerMap: ImmutableSegmentedDictionary<ProjectId, ICompilationTracker>.Empty,
              sourceGeneratorExecutionVersionMap: SourceGeneratorExecutionVersionMap.Empty,
              frozenSourceGeneratedDocumentStates: null)
    {
    }

    public SolutionServices Services => this.SolutionState.Services;

    // Only run this in debug builds; even the .Any() call across all projects can be expensive when there's a lot of them.
    [Conditional("DEBUG")]
    private void CheckInvariants()
    {
        // An id shouldn't point at a tracker for a different project.
        Contract.ThrowIfTrue(_projectIdToTrackerMap.Any(kvp => kvp.Key != kvp.Value.ProjectState.Id));

        // Solution and SG version maps must correspond to the same set of projects.
        Contract.ThrowIfFalse(this.SolutionState.ProjectStates
            .Select(kvp => kvp.Key)
            .SetEquals(_sourceGeneratorExecutionVersionMap.Map.Keys));
    }

    private SolutionCompilationState Branch(
        SolutionState newSolutionState,
        ImmutableSegmentedDictionary<ProjectId, ICompilationTracker>? projectIdToTrackerMap = null,
        SourceGeneratorExecutionVersionMap? sourceGeneratorExecutionVersionMap = null,
        Optional<TextDocumentStates<SourceGeneratedDocumentState>?> frozenSourceGeneratedDocumentStates = default,
        AsyncLazy<SolutionCompilationState>? cachedFrozenSnapshot = null)
    {
        projectIdToTrackerMap ??= _projectIdToTrackerMap;
        sourceGeneratorExecutionVersionMap ??= _sourceGeneratorExecutionVersionMap;
        var newFrozenSourceGeneratedDocumentStates = frozenSourceGeneratedDocumentStates.HasValue ? frozenSourceGeneratedDocumentStates.Value : FrozenSourceGeneratedDocumentStates;

        if (newSolutionState == this.SolutionState &&
            projectIdToTrackerMap == _projectIdToTrackerMap &&
            sourceGeneratorExecutionVersionMap == _sourceGeneratorExecutionVersionMap &&
            Equals(newFrozenSourceGeneratedDocumentStates, FrozenSourceGeneratedDocumentStates))
        {
            return this;
        }

        return new SolutionCompilationState(
            newSolutionState,
            PartialSemanticsEnabled,
            projectIdToTrackerMap.Value,
            sourceGeneratorExecutionVersionMap,
            newFrozenSourceGeneratedDocumentStates,
            cachedFrozenSnapshot);
    }

    /// <inheritdoc cref="SolutionState.ForkProject"/>
    private SolutionCompilationState ForkProject(
        StateChange stateChange,
        Func<StateChange, TranslationAction?>? translate,
        bool forkTracker)
    {
        return ForkProject(
            stateChange,
            translate: static (stateChange, translate) => translate?.Invoke(stateChange),
            forkTracker,
            arg: translate);
    }

    /// <inheritdoc cref="SolutionState.ForkProject"/>
    private SolutionCompilationState ForkProject<TArg>(
        StateChange stateChange,
        Func<StateChange, TArg, TranslationAction?> translate,
        bool forkTracker,
        TArg arg)
    {
        // If the solution didn't actually change, there's no need to change us.
        if (stateChange.NewSolutionState == this.SolutionState)
            return this;

        return ForceForkProject(stateChange, translate.Invoke(stateChange, arg), forkTracker);
    }

    /// <summary>
    /// Same as <see cref="ForkProject(StateChange, Func{StateChange, TranslationAction?}?,
    /// bool)"/> except that it will still fork even if newSolutionState is unchanged from <see cref="SolutionState"/>.
    /// </summary>
    private SolutionCompilationState ForceForkProject(
        StateChange stateChange,
        TranslationAction? translate,
        bool forkTracker)
    {
        var newSolutionState = stateChange.NewSolutionState;
        var newProjectState = stateChange.NewProjectState;
        var projectId = newProjectState.Id;

        var newDependencyGraph = newSolutionState.GetProjectDependencyGraph();
        var newTrackerMap = CreateCompilationTrackerMap(
            projectId,
            newDependencyGraph,
            static (trackerMap, arg) =>
            {
                // If we have a tracker for this project, then fork it as well (along with the
                // translation action and store it in the tracker map.
                if (trackerMap.TryGetValue(arg.projectId, out var tracker))
                {
                    if (!arg.forkTracker)
                        trackerMap.Remove(arg.projectId);
                    else
                        trackerMap[arg.projectId] = tracker.Fork(arg.newProjectState, arg.translate);
                }
            },
            (translate, forkTracker, projectId, newProjectState),
            skipEmptyCallback: true);

        return this.Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap);
    }

    /// <summary>
    /// Creates a mapping of <see cref="ProjectId"/> to <see cref="ICompilationTracker"/>
    /// </summary>
    /// <param name="changedProjectId">Changed project id</param>
    /// <param name="dependencyGraph">Dependency graph</param>
    /// <param name="modifyNewTrackerInfo">Callback to modify tracker information. Return value indicates whether the collection was modified.</param>
    /// <param name="arg">Data to pass to <paramref name="modifyNewTrackerInfo"/></param>
    private ImmutableSegmentedDictionary<ProjectId, ICompilationTracker> CreateCompilationTrackerMap<TArg>(
        ProjectId changedProjectId,
        ProjectDependencyGraph dependencyGraph,
        Action<ImmutableSegmentedDictionary<ProjectId, ICompilationTracker>.Builder, TArg> modifyNewTrackerInfo,
        TArg arg,
        bool skipEmptyCallback)
    {
        return CreateCompilationTrackerMap(CanReuse, (changedProjectId, dependencyGraph), modifyNewTrackerInfo, arg, skipEmptyCallback);

        // Returns true if 'tracker' can be reused for project 'id'
        static bool CanReuse(ProjectId id, (ProjectId changedProjectId, ProjectDependencyGraph dependencyGraph) arg)
        {
            if (id == arg.changedProjectId)
            {
                return true;
            }

            return !arg.dependencyGraph.DoesProjectTransitivelyDependOnProject(id, arg.changedProjectId);
        }
    }

    /// <summary>
    /// Creates a mapping of <see cref="ProjectId"/> to <see cref="ICompilationTracker"/>
    /// </summary>
    /// <param name="changedProjectIds">Changed project ids</param>
    /// <param name="dependencyGraph">Dependency graph</param>
    /// <param name="modifyNewTrackerInfo">Callback to modify tracker information. Return value indicates whether the collection was modified.</param>
    /// <param name="arg">Data to pass to <paramref name="modifyNewTrackerInfo"/></param>
    private ImmutableSegmentedDictionary<ProjectId, ICompilationTracker> CreateCompilationTrackerMap<TArg>(
        ImmutableArray<ProjectId> changedProjectIds,
        ProjectDependencyGraph dependencyGraph,
        Action<ImmutableSegmentedDictionary<ProjectId, ICompilationTracker>.Builder, TArg> modifyNewTrackerInfo,
        TArg arg,
        bool skipEmptyCallback)
    {
        return CreateCompilationTrackerMap(CanReuse, (changedProjectIds, dependencyGraph), modifyNewTrackerInfo, arg, skipEmptyCallback);

        // Returns true if 'tracker' can be reused for project 'id'
        static bool CanReuse(ProjectId id, (ImmutableArray<ProjectId> changedProjectIds, ProjectDependencyGraph dependencyGraph) arg)
        {
            if (arg.changedProjectIds.Contains(id))
                return true;

            foreach (var changedProjectId in arg.changedProjectIds)
            {
                if (arg.dependencyGraph.DoesProjectTransitivelyDependOnProject(id, changedProjectId))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Creates a mapping of <see cref="ProjectId"/> to <see cref="ICompilationTracker"/>
    /// </summary>
    /// <param name="canReuse">Callback to determine whether an item can be reused</param>
    /// <param name="argCanReuse">Data to pass to <paramref name="argCanReuse"/></param>
    /// <param name="modifyNewTrackerInfo">Callback to modify tracker information. Return value indicates whether the collection was modified.</param>
    /// <param name="argModifyNewTrackerInfo">Data to pass to <paramref name="modifyNewTrackerInfo"/></param>
    private ImmutableSegmentedDictionary<ProjectId, ICompilationTracker> CreateCompilationTrackerMap<TArgCanReuse, TArgModifyNewTrackerInfo>(
        Func<ProjectId, TArgCanReuse, bool> canReuse,
        TArgCanReuse argCanReuse,
        Action<ImmutableSegmentedDictionary<ProjectId, ICompilationTracker>.Builder, TArgModifyNewTrackerInfo> modifyNewTrackerInfo,
        TArgModifyNewTrackerInfo argModifyNewTrackerInfo,
        bool skipEmptyCallback)
    {
        // Keep _projectIdToTrackerMap in a local as it can change during the execution of this method
        var projectIdToTrackerMap = _projectIdToTrackerMap;

        // Avoid allocating the builder if the map is empty and the callback doesn't need
        // to be called with empty collections.
        if (projectIdToTrackerMap.Count == 0 && skipEmptyCallback)
            return projectIdToTrackerMap;

        var projectIdToTrackerMapBuilder = projectIdToTrackerMap.ToBuilder();
        foreach (var (id, tracker) in projectIdToTrackerMap)
        {
            if (!canReuse(id, argCanReuse))
            {
                var localTracker = tracker.Fork(tracker.ProjectState, translate: null);

                projectIdToTrackerMapBuilder[id] = localTracker;
            }
        }

        modifyNewTrackerInfo(projectIdToTrackerMapBuilder, argModifyNewTrackerInfo);

        return projectIdToTrackerMapBuilder.ToImmutable();
    }

    public SourceGeneratorExecutionVersionMap SourceGeneratorExecutionVersionMap => _sourceGeneratorExecutionVersionMap;

    /// <inheritdoc cref="SolutionState.AddProjects(ArrayBuilder{ProjectInfo})"/>
    public SolutionCompilationState AddProjects(ArrayBuilder<ProjectInfo> projectInfos)
    {
        if (projectInfos.Count == 0)
            return this;

        var newSolutionState = this.SolutionState.AddProjects(projectInfos);

        // When adding a project, we might add a project that an *existing* project now has a reference to.  That's
        // because we allow existing projects to have 'dangling' project references.  As such, we have to ensure we do
        // not reuse compilation trackers for any of those projects.
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var dependentProjects);
        var newDependencyGraph = newSolutionState.GetProjectDependencyGraph();
        foreach (var projectInfo in projectInfos)
            dependentProjects.AddRange(newDependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectInfo.Id));

        var newTrackerMap = CreateCompilationTrackerMap(
            static (projectId, dependentProjects) => !dependentProjects.Contains(projectId),
            dependentProjects,
            // We don't need to do anything here.  Compilation trackers are created on demand.  So we'll just keep the
            // tracker map as-is, and have the trackers for these new projects be created when needed.
            modifyNewTrackerInfo: static (_, _) => { }, argModifyNewTrackerInfo: default(VoidResult),
            skipEmptyCallback: true);

        // Add the new projects to the source generator execution version map.  Note: it's ok for us to have entries for
        // non-C#/VB projects.  These will have no effect in-proc as we won't have compilation-trackers for these
        // projects.  And, when communicating with the OOP process, we'll filter out these projects before sending them
        // across in SolutionCompilationState.GetFilteredSourceGenerationExecutionMap.
        var versionMapBuilder = _sourceGeneratorExecutionVersionMap.Map.ToBuilder();
        foreach (var projectInfo in projectInfos)
            versionMapBuilder.Add(projectInfo.Id, new());

        var sourceGeneratorExecutionVersionMap = new SourceGeneratorExecutionVersionMap(versionMapBuilder.ToImmutable());
        return Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap,
            sourceGeneratorExecutionVersionMap: sourceGeneratorExecutionVersionMap);
    }

    /// <inheritdoc cref="SolutionState.RemoveProjects"/>
    public SolutionCompilationState RemoveProjects(ArrayBuilder<ProjectId> projectIds)
    {
        if (projectIds.Count == 0)
            return this;

        // Now go and remove the projects from teh solution-state itself.
        var newSolutionState = this.SolutionState.RemoveProjects(projectIds);

        var originalDependencyGraph = this.SolutionState.GetProjectDependencyGraph();
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var dependentProjects);

        // Determine the set of projects that depend on the projects being removed.
        foreach (var projectId in projectIds)
        {
            foreach (var dependentProject in originalDependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectId))
                dependentProjects.Add(dependentProject);
        }

        // Now for each compilation tracker.
        // 1. remove the compilation tracker if we're removing the project.
        // 2. fork teh compilation tracker if it depended on a removed project.
        // 3. do nothing for the rest.
        var newTrackerMap = CreateCompilationTrackerMap(
            // Can reuse the compilation tracker for a project, unless it is some project that had a dependency on one
            // of the projects removed.
            static (projectId, dependentProjects) => !dependentProjects.Contains(projectId),
            dependentProjects,
            static (trackerMap, projectIds) =>
            {
                foreach (var projectId in projectIds)
                    trackerMap.Remove(projectId);
            },
            projectIds,
            skipEmptyCallback: true);

        var versionMapBuilder = _sourceGeneratorExecutionVersionMap.Map.ToBuilder();
        foreach (var projectId in projectIds)
            versionMapBuilder.Remove(projectId);

        return this.Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap,
            sourceGeneratorExecutionVersionMap: new(versionMapBuilder.ToImmutable()));
    }

    /// <inheritdoc cref="SolutionState.WithProjectAssemblyName"/>
    public SolutionCompilationState WithProjectAssemblyName(
        ProjectId projectId, string assemblyName)
    {
        return ForkProject(
            this.SolutionState.WithProjectAssemblyName(projectId, assemblyName),
            static (stateChange, assemblyName) => new TranslationAction.ProjectAssemblyNameAction(
                stateChange.OldProjectState, stateChange.NewProjectState),
            forkTracker: true,
            arg: assemblyName);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputFilePath"/>
    public SolutionCompilationState WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
    {
        return ForkProject(
            this.SolutionState.WithProjectOutputFilePath(projectId, outputFilePath),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputRefFilePath"/>
    public SolutionCompilationState WithProjectOutputRefFilePath(
        ProjectId projectId, string? outputRefFilePath)
    {
        return ForkProject(
            this.SolutionState.WithProjectOutputRefFilePath(projectId, outputRefFilePath),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectCompilationOutputInfo(
        ProjectId projectId, in CompilationOutputInfo info)
    {
        return ForkProject(
            this.SolutionState.WithProjectCompilationOutputInfo(projectId, info),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectDefaultNamespace(
        ProjectId projectId, string? defaultNamespace)
    {
        return ForkProject(
            this.SolutionState.WithProjectDefaultNamespace(projectId, defaultNamespace),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectChecksumAlgorithm"/>
    public SolutionCompilationState WithProjectChecksumAlgorithm(
        ProjectId projectId, SourceHashAlgorithm checksumAlgorithm)
    {
        return ForkProject(
            this.SolutionState.WithProjectChecksumAlgorithm(projectId, checksumAlgorithm),
            static stateChange => new TranslationAction.ReplaceAllSyntaxTreesAction(
                stateChange.OldProjectState, stateChange.NewProjectState, isParseOptionChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectName"/>
    public SolutionCompilationState WithProjectName(
        ProjectId projectId, string name)
    {
        return ForkProject(
            this.SolutionState.WithProjectName(projectId, name),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectFilePath"/>
    public SolutionCompilationState WithProjectFilePath(
        ProjectId projectId, string? filePath)
    {
        return ForkProject(
            this.SolutionState.WithProjectFilePath(projectId, filePath),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOptions"/>
    public SolutionCompilationState WithProjectCompilationOptions(
        ProjectId projectId, CompilationOptions? options)
    {
        return ForkProject(
            this.SolutionState.WithProjectCompilationOptions(projectId, options),
            static stateChange => new TranslationAction.ProjectCompilationOptionsAction(stateChange.OldProjectState, stateChange.NewProjectState),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectParseOptions"/>
    public SolutionCompilationState WithProjectParseOptions(
        ProjectId projectId, ParseOptions? options)
    {
        var stateChange = this.SolutionState.WithProjectParseOptions(projectId, options);

        if (this.PartialSemanticsEnabled)
        {
            // don't fork tracker with queued action since access via partial semantics can become inconsistent (throw).
            // Since changing options is rare event, it is okay to start compilation building from scratch.
            return ForkProject(
                stateChange,
                translate: null,
                forkTracker: false);
        }
        else
        {
            return ForkProject(
                stateChange,
                static stateChange => new TranslationAction.ReplaceAllSyntaxTreesAction(
                    stateChange.OldProjectState, stateChange.NewProjectState, isParseOptionChange: true),
                forkTracker: true);
        }
    }

    /// <inheritdoc cref="SolutionState.WithHasAllInformation"/>
    public SolutionCompilationState WithHasAllInformation(
        ProjectId projectId, bool hasAllInformation)
    {
        return ForkProject(
            this.SolutionState.WithHasAllInformation(projectId, hasAllInformation),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithRunAnalyzers"/>
    public SolutionCompilationState WithRunAnalyzers(
        ProjectId projectId, bool runAnalyzers)
    {
        return ForkProject(
            this.SolutionState.WithRunAnalyzers(projectId, runAnalyzers),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectDocumentsOrder"/>
    public SolutionCompilationState WithProjectDocumentsOrder(
        ProjectId projectId, ImmutableList<DocumentId> documentIds)
    {
        return ForkProject(
            this.SolutionState.WithProjectDocumentsOrder(projectId, documentIds),
            static stateChange => new TranslationAction.ReplaceAllSyntaxTreesAction(
                stateChange.OldProjectState, stateChange.NewProjectState, isParseOptionChange: false),
            forkTracker: true);
    }

    public SolutionCompilationState WithProjectAttributes(ProjectInfo.ProjectAttributes attributes)
    {
        var projectId = attributes.Id;
        var oldProject = SolutionState.GetRequiredProjectState(projectId);

        if (oldProject.ProjectInfo.Attributes.Language != attributes.Language)
        {
            throw new NotSupportedException(WorkspacesResources.Changing_project_language_is_not_supported);
        }

        if (oldProject.ProjectInfo.Attributes.IsSubmission != attributes.IsSubmission)
        {
            throw new NotSupportedException(WorkspacesResources.Changing_project_between_ordinary_and_interactive_submission_is_not_supported);
        }

        return
             WithProjectName(projectId, attributes.Name)
            .WithProjectAssemblyName(projectId, attributes.AssemblyName)
            .WithProjectFilePath(projectId, attributes.FilePath)
            .WithProjectOutputFilePath(projectId, attributes.OutputFilePath)
            .WithProjectOutputRefFilePath(projectId, attributes.OutputRefFilePath)
            .WithProjectCompilationOutputInfo(projectId, attributes.CompilationOutputInfo)
            .WithProjectDefaultNamespace(projectId, attributes.DefaultNamespace)
            .WithHasAllInformation(projectId, attributes.HasAllInformation)
            .WithRunAnalyzers(projectId, attributes.RunAnalyzers)
            .WithProjectChecksumAlgorithm(projectId, attributes.ChecksumAlgorithm);
    }

    public SolutionCompilationState WithProjectInfo(ProjectInfo info)
    {
        var projectId = info.Id;
        var newState = WithProjectAttributes(info.Attributes)
            .WithProjectCompilationOptions(projectId, info.CompilationOptions)
            .WithProjectParseOptions(projectId, info.ParseOptions)
            .WithProjectReferences(projectId, info.ProjectReferences)
            .WithProjectMetadataReferences(projectId, info.MetadataReferences)
            .WithProjectAnalyzerReferences(projectId, info.AnalyzerReferences);

        var oldProjectState = SolutionState.GetRequiredProjectState(projectId);

        // Note: buffers are reused across all calls to UpdateDocuments and cleared after each:
        using var _1 = ArrayBuilder<DocumentInfo>.GetInstance(out var addedDocumentInfos);
        using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var removedDocumentInfos);

        UpdateDocuments<DocumentState>(info.Documents);
        UpdateDocuments<AdditionalDocumentState>(info.AdditionalDocuments);
        UpdateDocuments<AnalyzerConfigDocumentState>(info.AnalyzerConfigDocuments);

        return newState;

        void UpdateDocuments<TDocumentState>(IReadOnlyList<DocumentInfo> newDocumentInfos)
            where TDocumentState : TextDocumentState
        {
            Debug.Assert(addedDocumentInfos.IsEmpty);
            Debug.Assert(removedDocumentInfos.IsEmpty);

            using var _3 = ArrayBuilder<TDocumentState>.GetInstance(out var updatedDocuments);

            var oldDocumentStates = oldProjectState.GetDocumentStates<TDocumentState>();

            foreach (var newDocumentInfo in newDocumentInfos)
            {
                if (oldDocumentStates.TryGetState(newDocumentInfo.Id, out var oldDocumentState))
                {
                    var newDocumentState = (TDocumentState)oldDocumentState.WithDocumentInfo(newDocumentInfo);
                    if (oldDocumentState != newDocumentState)
                    {
                        updatedDocuments.Add(newDocumentState);
                    }
                }
                else
                {
                    addedDocumentInfos.Add(newDocumentInfo);
                }
            }

            if (!oldDocumentStates.Ids.IsEmpty())
            {
                var newDocumentIdSet = newDocumentInfos.Select(static d => d.Id).ToSet();
                foreach (var oldDocumentId in oldDocumentStates.Ids)
                {
                    if (!newDocumentIdSet.Contains(oldDocumentId))
                    {
                        removedDocumentInfos.Add(oldDocumentId);
                    }
                }
            }

            newState = newState
                .WithDocumentStatesOfMultipleProjects<TDocumentState>([(projectId, updatedDocuments.ToImmutableAndClear())], GetUpdateDocumentsTranslationAction)
                .AddDocumentsToMultipleProjects<TDocumentState>(addedDocumentInfos.ToImmutableAndClear())
                .RemoveDocumentsFromSingleProject<TDocumentState>(projectId, removedDocumentInfos.ToImmutableAndClear());
        }
    }

    /// <inheritdoc cref="SolutionState.AddProjectReferences"/>
    public SolutionCompilationState AddProjectReferences(
        ProjectId projectId, IReadOnlyCollection<ProjectReference> projectReferences)
    {
        return ForkProject(
            this.SolutionState.AddProjectReferences(projectId, projectReferences),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveProjectReference"/>
    public SolutionCompilationState RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
    {
        return ForkProject(
            this.SolutionState.RemoveProjectReference(projectId, projectReference),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectReferences"/>
    public SolutionCompilationState WithProjectReferences(
        ProjectId projectId, IReadOnlyList<ProjectReference> projectReferences)
    {
        return ForkProject(
            this.SolutionState.WithProjectReferences(projectId, projectReferences),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.AddMetadataReferences"/>
    public SolutionCompilationState AddMetadataReferences(
        ProjectId projectId, IReadOnlyCollection<MetadataReference> metadataReferences)
    {
        return ForkProject(
            this.SolutionState.AddMetadataReferences(projectId, metadataReferences),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveMetadataReference"/>
    public SolutionCompilationState RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
    {
        return ForkProject(
            this.SolutionState.RemoveMetadataReference(projectId, metadataReference),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectMetadataReferences"/>
    public SolutionCompilationState WithProjectMetadataReferences(
        ProjectId projectId, IReadOnlyList<MetadataReference> metadataReferences)
    {
        return ForkProject(
            this.SolutionState.WithProjectMetadataReferences(projectId, metadataReferences),
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.AddAnalyzerReferences(ProjectId, ImmutableArray{AnalyzerReference})"/>
    public SolutionCompilationState AddAnalyzerReferences(StateChange stateChange, ImmutableArray<AnalyzerReference> analyzerReferences)
    {
        return ForkProject(
            stateChange,
            static (stateChange, analyzerReferences) => new TranslationAction.AddOrRemoveAnalyzerReferencesAction(
                stateChange.OldProjectState, stateChange.NewProjectState, referencesToAdd: analyzerReferences),
            forkTracker: true,
            arg: analyzerReferences);
    }

    public SolutionCompilationState AddAnalyzerReferences(IReadOnlyCollection<AnalyzerReference> analyzerReferences)
    {
        // Note: This is the codepath for adding analyzers from vsixes.  Importantly, we do not ever get SGs added from
        // this codepath, and as such we do not need to update the compilation trackers.  The methods that add SGs all
        // come from entrypoints that are specific to a particular project.
        return Branch(this.SolutionState.AddAnalyzerReferences(analyzerReferences));
    }

    public SolutionCompilationState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
    {
        // Note: This is the codepath for removing analyzers from vsixes.  Importantly, we do not ever get SGs removed
        // from this codepath, and as such we do not need to update the compilation trackers.  The methods that remove
        // SGs all come from entrypoints that are specific to a particular project.
        return Branch(this.SolutionState.RemoveAnalyzerReference(analyzerReference));
    }

    public SolutionCompilationState WithAnalyzerReferences(IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        // Note: This is the codepath for updating analyzers from vsixes.  Importantly, we do not ever get SGs changed
        // from this codepath, and as such we do not need to update the compilation trackers.  The methods that change
        // SGs all come from entrypoints that are specific to a particular project.
        return Branch(this.SolutionState.WithAnalyzerReferences(analyzerReferences));
    }

    /// <inheritdoc cref="SolutionState.RemoveAnalyzerReference(ProjectId, AnalyzerReference)"/>
    public SolutionCompilationState RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        return ForkProject(
            this.SolutionState.RemoveAnalyzerReference(projectId, analyzerReference),
            static (stateChange, analyzerReference) => new TranslationAction.AddOrRemoveAnalyzerReferencesAction(
                stateChange.OldProjectState, stateChange.NewProjectState, referencesToRemove: [analyzerReference]),
            forkTracker: true,
            arg: analyzerReference);
    }

    /// <inheritdoc cref="SolutionState.WithProjectAnalyzerReferences"/>
    public SolutionCompilationState WithProjectAnalyzerReferences(
        ProjectId projectId, IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        return ForkProject(
            this.SolutionState.WithProjectAnalyzerReferences(projectId, analyzerReferences),
            static stateChange =>
            {
                // The .Except() methods here aren't going to terribly cheap, but the assumption is adding or removing
                // just the generators we changed, rather than creating an entire new generator driver from scratch and
                // rerunning all generators, is cheaper in the end. This was written without data backing up that
                // assumption, so if a profile indicates to the contrary, this could be changed.
                //
                // When we're comparing AnalyzerReferences, we'll compare with reference equality; AnalyzerReferences
                // like AnalyzerFileReference may implement their own equality, but that can result in things getting
                // out of sync: two references that are value equal can still have their own generator instances; it's
                // important that as we're adding and removing references that are value equal that we still update with
                // the correct generator instances that are coming from the new reference that is actually held in the
                // project state from above. An alternative approach would be to call oldProject.WithAnalyzerReferences
                // keeping all the references in there that are value equal the same, but this avoids any surprises
                // where other components calling WithAnalyzerReferences might not expect that.

                var addedReferences = stateChange.NewProjectState.AnalyzerReferences.Except<AnalyzerReference>(stateChange.OldProjectState.AnalyzerReferences, ReferenceEqualityComparer.Instance).ToImmutableArray();
                var removedReferences = stateChange.OldProjectState.AnalyzerReferences.Except<AnalyzerReference>(stateChange.NewProjectState.AnalyzerReferences, ReferenceEqualityComparer.Instance).ToImmutableArray();

                return new TranslationAction.AddOrRemoveAnalyzerReferencesAction(
                    stateChange.OldProjectState, stateChange.NewProjectState, referencesToAdd: addedReferences, referencesToRemove: removedReferences);
            },
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentAttributes{TValue}"/>
    public SolutionCompilationState WithDocumentAttributes<TArg>(
        DocumentId documentId,
        TArg arg,
        Func<DocumentInfo.DocumentAttributes, TArg, DocumentInfo.DocumentAttributes> updateAttributes)
    {
        return UpdateDocumentState(
            SolutionState.WithDocumentAttributes(documentId, arg, updateAttributes), documentId);
    }

    internal SolutionCompilationState WithDocumentTexts(ImmutableArray<(DocumentId documentId, SourceText text)> texts, PreservationMode mode)
        => UpdateDocumentsInMultipleProjects<DocumentState, SourceText, PreservationMode>(
            texts,
            arg: mode,
            updateDocument: static (oldDocumentState, text, mode) =>
                SourceTextIsUnchanged(oldDocumentState, text) ? oldDocumentState : oldDocumentState.UpdateText(text, mode));

    private static bool SourceTextIsUnchanged(DocumentState oldDocument, SourceText text)
        => oldDocument.TryGetText(out var oldText) && text == oldText;

    /// <summary>
    /// Applies an update operation <paramref name="updateDocument"/> to specified <paramref name="documentsToUpdate"/>.
    /// Documents may be in different projects.
    /// </summary>
    private SolutionCompilationState UpdateDocumentsInMultipleProjects<TDocumentState, TDocumentData, TArg>(
        ImmutableArray<(DocumentId documentId, TDocumentData documentData)> documentsToUpdate,
        TArg arg,
        Func<TDocumentState, TDocumentData, TArg, TDocumentState> updateDocument)
        where TDocumentState : TextDocumentState
    {
        return WithDocumentStatesOfMultipleProjects(
            documentsToUpdate
                .GroupBy(static d => d.documentId.ProjectId)
                .Select(g =>
                {
                    var projectId = g.Key;
                    var oldProjectState = SolutionState.GetRequiredProjectState(projectId);
                    var oldDocumentStates = oldProjectState.GetDocumentStates<TDocumentState>();

                    using var _ = ArrayBuilder<TDocumentState>.GetInstance(out var newDocumentStates);
                    foreach (var (documentId, documentData) in g)
                    {
                        var oldDocumentState = oldDocumentStates.GetRequiredState(documentId);
                        var newDocumentState = updateDocument(oldDocumentState, documentData, arg);

                        if (ReferenceEquals(oldDocumentState, newDocumentState))
                            continue;

                        newDocumentStates.Add(newDocumentState);
                    }

                    return (projectId, newDocumentStates.ToImmutableAndClear());
                }),
            GetUpdateDocumentsTranslationAction);
    }

    /// <summary>
    /// Returns <see cref="SolutionCompilationState"/> with projects updated to new document states specified in <paramref name="updatedDocumentStatesPerProject"/>.
    /// </summary>
    private SolutionCompilationState WithDocumentStatesOfMultipleProjects<TDocumentState>(
        IEnumerable<(ProjectId projectId, ImmutableArray<TDocumentState> updatedDocumentState)> updatedDocumentStatesPerProject,
        Func<ProjectState, ImmutableArray<TDocumentState>, TranslationAction> getTranslationAction)
        where TDocumentState : TextDocumentState
    {
        var newCompilationState = this;

        foreach (var (projectId, newDocumentStates) in updatedDocumentStatesPerProject)
        {
            if (newDocumentStates.IsEmpty)
            {
                continue;
            }

            var oldProjectState = newCompilationState.SolutionState.GetRequiredProjectState(projectId);
            var compilationTranslationAction = getTranslationAction(oldProjectState, newDocumentStates);
            var newProjectState = compilationTranslationAction.NewProjectState;

            var stateChange = newCompilationState.SolutionState.ForkProject(
                oldProjectState,
                newProjectState);

            newCompilationState = newCompilationState.ForkProject(
                stateChange,
                static (_, compilationTranslationAction) => compilationTranslationAction,
                forkTracker: true,
                arg: compilationTranslationAction);
        }

        return newCompilationState;
    }

    /// <summary>
    /// Updates the <paramref name="oldProjectState"/> to a new state with <paramref name="newDocumentStates"/> and returns a <see cref="TranslationAction"/> that 
    /// reflects these changes in the project compilation.
    /// </summary>
    private static TranslationAction GetUpdateDocumentsTranslationAction<TDocumentState>(ProjectState oldProjectState, ImmutableArray<TDocumentState> newDocumentStates)
        where TDocumentState : TextDocumentState
    {
        return newDocumentStates switch
        {
            ImmutableArray<DocumentState> ordinaryNewDocumentStates => GetUpdateOrdinaryDocumentsTranslationAction(oldProjectState, ordinaryNewDocumentStates),
            ImmutableArray<AdditionalDocumentState> additionalNewDocumentStates => GetUpdateAdditionalDocumentsTranslationAction(oldProjectState, additionalNewDocumentStates),
            ImmutableArray<AnalyzerConfigDocumentState> analyzerConfigNewDocumentStates => GetUpdateAnalyzerConfigDocumentsTranslationAction(oldProjectState, analyzerConfigNewDocumentStates),
            _ => throw ExceptionUtilities.UnexpectedValue(typeof(TDocumentState))
        };

        TranslationAction GetUpdateOrdinaryDocumentsTranslationAction(ProjectState oldProjectState, ImmutableArray<DocumentState> newDocumentStates)
        {
            var oldDocumentStates = newDocumentStates.SelectAsArray(static (s, oldProjectState) => oldProjectState.DocumentStates.GetRequiredState(s.Id), oldProjectState);
            var newProjectState = oldProjectState.UpdateDocuments(oldDocumentStates, newDocumentStates);
            return new TranslationAction.TouchDocumentsAction(oldProjectState, newProjectState, oldDocumentStates, newDocumentStates);
        }

        TranslationAction GetUpdateAdditionalDocumentsTranslationAction(ProjectState oldProjectState, ImmutableArray<AdditionalDocumentState> newDocumentStates)
        {
            var oldDocumentStates = newDocumentStates.SelectAsArray(static (s, oldProjectState) => oldProjectState.AdditionalDocumentStates.GetRequiredState(s.Id), oldProjectState);
            var newProjectState = oldProjectState.UpdateAdditionalDocuments(oldDocumentStates, newDocumentStates);
            return new TranslationAction.TouchAdditionalDocumentsAction(oldProjectState, newProjectState, oldDocumentStates, newDocumentStates);
        }

        TranslationAction GetUpdateAnalyzerConfigDocumentsTranslationAction(ProjectState oldProjectState, ImmutableArray<AnalyzerConfigDocumentState> newDocumentStates)
        {
            var oldDocumentStates = newDocumentStates.SelectAsArray(static (s, oldProjectState) => oldProjectState.AnalyzerConfigDocumentStates.GetRequiredState(s.Id), oldProjectState);
            var newProjectState = oldProjectState.UpdateAnalyzerConfigDocuments(oldDocumentStates, newDocumentStates);
            return new TranslationAction.TouchAnalyzerConfigDocumentsAction(oldProjectState, newProjectState);
        }
    }

    public SolutionCompilationState WithDocumentState(
        DocumentState documentState)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentState(documentState), documentState.Id);
    }

    /// <inheritdoc cref="SolutionState.WithAdditionalDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithAdditionalDocumentText(
        DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateAdditionalDocumentState(
            this.SolutionState.WithAdditionalDocumentText(documentId, text, mode), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithAnalyzerConfigDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithAnalyzerConfigDocumentText(
        DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateAnalyzerConfigDocumentState(this.SolutionState.WithAnalyzerConfigDocumentText(documentId, text, mode));
    }

    /// <inheritdoc cref="SolutionState.WithFallbackAnalyzerOptions(ImmutableDictionary{string, StructuredAnalyzerConfigOptions})"/>
    public SolutionCompilationState WithFallbackAnalyzerOptions(ImmutableDictionary<string, StructuredAnalyzerConfigOptions> options)
        => Branch(SolutionState.WithFallbackAnalyzerOptions(options));

    /// <inheritdoc cref="SolutionState.WithDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithDocumentText(
        DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentText(documentId, textAndVersion, mode), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithAdditionalDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithAdditionalDocumentText(
        DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateAdditionalDocumentState(
            this.SolutionState.WithAdditionalDocumentText(documentId, textAndVersion, mode), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithAnalyzerConfigDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithAnalyzerConfigDocumentText(
        DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateAnalyzerConfigDocumentState(
            this.SolutionState.WithAnalyzerConfigDocumentText(documentId, textAndVersion, mode));
    }

    /// <inheritdoc cref="Solution.WithDocumentSyntaxRoots(ImmutableArray{ValueTuple{DocumentId, SyntaxNode}}, PreservationMode)"/>
    public SolutionCompilationState WithDocumentSyntaxRoots(ImmutableArray<(DocumentId documentId, SyntaxNode root)> syntaxRoots, PreservationMode mode)
    {
        return UpdateDocumentsInMultipleProjects<DocumentState, SyntaxNode, PreservationMode>(
            syntaxRoots,
            arg: mode,
            static (oldDocumentState, root, mode) =>
                oldDocumentState.TryGetSyntaxTree(out var oldTree) && oldTree.TryGetRoot(out var oldRoot) && oldRoot == root
                ? oldDocumentState
                : oldDocumentState.UpdateTree(root, mode));
    }

    public SolutionCompilationState WithDocumentContentsFrom(
        ImmutableArray<(DocumentId documentId, DocumentState documentState)> documentIdsAndStates, bool forceEvenIfTreesWouldDiffer)
    {
        return UpdateDocumentsInMultipleProjects<DocumentState, DocumentState, bool>(
            documentIdsAndStates,
            arg: forceEvenIfTreesWouldDiffer,
            static (oldDocumentState, documentState, forceEvenIfTreesWouldDiffer) =>
                oldDocumentState.TextAndVersionSource == documentState.TextAndVersionSource && oldDocumentState.TreeSource == documentState.TreeSource
                ? oldDocumentState
                : oldDocumentState.UpdateTextAndTreeContents(documentState.TextAndVersionSource, documentState.TreeSource, forceEvenIfTreesWouldDiffer));
    }

    /// <inheritdoc cref="SolutionState.WithDocumentSourceCodeKind"/>
    public SolutionCompilationState WithDocumentSourceCodeKind(
        DocumentId documentId, SourceCodeKind sourceCodeKind)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentSourceCodeKind(documentId, sourceCodeKind), documentId);
    }

    /// <inheritdoc cref="SolutionState.UpdateDocumentTextLoader"/>
    public SolutionCompilationState UpdateDocumentTextLoader(
        DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var stateChange = this.SolutionState.UpdateDocumentTextLoader(documentId, loader, mode);

        // Note: state is currently not reused.
        // If UpdateDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
        Debug.Assert(stateChange.NewSolutionState != this.SolutionState);

        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateDocumentState(stateChange, documentId);
    }

    /// <inheritdoc cref="SolutionState.UpdateAdditionalDocumentTextLoader"/>
    public SolutionCompilationState UpdateAdditionalDocumentTextLoader(
        DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var stateChange = this.SolutionState.UpdateAdditionalDocumentTextLoader(documentId, loader, mode);

        // Note: state is currently not reused.
        // If UpdateAdditionalDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
        Debug.Assert(stateChange.NewSolutionState != this.SolutionState);

        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateAdditionalDocumentState(stateChange, documentId);
    }

    /// <inheritdoc cref="SolutionState.UpdateAnalyzerConfigDocumentTextLoader"/>
    public SolutionCompilationState UpdateAnalyzerConfigDocumentTextLoader(
        DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        var stateChange = this.SolutionState.UpdateAnalyzerConfigDocumentTextLoader(documentId, loader, mode);

        // Note: state is currently not reused.
        // If UpdateAnalyzerConfigDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
        Debug.Assert(stateChange.NewSolutionState != this.SolutionState);

        // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
        // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
        return UpdateAnalyzerConfigDocumentState(stateChange);
    }

    private SolutionCompilationState UpdateDocumentState(StateChange stateChange, DocumentId documentId)
    {
        return ForkProject(
            stateChange,
            static (stateChange, documentId) =>
            {
                // This function shouldn't have been called if the document has not changed
                Debug.Assert(stateChange.OldProjectState != stateChange.NewProjectState);

                var oldDocument = stateChange.OldProjectState.DocumentStates.GetRequiredState(documentId);
                var newDocument = stateChange.NewProjectState.DocumentStates.GetRequiredState(documentId);

                return new TranslationAction.TouchDocumentsAction(
                    stateChange.OldProjectState, stateChange.NewProjectState, [oldDocument], [newDocument]);
            },
            forkTracker: true,
            arg: documentId);
    }

    private SolutionCompilationState UpdateAdditionalDocumentState(StateChange stateChange, DocumentId documentId)
    {
        return ForkProject(
            stateChange,
            static (stateChange, documentId) =>
            {
                // This function shouldn't have been called if the document has not changed
                Debug.Assert(stateChange.OldProjectState != stateChange.NewProjectState);

                var oldDocument = stateChange.OldProjectState.AdditionalDocumentStates.GetRequiredState(documentId);
                var newDocument = stateChange.NewProjectState.AdditionalDocumentStates.GetRequiredState(documentId);

                return new TranslationAction.TouchAdditionalDocumentsAction(
                    stateChange.OldProjectState, stateChange.NewProjectState, [oldDocument], [newDocument]);
            },
            forkTracker: true,
            arg: documentId);
    }

    private SolutionCompilationState UpdateAnalyzerConfigDocumentState(StateChange stateChange)
    {
        return ForkProject(
            stateChange,
            static stateChange => new TranslationAction.TouchAnalyzerConfigDocumentsAction(stateChange.OldProjectState, stateChange.NewProjectState),
            forkTracker: true);
    }

    /// <summary>
    /// Gets the <see cref="Project"/> associated with an assembly symbol.
    /// </summary>
    public static ProjectId? GetProjectId(IAssemblySymbol? assemblySymbol)
    {
        if (assemblySymbol == null)
            return null;

        s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblySymbol, out var id);
        return id;
    }

    private bool TryGetCompilationTracker(ProjectId projectId, [NotNullWhen(returnValue: true)] out ICompilationTracker? tracker)
        => _projectIdToTrackerMap.TryGetValue(projectId, out tracker);

    private static readonly Func<ProjectId, SolutionState, RegularCompilationTracker> s_createCompilationTrackerFunction = CreateCompilationTracker;

    private static RegularCompilationTracker CreateCompilationTracker(ProjectId projectId, SolutionState solution)
    {
        var projectState = solution.GetProjectState(projectId);
        Contract.ThrowIfNull(projectState);
        return new RegularCompilationTracker(projectState);
    }

    private ICompilationTracker GetCompilationTracker(ProjectId projectId)
    {
        if (!_projectIdToTrackerMap.TryGetValue(projectId, out var tracker))
        {
            tracker = RoslynImmutableInterlocked.GetOrAdd(ref _projectIdToTrackerMap, projectId, s_createCompilationTrackerFunction, this.SolutionState);
        }

        return tracker;
    }

    public Task<VersionStamp> GetDependentVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentVersionAsync(this, cancellationToken);

    public Task<VersionStamp> GetDependentSemanticVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentSemanticVersionAsync(this, cancellationToken);

    public Task<Checksum> GetDependentChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentChecksumAsync(this, cancellationToken);

    public bool TryGetCompilation(ProjectId projectId, [NotNullWhen(returnValue: true)] out Compilation? compilation)
    {
        this.SolutionState.CheckContainsProject(projectId);
        compilation = null;

        return this.TryGetCompilationTracker(projectId, out var tracker)
            && tracker.TryGetCompilation(out compilation);
    }

    /// <summary>
    /// Returns the compilation for the specified <see cref="ProjectId"/>.  Can return <see langword="null"/> when the project
    /// does not support compilations.
    /// </summary>
    /// <remarks>
    /// The compilation is guaranteed to have a syntax tree for each document of the project.
    /// </remarks>
    private Task<Compilation?> GetCompilationAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        // TODO: figure out where this is called and why the nullable suppression is required
        return GetCompilationAsync(this.SolutionState.GetProjectState(projectId)!, cancellationToken);
    }

    /// <summary>
    /// Returns the compilation for the specified <see cref="ProjectState"/>.  Can return <see langword="null"/> when the project
    /// does not support compilations.
    /// </summary>
    /// <remarks>
    /// The compilation is guaranteed to have a syntax tree for each document of the project.
    /// </remarks>
    public Task<Compilation?> GetCompilationAsync(ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetCompilationAsync(this, cancellationToken).AsNullable()
            : SpecializedTasks.Null<Compilation>();
    }

    /// <summary>
    /// Return reference completeness for the given project and all projects this references.
    /// </summary>
    public Task<bool> HasSuccessfullyLoadedAsync(ProjectState project, CancellationToken cancellationToken)
    {
        // return HasAllInformation when compilation is not supported.
        // regardless whether project support compilation or not, if projectInfo is not complete, we can't guarantee its reference completeness
        return project.SupportsCompilation
            ? this.GetCompilationTracker(project.Id).HasSuccessfullyLoadedAsync(this, cancellationToken)
            : project.HasAllInformation ? SpecializedTasks.True : SpecializedTasks.False;
    }

    /// <summary>
    /// Returns the generated document states for source generated documents.
    /// </summary>
    public ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(ProjectState project, CancellationToken cancellationToken)
        => GetSourceGeneratedDocumentStatesAsync(project, withFrozenSourceGeneratedDocuments: true, cancellationToken);

    /// <inheritdoc cref="GetSourceGeneratedDocumentStatesAsync(ProjectState, CancellationToken)"/>
    public ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(
        ProjectState project, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetSourceGeneratedDocumentStatesAsync(this, withFrozenSourceGeneratedDocuments, cancellationToken)
            : new(TextDocumentStates<SourceGeneratedDocumentState>.Empty);
    }

    public ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
        ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetSourceGeneratorDiagnosticsAsync(this, cancellationToken)
            : new([]);
    }

    public ValueTask<GeneratorDriverRunResult?> GetSourceGeneratorRunResultAsync(
    ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetSourceGeneratorRunResultAsync(this, cancellationToken)
            : new();
    }

    /// <summary>
    /// Returns the <see cref="SourceGeneratedDocumentState"/> for a source generated document that has already been generated and observed.
    /// </summary>
    /// <remarks>
    /// This is only safe to call if you already have seen the SyntaxTree or equivalent that indicates the document state has already been
    /// generated. This method exists to implement <see cref="Solution.GetDocument(SyntaxTree?)"/> and is best avoided unless you're doing something
    /// similarly tricky like that.
    /// </remarks>
    public SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(
        DocumentId documentId)
    {
        return GetCompilationTracker(documentId.ProjectId).TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
    }

    /// <summary>
    /// Get a metadata reference to this compilation info's compilation with respect to
    /// another project. For cross language references produce a skeletal assembly. If the
    /// compilation is not available, it is built. If a skeletal assembly reference is
    /// needed and does not exist, it is also built.
    /// </summary>
    private async Task<MetadataReference?> GetMetadataReferenceAsync(
        ICompilationTracker tracker, ProjectState fromProject, ProjectReference projectReference, bool includeCrossLanguage, CancellationToken cancellationToken)
    {
        try
        {
            // If same language then we can wrap the other project's compilation into a compilation reference
            if (tracker.ProjectState.LanguageServices == fromProject.LanguageServices)
            {
                // otherwise, base it off the compilation by building it first.
                var compilation = await tracker.GetCompilationAsync(this, cancellationToken).ConfigureAwait(false);
                return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
            }

            if (!includeCrossLanguage)
                return null;

            // otherwise get a metadata only image reference that is built by emitting the metadata from the
            // referenced project's compilation and re-importing it.
            using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_GetMetadataOnlyImage, cancellationToken))
            {
                var properties = new MetadataReferenceProperties(aliases: projectReference.Aliases, embedInteropTypes: projectReference.EmbedInteropTypes);
                return await tracker.GetOrBuildSkeletonReferenceAsync(this, properties, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Get a metadata reference for the project's compilation.  Returns <see langword="null"/> upon failure, which 
    /// can happen when trying to build a skeleton reference that fails to build.
    /// </summary>
    public Task<MetadataReference?> GetMetadataReferenceAsync(
        ProjectReference projectReference, ProjectState fromProject, bool includeCrossLanguage, CancellationToken cancellationToken)
    {
        try
        {
            // Get the compilation state for this project.  If it's not already created, then this
            // will create it.  Then force that state to completion and get a metadata reference to it.
            var tracker = this.GetCompilationTracker(projectReference.ProjectId);
            return GetMetadataReferenceAsync(tracker, fromProject, projectReference, includeCrossLanguage, cancellationToken);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Undoes the operation of <see cref="WithFrozenSourceGeneratedDocuments"/>; any frozen source generated document is allowed
    /// to have it's real output again.
    /// </summary>
    public SolutionCompilationState WithoutFrozenSourceGeneratedDocuments()
    {
        // If there's nothing frozen, there's nothing to do.
        if (FrozenSourceGeneratedDocumentStates == null)
            return this;

        var projectIdsToUnfreeze = FrozenSourceGeneratedDocumentStates.States.Values
            .Select(static state => state.Identity.DocumentId.ProjectId)
            .Distinct()
            .ToImmutableArray();

        // Since we previously froze documents in these projects, we should have a CompilationTracker entry for it, and
        // it should be a WithFrozenSourceGeneratedDocumentsCompilationTracker. To undo the operation, we'll just
        // restore the original CompilationTracker.
        var newTrackerMap = CreateCompilationTrackerMap(
            projectIdsToUnfreeze,
            this.SolutionState.GetProjectDependencyGraph(),
            static (trackerMap, projectIdsToUnfreeze) =>
            {
                foreach (var projectId in projectIdsToUnfreeze)
                {
                    Contract.ThrowIfFalse(trackerMap.TryGetValue(projectId, out var existingTracker));
                    // TODO(cyrusn): Is it possible to wrap an underlying tracker with multiple frozen document
                    // compilation trackers?  Should we be unwrapping as much as we can here?  Or would that also be bad
                    // given that we're basing what we want to unfreeze on the FrozenSourceGeneratedDocumentStates,
                    // which may not represent those inner freezes.  Unclear.  There may be bugs here.
                    var replacingItemTracker = (WithFrozenSourceGeneratedDocumentsCompilationTracker)existingTracker;
                    trackerMap[projectId] = replacingItemTracker.UnderlyingTracker;
                }
            },
            projectIdsToUnfreeze,
            skipEmptyCallback: projectIdsToUnfreeze.Length == 0);

        // We pass the same solution state, since this change is only a change of the generated documents -- none of the core
        // documents or project structure changes in any way.
        return this.Branch(
            this.SolutionState,
            projectIdToTrackerMap: newTrackerMap,
            frozenSourceGeneratedDocumentStates: null);
    }

    /// <summary>
    /// Returns a new SolutionState that will always produce a specific output for a generated file. This is used only in the
    /// implementation of <see cref="TextExtensions.GetOpenDocumentInCurrentContextWithChanges"/> where if a user has a source
    /// generated file open, we need to make sure everything lines up.
    /// </summary>
    public SolutionCompilationState WithFrozenSourceGeneratedDocuments(
        ImmutableArray<(SourceGeneratedDocumentIdentity documentIdentity, DateTime generationDateTime, SourceText sourceText)> documents)
    {
        // We won't support freezing multiple source generated documents more than once in a chain, simply because we have no need
        // to support that; these solutions are created on demand when we need to operate on an open source generated document,
        // and so those are always forks off the main solution. There's also a bit of a design question -- does calling this a second time
        // leave the existing frozen documents in place, or replace them? It depends on the need, but until then we'll cross that bridge
        // if/when we need it.
        Contract.ThrowIfFalse(FrozenSourceGeneratedDocumentStates == null, $"We shouldn't be calling {nameof(WithFrozenSourceGeneratedDocuments)} on a solution with frozen source generated documents.");

        if (documents.IsEmpty)
            return this;

        // We'll keep track if every document we're reusing is the exact same as the final generated output we already have
        using var _ = ArrayBuilder<SourceGeneratedDocumentState>.GetInstance(documents.Length, out var documentStates);
        foreach (var (documentIdentity, generationDateTime, sourceText) in documents)
        {
            var existingGeneratedState = TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentIdentity.DocumentId);
            if (existingGeneratedState != null)
            {
                var newGeneratedState = existingGeneratedState
                    .WithText(sourceText)
                    .WithParseOptions(existingGeneratedState.ParseOptions)
                    .WithGenerationDateTime(generationDateTime);

                // If the content already matched, we can just reuse the existing state, so we don't need to track this one
                if (newGeneratedState != existingGeneratedState)
                    documentStates.Add(newGeneratedState);
            }
            else
            {
                // There is no document that we know of yet, so we'll add this back in
                var projectState = this.SolutionState.GetRequiredProjectState(documentIdentity.DocumentId.ProjectId);
                var newGeneratedState = SourceGeneratedDocumentState.Create(
                    documentIdentity,
                    sourceText,
                    projectState.ParseOptions!,
                    projectState.LanguageServices,
                    // Just compute the checksum from the source text passed in.
                    originalSourceTextChecksum: null,
                    generationDateTime);
                documentStates.Add(newGeneratedState);
            }
        }

        // If every document we looked at matched what we've already generated, we have nothing new to do
        if (documentStates.Count == 0)
            return this;

        var documentStatesByProjectId = documentStates.ToDictionary(static state => state.Id.ProjectId);
        var newTrackerMap = CreateCompilationTrackerMap(
            [.. documentStatesByProjectId.Keys],
            this.SolutionState.GetProjectDependencyGraph(),
            static (trackerMap, arg) =>
            {
                foreach (var (projectId, documentStatesForProject) in arg.documentStatesByProjectId)
                {
                    // We want to create a new snapshot with a new compilation tracker that will do this replacement.
                    // If we already have an existing tracker we'll just wrap that (so we also are reusing any underlying
                    // computations). If we don't have one, we'll create one and then wrap it.
                    if (!trackerMap.TryGetValue(projectId, out var existingTracker))
                    {
                        existingTracker = CreateCompilationTracker(projectId, arg.SolutionState);
                    }

                    trackerMap[projectId] = new WithFrozenSourceGeneratedDocumentsCompilationTracker(existingTracker, new(documentStatesForProject));
                }
            },
            (documentStatesByProjectId, this.SolutionState),
            skipEmptyCallback: false);

        // We pass the same solution state, since this change is only a change of the generated documents -- none of the core
        // documents or project structure changes in any way.
        return this.Branch(
            this.SolutionState,
            projectIdToTrackerMap: newTrackerMap,
            frozenSourceGeneratedDocumentStates: new TextDocumentStates<SourceGeneratedDocumentState>(documentStates));
    }

    public SolutionCompilationState WithNewWorkspace(string? workspaceKind, int workspaceVersion, SolutionServices services)
    {
        return this.Branch(
            this.SolutionState.WithNewWorkspace(workspaceKind, workspaceVersion, services));
    }

    public SolutionCompilationState WithOptions(SolutionOptionSet options)
    {
        return this.Branch(
            this.SolutionState.WithOptions(options));
    }

    /// <summary>
    /// Updates entries in our <see cref="_sourceGeneratorExecutionVersionMap"/> to the corresponding values in the
    /// given <paramref name="sourceGeneratorExecutionVersions"/>.  Importantly, <paramref
    /// name="sourceGeneratorExecutionVersions"/> must refer to projects in this solution.  Projects not mentioned in
    /// <paramref name="sourceGeneratorExecutionVersions"/> will not be touched (and they will stay in the map).
    /// </summary>
    public SolutionCompilationState UpdateSpecificSourceGeneratorExecutionVersions(
        SourceGeneratorExecutionVersionMap sourceGeneratorExecutionVersions)
    {
        var versionMapBuilder = _sourceGeneratorExecutionVersionMap.Map.ToBuilder();
        var newIdToTrackerMapBuilder = _projectIdToTrackerMap.ToBuilder();
        var changed = false;

        foreach (var (projectId, sourceGeneratorExecutionVersion) in sourceGeneratorExecutionVersions.Map)
        {
            var currentExecutionVersion = versionMapBuilder[projectId];

            // Nothing to do if already at this version.
            if (currentExecutionVersion == sourceGeneratorExecutionVersion)
                continue;

            changed = true;
            versionMapBuilder[projectId] = sourceGeneratorExecutionVersion;

            // If we do already have a compilation tracker for this project, then let the tracker know that the source
            // generator version has changed. We do this by telling it that it should now create SG docs and skeleton
            // references if they're out of date.
            if (_projectIdToTrackerMap.TryGetValue(projectId, out var existingTracker))
            {
                // if the major version has changed then we also want to drop the generator driver so that we're rerun
                // generators from scratch.
                var forceRegeneration = currentExecutionVersion.MajorVersion != sourceGeneratorExecutionVersion.MajorVersion;
                var newTracker = existingTracker.WithCreateCreationPolicy(forceRegeneration);
                if (newTracker != existingTracker)
                    newIdToTrackerMapBuilder[projectId] = newTracker;
            }
        }

        if (!changed)
            return this;

        return this.Branch(
            this.SolutionState,
            projectIdToTrackerMap: newIdToTrackerMapBuilder.ToImmutable(),
            sourceGeneratorExecutionVersionMap: new(versionMapBuilder.ToImmutable()));
    }

    public SolutionCompilationState WithFrozenPartialCompilations(CancellationToken cancellationToken)
        => _cachedFrozenSnapshot.GetValue(cancellationToken);

    private SolutionCompilationState ComputeFrozenSnapshot(CancellationToken cancellationToken)
    {
        var newIdToProjectStateMapBuilder = this.SolutionState.ProjectStates.ToBuilder();
        var newIdToTrackerMapBuilder = _projectIdToTrackerMap.ToBuilder();

        foreach (var projectId in this.SolutionState.ProjectIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Definitely do nothing for non-C#/VB projects.  We have nothing to freeze in that case.
            var oldProjectState = this.SolutionState.GetRequiredProjectState(projectId);
            if (!oldProjectState.SupportsCompilation)
                continue;

            var oldTracker = GetCompilationTracker(projectId);

            // Since we're freezing, set both generators and skeletons to not be created.  We don't want to take any
            // perf hit on either of those at all for our clients.
            var newTracker = oldTracker.WithDoNotCreateCreationPolicy(cancellationToken);
            if (oldTracker == newTracker)
                continue;

            Contract.ThrowIfFalse(newIdToProjectStateMapBuilder.ContainsKey(projectId));

            var newProjectState = newTracker.ProjectState;

            newIdToProjectStateMapBuilder[projectId] = newProjectState;
            newIdToTrackerMapBuilder[projectId] = newTracker;
        }

        var newIdToProjectStateMap = newIdToProjectStateMapBuilder.ToImmutable();
        var newIdToTrackerMap = newIdToTrackerMapBuilder.ToImmutable();

        var dependencyGraph = SolutionState.CreateDependencyGraph(this.SolutionState.ProjectIds, newIdToProjectStateMap);

        var newState = this.SolutionState.Branch(
            idToProjectStateMap: newIdToProjectStateMap,
            dependencyGraph: dependencyGraph);

        var newCompilationState = this.Branch(
            newState,
            newIdToTrackerMap,
            // Set the frozen solution to be its own frozen solution.  Freezing multiple times is a no-op.
            cachedFrozenSnapshot: _cachedFrozenSnapshot);

        return newCompilationState;
    }

    /// <summary>
    /// Creates a branch of the solution that has its compilations frozen in whatever state they are in at the time,
    /// assuming a background compiler is busy building this compilations.
    /// <para/>
    /// A compilation for the project containing the specified document id will be guaranteed to exist with at least the
    /// syntax tree for the document.
    /// <para/>
    /// This not intended to be the public API, use Document.WithFrozenPartialSemantics() instead.
    /// </summary>
    public SolutionCompilationState WithFrozenPartialCompilationIncludingSpecificDocument(
        DocumentId documentId, CancellationToken cancellationToken)
    {
        // in progress solutions are disabled for some testing
        if (this.Services.GetService<IWorkspacePartialSolutionsTestHook>()?.IsPartialSolutionDisabled == true)
            return this;

        var currentCompilationState = this;
        var currentDocumentState = this.SolutionState.GetRequiredDocumentState(documentId);

        // We want all linked versions of this document to also be present in the frozen solution snapshot (that way
        // features like 'completion' can see that there are linked docs and give messages about symbols not being
        // available in certain project contexts). We do this in a slightly hacky way for perf though. Specifically,
        // instead of parsing *all* the sibling files (which can be expensive, especially for a file linked in many
        // projects/tfms), we only parse this single tree.  We then use that same tree across all siblings.  That's
        // technically inaccurate, but we can accept that as the primary purpose of 'frozen partial' is to get a
        // snapshot *fast* that is allowed to be *inaccurate*.
        //
        // Note: this does mean that some *potentially* desirable feature behaviors may not be possible.  For example,
        // because of this unification, all targets will see the user in the same parsed #if region.  That means, if the
        // user is in a conditionally-disabled region in the primary target, they will also be in such a region in all
        // other targets.  This would prevent such a feature from using the information from other targets (perhaps
        // where it is not conditionally-disabled) to drive a richer experience here.  We consider that acceptable given
        // the perf benefit.  But we could consider relaxing this in the future.
        //
        // Note: this is very different from the logic we have in the workspace to 'UnifyLinkedDocumentContents'. In
        // that case, we only share trees when completely safe and accurate to do so (for example, where no
        // directives are involved).  As that is used for the real solution snapshot, it must be correct.  The
        // frozen-partial snapshot is different as it is a fork that is already allowed to be inaccurate for perf
        // reasons (for example, missing trees, or missing references).
        //
        // The 'forceEvenIfTreesWouldDiffer' flag here allows us to share the doc contents even in the case where
        // correctness might be violated.
        //
        // Note: this forking can still be expensive.  It would be nice to do this as one large fork step rather than N
        // medium sized ones.
        //
        // Note: GetRelatedDocumentIds will include `documentId` as well.  But that's ok.  Calling
        // WithDocumentContentsFrom with the current document state no-ops immediately, returning back the same
        // compilation state instance.  So in the case where there are no linked documents, there is no cost here.  And
        // there is no additional cost processing the initiating document in this loop.
        var allDocumentIds = this.SolutionState.GetRelatedDocumentIds(documentId);
        var allDocumentIdsWithCurrentDocumentState = allDocumentIds.SelectAsArray(static (docId, currentDocumentState) => (docId, currentDocumentState), currentDocumentState);
        currentCompilationState = currentCompilationState.WithDocumentContentsFrom(allDocumentIdsWithCurrentDocumentState, forceEvenIfTreesWouldDiffer: true);

        return WithFrozenPartialCompilationIncludingSpecificDocumentWorker(currentCompilationState, documentId, cancellationToken);

        // Intentionally static, so we only operate on @this, not `this`.
        static SolutionCompilationState WithFrozenPartialCompilationIncludingSpecificDocumentWorker(
            SolutionCompilationState @this, DocumentId documentId, CancellationToken cancellationToken)
        {
            try
            {
                var allDocumentIds = @this.SolutionState.GetRelatedDocumentIds(documentId);
                using var _ = ArrayBuilder<DocumentState>.GetInstance(allDocumentIds.Length, out var documentStates);

                // We grab all the contents of linked files as well to ensure that our snapshot is correct wrt to the
                // set of linked document ids our state says are in it.  Note: all of these trees should share the same
                // green trees, as that is setup in our outer caller.  This helps ensure that the cost here is low for
                // files with lots of linked siblings.
                foreach (var currentDocumentId in allDocumentIds)
                {
                    var documentState = @this.SolutionState.GetRequiredDocumentState(currentDocumentId);
                    documentStates.Add(documentState);
                }

                // now freeze the solution state, capturing whatever compilations are in progress.
                var frozenCompilationState = @this.WithFrozenPartialCompilations(cancellationToken);

                return ComputeFrozenPartialState(frozenCompilationState, documentStates, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        static SolutionCompilationState ComputeFrozenPartialState(
            SolutionCompilationState frozenCompilationState,
            ArrayBuilder<DocumentState> documentStates,
            CancellationToken cancellationToken)
        {
            var currentState = frozenCompilationState;

            using var _ = PooledDictionary<ProjectId, ArrayBuilder<DocumentState>>.GetInstance(out var missingDocumentStates);

            // First, either update documents that have changed, or keep track of documents that are missing.
            foreach (var newDocumentState in documentStates)
            {
                var documentId = newDocumentState.Id;
                var oldProjectState = currentState.SolutionState.GetRequiredProjectState(documentId.ProjectId);
                var oldDocumentState = oldProjectState.DocumentStates.GetState(documentId);

                if (oldDocumentState is null)
                {
                    missingDocumentStates.MultiAdd(documentId.ProjectId, newDocumentState);
                }
                else
                {
                    currentState = currentState.WithDocumentState(newDocumentState);
                }
            }

            // Now, add all missing documents per project.
            currentState = currentState.WithDocumentStatesOfMultipleProjects(
                // Do a SelectAsArray here to ensure that we realize the array once, and as such only call things like
                // ToImmutableAndFree once per ArrayBuilder.
                missingDocumentStates.SelectAsArray(kvp => (kvp.Key, kvp.Value.ToImmutableAndFree())),
                GetAddDocumentsTranslationAction);

            return currentState;
        }
    }

    /// <summary>
    /// Core helper that takes a set of <see cref="DocumentInfo" />s and does the application of the appropriate documents to each project.
    /// </summary>
    /// <param name="documentInfos">The set of documents to add.</param>
    public SolutionCompilationState AddDocumentsToMultipleProjects<TDocumentState>(
        ImmutableArray<DocumentInfo> documentInfos)
        where TDocumentState : TextDocumentState
    {
        if (documentInfos.IsDefault)
            throw new ArgumentNullException(nameof(documentInfos));

        if (documentInfos.IsEmpty)
            return this;

        // The documents might be contributing to multiple different projects; split them by project and then we'll
        // process one project at a time.
        return WithDocumentStatesOfMultipleProjects(
            documentInfos.GroupBy(d => d.Id.ProjectId).Select(g =>
            {
                var projectId = g.Key;
                SolutionState.CheckContainsProject(projectId);
                var projectState = SolutionState.GetRequiredProjectState(projectId);
                return (projectId, newDocumentStates: g.SelectAsArray(projectState.CreateDocument<TDocumentState>));
            }),
            GetAddDocumentsTranslationAction);
    }

    public SolutionCompilationState RemoveDocumentsFromMultipleProjects<T>(ImmutableArray<DocumentId> documentIds)
        where T : TextDocumentState
    {
        if (documentIds.IsEmpty)
        {
            return this;
        }

        // The documents might be contributing to multiple different projects; split them by project and then we'll process
        // project-at-a-time.
        var documentIdsByProjectId = documentIds.ToLookup(id => id.ProjectId);

        var newCompilationState = this;

        foreach (var documentIdsInProject in documentIdsByProjectId)
        {
            newCompilationState = newCompilationState.RemoveDocumentsFromSingleProject<T>(documentIdsInProject.Key, [.. documentIdsInProject]);
        }

        return newCompilationState;
    }

    private SolutionCompilationState RemoveDocumentsFromSingleProject<T>(ProjectId projectId, ImmutableArray<DocumentId> documentIds)
        where T : TextDocumentState
    {
        using var _ = ArrayBuilder<T>.GetInstance(out var removedDocumentStates);

        var oldProjectState = SolutionState.GetRequiredProjectState(projectId);
        var oldDocumentStates = oldProjectState.GetDocumentStates<T>();

        foreach (var documentId in documentIds)
        {
            removedDocumentStates.Add(oldDocumentStates.GetRequiredState(documentId));
        }

        var removedDocumentStatesForProject = removedDocumentStates.ToImmutable();

        var compilationTranslationAction = GetRemoveDocumentsTranslationAction(oldProjectState, documentIds, removedDocumentStatesForProject);
        var newProjectState = compilationTranslationAction.NewProjectState;

        var stateChange = SolutionState.ForkProject(
            oldProjectState,
            newProjectState);

        return ForkProject(
            stateChange,
            static (_, compilationTranslationAction) => compilationTranslationAction,
            forkTracker: true,
            arg: compilationTranslationAction);
    }

    private static TranslationAction GetRemoveDocumentsTranslationAction<TDocumentState>(ProjectState oldProject, ImmutableArray<DocumentId> documentIds, ImmutableArray<TDocumentState> states)
        => states switch
        {
            ImmutableArray<DocumentState> documentStates => new TranslationAction.RemoveDocumentsAction(oldProject, oldProject.RemoveDocuments(documentIds), documentStates),
            ImmutableArray<AdditionalDocumentState> additionalDocumentStates => new TranslationAction.RemoveAdditionalDocumentsAction(oldProject, oldProject.RemoveAdditionalDocuments(documentIds), additionalDocumentStates),
            ImmutableArray<AnalyzerConfigDocumentState> _ => new TranslationAction.TouchAnalyzerConfigDocumentsAction(oldProject, oldProject.RemoveAnalyzerConfigDocuments(documentIds)),
            _ => throw ExceptionUtilities.UnexpectedValue(states)
        };

    private static TranslationAction GetAddDocumentsTranslationAction<TDocumentState>(ProjectState oldProject, ImmutableArray<TDocumentState> states)
        => states switch
        {
            ImmutableArray<DocumentState> documentStates => new TranslationAction.AddDocumentsAction(oldProject, oldProject.AddDocuments(documentStates), documentStates),
            ImmutableArray<AdditionalDocumentState> additionalDocumentStates => new TranslationAction.AddAdditionalDocumentsAction(oldProject, oldProject.AddAdditionalDocuments(additionalDocumentStates), additionalDocumentStates),
            ImmutableArray<AnalyzerConfigDocumentState> analyzerConfigDocumentStates => new TranslationAction.TouchAnalyzerConfigDocumentsAction(oldProject, oldProject.AddAnalyzerConfigDocuments(analyzerConfigDocumentStates)),
            _ => throw ExceptionUtilities.UnexpectedValue(states)
        };

    /// <inheritdoc cref="Solution.WithCachedSourceGeneratorState(ProjectId, Project)"/>
    public SolutionCompilationState WithCachedSourceGeneratorState(ProjectId projectToUpdate, Project projectWithCachedGeneratorState)
    {
        this.SolutionState.CheckContainsProject(projectToUpdate);

        // First see if we have a generator driver that we can get from the other project.

        if (!projectWithCachedGeneratorState.Solution.CompilationState.TryGetCompilationTracker(projectWithCachedGeneratorState.Id, out var tracker) ||
            tracker.GeneratorDriver is null)
        {
            // We don't actually have any state at all, so no change.
            return this;
        }

        var projectToUpdateState = this.SolutionState.GetRequiredProjectState(projectToUpdate);

        // Note: we have to force this fork to happen as the actual solution-state object is not changing. We're just
        // changing the tracker for a particular project.
        var newCompilationState = this.ForceForkProject(
            new(this.SolutionState, projectToUpdateState, projectToUpdateState),
            translate: new TranslationAction.ReplaceGeneratorDriverAction(
                oldProjectState: projectToUpdateState,
                newProjectState: projectToUpdateState,
                tracker.GeneratorDriver),
            forkTracker: true);

        return newCompilationState;
    }

    /// <summary>
    /// Creates a new solution instance with all the documents specified updated to have the same specified text.
    /// </summary>
    public SolutionCompilationState WithDocumentText(IEnumerable<DocumentId?> documentIds, SourceText text, PreservationMode mode)
    {
        using var _ = ArrayBuilder<(DocumentId, SourceText)>.GetInstance(out var changedDocuments);

        foreach (var documentId in documentIds)
        {
            // This API has always allowed null document IDs and documents IDs not contained within the solution. So
            // skip those if we run into that (otherwise the call to WithDocumentText will throw, as it is more
            // restrictive).
            if (documentId is null)
                continue;

            var documentState = this.SolutionState.GetProjectState(documentId.ProjectId)?.DocumentStates.GetState(documentId);
            if (documentState != null)
            {
                // before allocating an array below (and calling into a function that does a fair amount of linq work),
                // do a fast check if the text has actually changed. this shows up in allocation traces and is
                // worthwhile to avoid for the common case where we're continually being asked to update the same doc to
                // the same text (for example, when GetOpenDocumentInCurrentContextWithChanges) is called.
                //
                // The use of GetRequiredState mirrors what happens in WithDocumentTexts
                if (!SourceTextIsUnchanged(documentState, text))
                    changedDocuments.Add((documentId, text));
            }
        }

        if (changedDocuments.Count == 0)
            return this;

        return this.WithDocumentTexts(changedDocuments.ToImmutableAndClear(), mode);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SolutionCompilationState compilationState)
    {
        public GeneratorDriver? GetGeneratorDriver(Project project)
            => project.SupportsCompilation ? compilationState.GetCompilationTracker(project.Id).GeneratorDriver : null;
    }
}
