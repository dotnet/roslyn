// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.SolutionInfo;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    public bool PartialSemanticsEnabled { get; }

    // Values for all these are created on demand.
    private ImmutableDictionary<ProjectId, ICompilationTracker> _projectIdToTrackerMap;

    /// <summary>
    /// Cache we use to map between unrooted symbols (i.e. assembly, module and dynamic symbols) and the project
    /// they came from.  That way if we are asked about many symbols from the same assembly/module we can answer the
    /// question quickly after computing for the first one.  Created on demand.
    /// </summary>
    private ConditionalWeakTable<ISymbol, ProjectId?>? _unrootedSymbolToProjectId;
    private static readonly Func<ConditionalWeakTable<ISymbol, ProjectId?>> s_createTable = () => new ConditionalWeakTable<ISymbol, ProjectId?>();

    private readonly SourceGeneratedDocumentState? _frozenSourceGeneratedDocumentState;

    private SolutionState(
        bool partialSemanticsEnabled,
        ImmutableDictionary<ProjectId, ICompilationTracker> projectIdToTrackerMap,
        SourceGeneratedDocumentState? frozenSourceGeneratedDocument)
    {
        PartialSemanticsEnabled = partialSemanticsEnabled;
        _projectIdToTrackerMap = projectIdToTrackerMap;
        _frozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument;
    }

    public SolutionState(
        bool partialSemanticsEnabled)
        : this(
            partialSemanticsEnabled,
            projectIdToTrackerMap: ImmutableDictionary<ProjectId, ICompilationTracker>.Empty,
            frozenSourceGeneratedDocument: null)
    {
    }

    private SolutionCompilationState Branch(
        ImmutableDictionary<ProjectId, ICompilationTracker>? projectIdToTrackerMap = null,
        Optional<SourceGeneratedDocumentState?> frozenSourceGeneratedDocument = default)
    {
        projectIdToTrackerMap ??= _projectIdToTrackerMap;
        var newFrozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument.HasValue ? frozenSourceGeneratedDocument.Value : _frozenSourceGeneratedDocumentState;

        if (projectIdToTrackerMap == _projectIdToTrackerMap &&
            newFrozenSourceGeneratedDocumentState == _frozenSourceGeneratedDocumentState)
        {
            return this;
        }

        return new SolutionCompilationState(
            PartialSemanticsEnabled,
            projectIdToTrackerMap,
            newFrozenSourceGeneratedDocumentState);
    }

    /// <inheritdoc cref="SolutionState.ForkProject"/>
    private SolutionCompilationState ForkProject(
        ProjectState newProjectState,
        ProjectDependencyGraph newDependencyGraph,
        CompilationAndGeneratorDriverTranslationAction? translate,
        //ProjectDependencyGraph? newDependencyGraph = null,
        bool forkTracker)
    {
        var projectId = newProjectState.Id;

        var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
        // If we have a tracker for this project, then fork it as well (along with the
        // translation action and store it in the tracker map.
        if (newTrackerMap.TryGetValue(projectId, out var tracker))
        {
            newTrackerMap = newTrackerMap.Remove(projectId);

            if (forkTracker)
            {
                newTrackerMap = newTrackerMap.Add(projectId, tracker.Fork(newProjectState, translate));
            }
        }

        return this.Branch(
            projectIdToTrackerMap: newTrackerMap);
    }

    private ImmutableDictionary<ProjectId, ICompilationTracker> CreateCompilationTrackerMap(ProjectId changedProjectId, ProjectDependencyGraph dependencyGraph)
    {
        if (_projectIdToTrackerMap.Count == 0)
            return _projectIdToTrackerMap;

        using var _ = ArrayBuilder<KeyValuePair<ProjectId, ICompilationTracker>>.GetInstance(_projectIdToTrackerMap.Count, out var newTrackerInfo);
        var allReused = true;
        foreach (var (id, tracker) in _projectIdToTrackerMap)
        {
            var localTracker = tracker;
            if (!CanReuse(id))
            {
                localTracker = tracker.Fork(tracker.ProjectState, translate: null);
                allReused = false;
            }

            newTrackerInfo.Add(new KeyValuePair<ProjectId, ICompilationTracker>(id, localTracker));
        }

        if (allReused)
            return _projectIdToTrackerMap;

        return ImmutableDictionary.CreateRange(newTrackerInfo);

        // Returns true if 'tracker' can be reused for project 'id'
        bool CanReuse(ProjectId id)
        {
            if (id == changedProjectId)
            {
                return true;
            }

            return !dependencyGraph.DoesProjectTransitivelyDependOnProject(id, changedProjectId);
        }
    }

    /// <inheritdoc cref="SolutionState.AddProject(ProjectInfo)"/>
    public SolutionCompilationState AddProject(ProjectId projectId, ProjectDependencyGraph newDependencyGraph)
    {
        var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);

        return Branch(
            projectIdToTrackerMap: newTrackerMap);
    }

    /// <inheritdoc cref="SolutionState.RemoveProject(ProjectId)"/>
    public SolutionCompilationState RemoveProject(ProjectId projectId, ProjectDependencyGraph newDependencyGraph)
    {
        var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);

        return this.Branch(
            projectIdToTrackerMap: newTrackerMap.Remove(projectId));
    }

    /// <inheritdoc cref="SolutionState.WithProjectAssemblyName"/>
    public SolutionCompilationState WithProjectAssemblyName(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string assemblyName)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.ProjectAssemblyNameAction(assemblyName),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputFilePath"/>
    public SolutionCompilationState WithProjectOutputFilePath(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string? outputFilePath)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputRefFilePath"/>
    public SolutionCompilationState WithProjectOutputRefFilePath(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string? outputRefFilePath)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }
}
