// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectCompilationOutputInfo(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, in CompilationOutputInfo info)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectDefaultNamespace(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string? defaultNamespace)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectChecksumAlgorithm"/>
    public SolutionCompilationState WithProjectChecksumAlgorithm(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, SourceHashAlgorithm checksumAlgorithm)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(newProject, isParseOptionChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectName"/>
    public SolutionCompilationState WithProjectName(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string name)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectFilePath"/>
    public SolutionCompilationState WithProjectFilePath(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, string? filePath)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOptions"/>
    public SolutionCompilationState WithProjectCompilationOptions(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, CompilationOptions options)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectParseOptions"/>
    public SolutionCompilationState WithProjectParseOptions(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, ParseOptions options)
    {
        if (this.PartialSemanticsEnabled)
        {
            // don't fork tracker with queued action since access via partial semantics can become inconsistent (throw).
            // Since changing options is rare event, it is okay to start compilation building from scratch.
            return ForkProject(
                newProject,
                newDependencyGraph,
                translate: null,
                forkTracker: false);
        }
        else
        {
            return ForkProject(
                newProject,
                newDependencyGraph,
                new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(newProject, isParseOptionChange: true),
                forkTracker: true);
        }
    }

    /// <inheritdoc cref="SolutionState.WithHasAllInformation"/>
    public SolutionCompilationState WithHasAllInformation(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, bool hasAllInformation)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithRunAnalyzers"/>
    public SolutionCompilationState WithRunAnalyzers(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, bool runAnalyzers)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectDocumentsOrder"/>
    public SolutionCompilationState WithProjectDocumentsOrder(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, ImmutableList<DocumentId> documentIds)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(newProject, isParseOptionChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.AddProjectReferences"/>
    public SolutionCompilationState AddProjectReferences(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, IReadOnlyCollection<ProjectReference> projectReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveProjectReference"/>
    public SolutionCompilationState RemoveProjectReference(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, ProjectReference projectReference)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectReferences"/>
    public SolutionCompilationState WithProjectReferences(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, IReadOnlyList<ProjectReference> projectReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.AddMetadataReferences"/>
    public SolutionCompilationState AddMetadataReferences(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, IReadOnlyCollection<MetadataReference> metadataReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveMetadataReference"/>
    public SolutionCompilationState RemoveMetadataReference(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, MetadataReference metadataReference)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectMetadataReferences"/>
    public SolutionCompilationState WithProjectMetadataReferences(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, IReadOnlyList<MetadataReference> metadataReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.AddAnalyzerReferences(ProjectId, ImmutableArray{AnalyzerReference})"/>
    public SolutionCompilationState AddAnalyzerReferences(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, ImmutableArray<AnalyzerReference> analyzerReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToAdd: analyzerReferences),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveAnalyzerReference(ProjectId, AnalyzerReference)"/>
    public SolutionCompilationState RemoveAnalyzerReference(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, AnalyzerReference analyzerReference)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToRemove: ImmutableArray.Create(analyzerReference)),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectAnalyzerReferences"/>
    public SolutionCompilationState WithProjectAnalyzerReferences(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        // The .Except() methods here aren't going to terribly cheap, but the assumption is adding or removing just the generators
        // we changed, rather than creating an entire new generator driver from scratch and rerunning all generators, is cheaper
        // in the end. This was written without data backing up that assumption, so if a profile indicates to the contrary,
        // this could be changed.
        //
        // When we're comparing AnalyzerReferences, we'll compare with reference equality; AnalyzerReferences like AnalyzerFileReference
        // may implement their own equality, but that can result in things getting out of sync: two references that are value equal can still
        // have their own generator instances; it's important that as we're adding and removing references that are value equal that we
        // still update with the correct generator instances that are coming from the new reference that is actually held in the project state from above.
        // An alternative approach would be to call oldProject.WithAnalyzerReferences keeping all the references in there that are value equal the same,
        // but this avoids any surprises where other components calling WithAnalyzerReferences might not expect that.
        var addedReferences = newProject.AnalyzerReferences.Except<AnalyzerReference>(oldProject.AnalyzerReferences, ReferenceEqualityComparer.Instance).ToImmutableArray();
        var removedReferences = oldProject.AnalyzerReferences.Except<AnalyzerReference>(newProject.AnalyzerReferences, ReferenceEqualityComparer.Instance).ToImmutableArray();

        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToAdd: addedReferences, referencesToRemove: removedReferences),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentName"/>
    public SolutionCompilationState WithDocumentName(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, string name)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: false);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentFolders"/>
    public SolutionCompilationState WithDocumentFolders(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, IReadOnlyList<string> folders)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: false);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentFilePath"/>
    public SolutionCompilationState WithDocumentFilePath(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, string? filePath)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: false);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithDocumentText(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.WithAdditionalDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithAdditionalDocumentText(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateAdditionalDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    private SolutionCompilationState UpdateDocumentState(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, bool contentChanged)
    {
        // This method shouldn't have been called if the document has not changed.
        Debug.Assert(oldProject != newProject);

        var oldDocument = oldProject.DocumentStates.GetRequiredState(documentId);
        var newDocument = newProject.DocumentStates.GetRequiredState(documentId);

        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.TouchDocumentAction(oldDocument, newDocument),
            forkTracker: true);
    }

    private SolutionCompilationState UpdateAdditionalDocumentState(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, bool contentChanged)
    {
        // This method shouldn't have been called if the document has not changed.
        Debug.Assert(oldProject != newProject);

        var oldDocument = oldProject.AdditionalDocumentStates.GetRequiredState(documentId);
        var newDocument = newProject.AdditionalDocumentStates.GetRequiredState(documentId);

        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.TouchAdditionalDocumentAction(oldDocument, newDocument),
            forkTracker: true);
    }
}
