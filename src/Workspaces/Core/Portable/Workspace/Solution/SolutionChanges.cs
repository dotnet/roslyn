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
    internal Solution OldSolution { get; }
    internal Solution NewSolution { get; }

    internal SolutionChanges(Solution newSolution, Solution oldSolution)
    {
        NewSolution = newSolution;
        OldSolution = oldSolution;
    }

    public IEnumerable<Project> GetAddedProjects()
    {
        foreach (var id in NewSolution.ProjectIds)
        {
            if (!OldSolution.ContainsProject(id))
            {
                yield return NewSolution.GetRequiredProject(id);
            }
        }
    }

    public IEnumerable<ProjectChanges> GetProjectChanges()
    {
        var old = OldSolution;

        // if the project states are different then there is a change.
        foreach (var id in NewSolution.ProjectIds)
        {
            var newState = NewSolution.GetProjectState(id);
            var oldState = old.GetProjectState(id);
            if (oldState != null && newState != null && newState != oldState)
            {
                yield return NewSolution.GetRequiredProject(id).GetChanges(OldSolution.GetRequiredProject(id));
            }
        }
    }

    public IEnumerable<Project> GetRemovedProjects()
    {
        foreach (var id in OldSolution.ProjectIds)
        {
            if (!NewSolution.ContainsProject(id))
            {
                yield return OldSolution.GetRequiredProject(id);
            }
        }
    }

    public IEnumerable<AnalyzerReference> GetAddedAnalyzerReferences()
    {
        var oldAnalyzerReferences = new HashSet<AnalyzerReference>(OldSolution.AnalyzerReferences);
        foreach (var analyzerReference in NewSolution.AnalyzerReferences)
        {
            if (!oldAnalyzerReferences.Contains(analyzerReference))
            {
                yield return analyzerReference;
            }
        }
    }

    public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
    {
        var newAnalyzerReferences = new HashSet<AnalyzerReference>(NewSolution.AnalyzerReferences);
        foreach (var analyzerReference in OldSolution.AnalyzerReferences)
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
        if (NewSolution.CompilationState.FrozenSourceGeneratedDocumentStates.IsEmpty)
            return [];

        using var _ = ArrayBuilder<SourceGeneratedDocumentState>.GetInstance(out var oldStateBuilder);
        foreach (var (id, _) in NewSolution.CompilationState.FrozenSourceGeneratedDocumentStates.States)
        {
            var oldState = OldSolution.CompilationState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(id);
            oldStateBuilder.AddIfNotNull(oldState);
        }

        var oldStates = new TextDocumentStates<SourceGeneratedDocumentState>(oldStateBuilder);
        return NewSolution.CompilationState.FrozenSourceGeneratedDocumentStates.GetChangedStateIds(
            oldStates,
            ignoreUnchangedContent: true,
            ignoreUnchangeableDocuments: false);
    }
}
