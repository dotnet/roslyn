// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public readonly struct SolutionChanges
{
    private readonly Solution _newSolution;
    private readonly Solution _oldSolution;

    internal Solution OldSolution => _oldSolution;
    internal Solution NewSolution => _newSolution;

    internal SolutionChanges(Solution newSolution, Solution oldSolution)
    {
        _newSolution = newSolution;
        _oldSolution = oldSolution;
    }

    public IEnumerable<Project> GetAddedProjects()
    {
        foreach (var id in _newSolution.ProjectIds)
        {
            if (!_oldSolution.ContainsProject(id))
            {
                yield return _newSolution.GetRequiredProject(id);
            }
        }
    }

    public IEnumerable<ProjectChanges> GetProjectChanges()
    {
        var old = _oldSolution;

        // if the project states are different then there is a change.
        foreach (var id in _newSolution.ProjectIds)
        {
            var newState = _newSolution.GetProjectState(id);
            var oldState = old.GetProjectState(id);
            if (oldState != null && newState != null && newState != oldState)
            {
                yield return _newSolution.GetRequiredProject(id).GetChanges(_oldSolution.GetRequiredProject(id));
            }
        }
    }

    public IEnumerable<Project> GetRemovedProjects()
    {
        foreach (var id in _oldSolution.ProjectIds)
        {
            if (!_newSolution.ContainsProject(id))
            {
                yield return _oldSolution.GetRequiredProject(id);
            }
        }
    }

    public IEnumerable<AnalyzerReference> GetAddedAnalyzerReferences()
    {
        var oldAnalyzerReferences = new HashSet<AnalyzerReference>(_oldSolution.AnalyzerReferences);
        foreach (var analyzerReference in _newSolution.AnalyzerReferences)
        {
            if (!oldAnalyzerReferences.Contains(analyzerReference))
            {
                yield return analyzerReference;
            }
        }
    }

    public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
    {
        var newAnalyzerReferences = new HashSet<AnalyzerReference>(_newSolution.AnalyzerReferences);
        foreach (var analyzerReference in _oldSolution.AnalyzerReferences)
        {
            if (!newAnalyzerReferences.Contains(analyzerReference))
            {
                yield return analyzerReference;
            }
        }
    }

    /// <summary>
    /// Gets changed source generated document ids that were modified with <see cref="Solution.WithFrozenSourceGeneratedDocuments(System.Collections.Immutable.ImmutableArray{ValueTuple{SourceGeneratedDocumentIdentity, DateTime, Text.SourceText}})"/>
    /// </summary>
    /// <remarks>
    /// It is possible for a source generated document to be "frozen" without it existing in the solution, and in that case
    /// this method will not return that document. This only returns changes to source generated documents, hence they had
    /// to already be observed in the old solution.
    /// </remarks>
    internal IEnumerable<DocumentId> GetExplicitlyChangedSourceGeneratedDocuments()
    {
        if (_newSolution.CompilationState.FrozenSourceGeneratedDocumentStates.IsEmpty)
            return [];

        using var _ = ArrayBuilder<SourceGeneratedDocumentState>.GetInstance(out var oldStateBuilder);
        foreach (var (id, _) in _newSolution.CompilationState.FrozenSourceGeneratedDocumentStates.States)
        {
            var oldState = _oldSolution.CompilationState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(id);
            oldStateBuilder.AddIfNotNull(oldState);
        }

        var oldStates = new TextDocumentStates<SourceGeneratedDocumentState>(oldStateBuilder);
        return _newSolution.CompilationState.FrozenSourceGeneratedDocumentStates.GetChangedStateIds(
            oldStates,
            ignoreUnchangedContent: true,
            ignoreUnchangeableDocuments: false);
    }
}
