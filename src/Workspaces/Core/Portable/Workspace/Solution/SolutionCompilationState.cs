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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
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
    public SolutionState Solution { get; }

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

    private SolutionCompilationState(
        SolutionState solution,
        bool partialSemanticsEnabled,
        ImmutableDictionary<ProjectId, ICompilationTracker> projectIdToTrackerMap,
        SourceGeneratedDocumentState? frozenSourceGeneratedDocument)
    {
        Solution = solution;
        PartialSemanticsEnabled = partialSemanticsEnabled;
        _projectIdToTrackerMap = projectIdToTrackerMap;
        _frozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument;

        // when solution state is changed, we recalculate its checksum
        _lazyChecksums = AsyncLazy.Create(c => ComputeChecksumsAsync(projectsToInclude: null, c));

        CheckInvariants();
    }

    public SolutionCompilationState(
        SolutionState solution,
        bool partialSemanticsEnabled)
        : this(
              solution,
              partialSemanticsEnabled,
              projectIdToTrackerMap: ImmutableDictionary<ProjectId, ICompilationTracker>.Empty,
              frozenSourceGeneratedDocument: null)
    {
    }

    public ImmutableDictionary<ProjectId, ICompilationTracker> ProjectIdToTrackerMap
        => _projectIdToTrackerMap;

    private void CheckInvariants()
    {
        // Only run this in debug builds; even the .Any() call across all projects can be expensive when there's a lot of them.
#if DEBUG
        // An id shouldn't point at a tracker for a different project.
        Contract.ThrowIfTrue(_projectIdToTrackerMap.Any(kvp => kvp.Key != kvp.Value.ProjectState.Id));
#endif
    }

    public SourceGeneratedDocumentState? FrozenSourceGeneratedDocumentState => _frozenSourceGeneratedDocumentState;

    internal SolutionCompilationState Branch(
        SolutionState newSolutionState,
        ImmutableDictionary<ProjectId, ICompilationTracker>? projectIdToTrackerMap = null,
        Optional<SourceGeneratedDocumentState?> frozenSourceGeneratedDocument = default)
    {
        projectIdToTrackerMap ??= _projectIdToTrackerMap;
        var newFrozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument.HasValue ? frozenSourceGeneratedDocument.Value : _frozenSourceGeneratedDocumentState;

        if (newSolutionState == this.Solution &&
            projectIdToTrackerMap == _projectIdToTrackerMap &&
            newFrozenSourceGeneratedDocumentState == _frozenSourceGeneratedDocumentState)
        {
            return this;
        }

        return new SolutionCompilationState(
            newSolutionState,
            PartialSemanticsEnabled,
            projectIdToTrackerMap,
            newFrozenSourceGeneratedDocumentState);
    }

    /// <inheritdoc cref="SolutionState.ForkProject"/>
    public SolutionCompilationState ForkProject(
        (SolutionState newSolutionState, ProjectState newProjectState) stateTuple,
        CompilationAndGeneratorDriverTranslationAction? translate,
        //ProjectDependencyGraph? newDependencyGraph = null,
        bool forkTracker)
    {
        return ForkProject(
            stateTuple.newSolutionState,
            stateTuple.newProjectState,
            translate,
            forkTracker);
    }

    /// <inheritdoc cref="SolutionState.ForkProject"/>
    public SolutionCompilationState ForkProject(
        SolutionState newSolutionState,
        ProjectState newProjectState,
        CompilationAndGeneratorDriverTranslationAction? translate,
        //ProjectDependencyGraph? newDependencyGraph = null,
        bool forkTracker)
    {
        // If the spsolution didn't actually change, there's no need to change us.
        if (newSolutionState == this.Solution)
            return this;

        var projectId = newProjectState.Id;

        var newDependencyGraph = newSolutionState.GetProjectDependencyGraph();
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
            newSolutionState,
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
    public SolutionCompilationState AddProject(SolutionState newSolutionState, ProjectId projectId)
    {
        var newTrackerMap = CreateCompilationTrackerMap(projectId, newSolutionState.GetProjectDependencyGraph());

        return Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap);
    }

    /// <inheritdoc cref="SolutionState.RemoveProject(ProjectId)"/>
    public SolutionCompilationState RemoveProject(SolutionState newSolutionState, ProjectId projectId)
    {
        var newTrackerMap = CreateCompilationTrackerMap(projectId, newSolutionState.GetProjectDependencyGraph());

        return this.Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap.Remove(projectId));
    }

    /// <inheritdoc cref="SolutionState.WithProjectAssemblyName"/>
    public SolutionCompilationState WithProjectAssemblyName(
        (SolutionState newSolutionState, ProjectState newProject) tuple, string assemblyName)
    {
        return ForkProject(
            tuple,
            new CompilationAndGeneratorDriverTranslationAction.ProjectAssemblyNameAction(assemblyName),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputFilePath"/>
    public SolutionCompilationState WithProjectOutputFilePath(
        (SolutionState newSolutionState, ProjectState newProject) tuple, string? outputFilePath)
    {
        return ForkProject(
            tuple,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectOutputRefFilePath"/>
    public SolutionCompilationState WithProjectOutputRefFilePath(
        (SolutionState newSolutionState, ProjectState newProject) tuple, string? outputRefFilePath)
    {
        return ForkProject(
            tuple,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectCompilationOutputInfo(
        (SolutionState newSolutionState, ProjectState newProject) tuple, in CompilationOutputInfo info)
    {
        return ForkProject(
            tuple,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectCompilationOutputInfo"/>
    public SolutionCompilationState WithProjectDefaultNamespace(
        (SolutionState newSolutionState, ProjectState newProject) tuple, string? defaultNamespace)
    {
        return ForkProject(
            tuple,
            translate: null,
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectChecksumAlgorithm"/>
    public SolutionCompilationState WithProjectChecksumAlgorithm(
        (SolutionState newSolutionState, ProjectState newProject) tuple, SourceHashAlgorithm checksumAlgorithm)
    {
        return ForkProject(
            tuple,
            new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(tuple.newProject, isParseOptionChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectName"/>
    public SolutionCompilationState WithProjectName(
        (SolutionState newSolutionState, ProjectState newProject) tuple, string name)
    {
        return ForkProject(
            tuple,
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
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, ImmutableArray<AnalyzerReference> analyzerReferences)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToAdd: analyzerReferences),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.RemoveAnalyzerReference(ProjectId, AnalyzerReference)"/>
    public SolutionCompilationState RemoveAnalyzerReference(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, AnalyzerReference analyzerReference)
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

    /// <inheritdoc cref="SolutionState.WithAnalyzerConfigDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithAnalyzerConfigDocumentText(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateAnalyzerConfigDocumentState(newProject, newDependencyGraph);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithDocumentText(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.WithAdditionalDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithAdditionalDocumentText(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateAdditionalDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.WithAnalyzerConfigDocumentText(DocumentId, TextAndVersion, PreservationMode)"/>
    public SolutionCompilationState WithAnalyzerConfigDocumentText(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode)
    {
        return UpdateAnalyzerConfigDocumentState(
            newProject, newDependencyGraph);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentSyntaxRoot"/>
    public SolutionCompilationState WithDocumentSyntaxRoot(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, SyntaxNode root, PreservationMode mode)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    public SolutionCompilationState WithDocumentContentsFrom(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, DocumentState documentState)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentSourceCodeKind"/>
    public SolutionCompilationState WithDocumentSourceCodeKind(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, SourceCodeKind sourceCodeKind)
    {
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.UpdateDocumentTextLoader"/>
    public SolutionCompilationState UpdateDocumentTextLoader(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.UpdateAdditionalDocumentTextLoader"/>
    public SolutionCompilationState UpdateAdditionalDocumentTextLoader(
        ProjectState oldProject, ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        // Assumes that content has changed. User could have closed a doc without saving and we are loading text
        // from closed file with old content.
        return UpdateAdditionalDocumentState(
            oldProject, newProject, newDependencyGraph, documentId, contentChanged: true);
    }

    /// <inheritdoc cref="SolutionState.UpdateAnalyzerConfigDocumentTextLoader"/>
    public SolutionCompilationState UpdateAnalyzerConfigDocumentTextLoader(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph, DocumentId documentId, TextLoader loader, PreservationMode mode)
    {
        // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
        // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
        return UpdateAnalyzerConfigDocumentState(
            newProject, newDependencyGraph);
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

    private SolutionCompilationState UpdateAnalyzerConfigDocumentState(
        ProjectState newProject, ProjectDependencyGraph newDependencyGraph)
    {
        return ForkProject(
            newProject,
            newDependencyGraph,
            newProject.CompilationOptions != null
                ? new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true)
                : null,
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

    internal bool TryGetCompilationTracker(ProjectId projectId, [NotNullWhen(returnValue: true)] out ICompilationTracker? tracker)
        => _projectIdToTrackerMap.TryGetValue(projectId, out tracker);

    private static readonly Func<ProjectId, SolutionState, CompilationTracker> s_createCompilationTrackerFunction = CreateCompilationTracker;

    private static CompilationTracker CreateCompilationTracker(ProjectId projectId, SolutionState solution)
    {
        var projectState = solution.GetProjectState(projectId);
        Contract.ThrowIfNull(projectState);
        return new CompilationTracker(projectState);
    }

    internal ICompilationTracker GetCompilationTracker(ProjectId projectId)
    {
        if (!_projectIdToTrackerMap.TryGetValue(projectId, out var tracker))
        {
            tracker = ImmutableInterlocked.GetOrAdd(ref _projectIdToTrackerMap, projectId, s_createCompilationTrackerFunction, this.Solution);
        }

        return tracker;
    }

    public Task<VersionStamp> GetDependentVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentVersionAsync(this, cancellationToken);

    public Task<VersionStamp> GetDependentSemanticVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentSemanticVersionAsync(this, cancellationToken);

    public Task<Checksum> GetDependentChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
        => this.GetCompilationTracker(projectId).GetDependentChecksumAsync(cancellationToken);

    public bool TryGetCompilation(ProjectId projectId, [NotNullWhen(returnValue: true)] out Compilation? compilation)
    {
        solution.CheckContainsProject(projectId);
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
        return GetCompilationAsync(solution, solution.GetProjectState(projectId)!, cancellationToken);
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
            ? GetCompilationTracker(solution, project.Id).GetCompilationAsync(solution, this, cancellationToken).AsNullable()
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
            ? this.GetCompilationTracker(solution, project.Id).HasSuccessfullyLoadedAsync(solution, this, cancellationToken)
            : project.HasAllInformation ? SpecializedTasks.True : SpecializedTasks.False;
    }

    /// <summary>
    /// Returns the generated document states for source generated documents.
    /// </summary>
    public ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(
        ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(solution, project.Id).GetSourceGeneratedDocumentStatesAsync(solution, this, cancellationToken)
            : new(TextDocumentStates<SourceGeneratedDocumentState>.Empty);
    }

    public ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
        ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(solution, project.Id).GetSourceGeneratorDiagnosticsAsync(solution, this, cancellationToken)
            : new(ImmutableArray<Diagnostic>.Empty);
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
        return GetCompilationTracker(solution, documentId.ProjectId).TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
    }

    /// <summary>
    /// Attempt to get the best readily available compilation for the project. It may be a
    /// partially built compilation.
    /// </summary>
    private MetadataReference? GetPartialMetadataReference(
        ProjectReference projectReference,
        ProjectState fromProject)
    {
        // Try to get the compilation state for this project.  If it doesn't exist, don't do any
        // more work.
        if (!_projectIdToTrackerMap.TryGetValue(projectReference.ProjectId, out var state))
        {
            return null;
        }

        return state.GetPartialMetadataReference(fromProject, projectReference);
    }

    /// <summary>
    /// Get a metadata reference to this compilation info's compilation with respect to
    /// another project. For cross language references produce a skeletal assembly. If the
    /// compilation is not available, it is built. If a skeletal assembly reference is
    /// needed and does not exist, it is also built.
    /// </summary>
    private async Task<MetadataReference?> GetMetadataReferenceAsync(
        ICompilationTracker tracker, ProjectState fromProject, ProjectReference projectReference, CancellationToken cancellationToken)
    {
        try
        {
            // If same language then we can wrap the other project's compilation into a compilation reference
            if (tracker.ProjectState.LanguageServices == fromProject.LanguageServices)
            {
                // otherwise, base it off the compilation by building it first.
                var compilation = await tracker.GetCompilationAsync(solution, this, cancellationToken).ConfigureAwait(false);
                return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
            }

            // otherwise get a metadata only image reference that is built by emitting the metadata from the
            // referenced project's compilation and re-importing it.
            using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_GetMetadataOnlyImage, cancellationToken))
            {
                var properties = new MetadataReferenceProperties(aliases: projectReference.Aliases, embedInteropTypes: projectReference.EmbedInteropTypes);
                return await tracker.SkeletonReferenceCache.GetOrBuildReferenceAsync(
                    tracker, solution, this, properties, cancellationToken).ConfigureAwait(false);
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
        ProjectReference projectReference, ProjectState fromProject, CancellationToken cancellationToken)
    {
        try
        {
            // Get the compilation state for this project.  If it's not already created, then this
            // will create it.  Then force that state to completion and get a metadata reference to it.
            var tracker = this.GetCompilationTracker(solution, projectReference.ProjectId);
            return GetMetadataReferenceAsync(solution, tracker, fromProject, projectReference, cancellationToken);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Undoes the operation of <see cref="WithFrozenSourceGeneratedDocument"/>; any frozen source generated document is allowed
    /// to have it's real output again.
    /// </summary>
    public SolutionCompilationState WithoutFrozenSourceGeneratedDocuments(ProjectDependencyGraph dependencyGraph)
    {
        // If there's nothing frozen, there's nothing to do.
        if (_frozenSourceGeneratedDocumentState == null)
            return this;

        var projectId = _frozenSourceGeneratedDocumentState.Id.ProjectId;

        // Since we previously froze this document, we should have a CompilationTracker entry for it, and it should be a
        // GeneratedFileReplacingCompilationTracker. To undo the operation, we'll just restore the original CompilationTracker.
        var newTrackerMap = CreateCompilationTrackerMap(projectId, dependencyGraph);
        Contract.ThrowIfFalse(newTrackerMap.TryGetValue(projectId, out var existingTracker));
        var replacingItemTracker = existingTracker as GeneratedFileReplacingCompilationTracker;
        Contract.ThrowIfNull(replacingItemTracker);
        newTrackerMap = newTrackerMap.SetItem(projectId, replacingItemTracker.UnderlyingTracker);

        return this.Branch(
            projectIdToTrackerMap: newTrackerMap,
            frozenSourceGeneratedDocument: null);
    }

    /// <summary>
    /// Returns a new SolutionState that will always produce a specific output for a generated file. This is used only in the
    /// implementation of <see cref="TextExtensions.GetOpenDocumentInCurrentContextWithChanges"/> where if a user has a source
    /// generated file open, we need to make sure everything lines up.
    /// </summary>
    public SolutionCompilationState WithFrozenSourceGeneratedDocument(
        SourceGeneratedDocumentIdentity documentIdentity, SourceText sourceText)
    {
        // We won't support freezing multiple source generated documents at once. Although nothing in the implementation
        // of this method would have problems, this simplifies the handling of serializing this solution to out-of-proc.
        // Since we only produce these snapshots from an open document, there should be no way to observe this, so this assertion
        // also serves as a good check on the system. If down the road we need to support this, we can remove this check and
        // update the out-of-process serialization logic accordingly.
        Contract.ThrowIfTrue(_frozenSourceGeneratedDocumentState != null, "We shouldn't be calling WithFrozenSourceGeneratedDocument on a solution with a frozen source generated document.");

        var existingGeneratedState = TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(solution, documentIdentity.DocumentId);
        SourceGeneratedDocumentState newGeneratedState;

        if (existingGeneratedState != null)
        {
            newGeneratedState = existingGeneratedState
                .WithText(sourceText)
                .WithParseOptions(existingGeneratedState.ParseOptions);

            // If the content already matched, we can just reuse the existing state
            if (newGeneratedState == existingGeneratedState)
            {
                return this;
            }
        }
        else
        {
            var projectState = solution.GetRequiredProjectState(documentIdentity.DocumentId.ProjectId);
            newGeneratedState = SourceGeneratedDocumentState.Create(
                documentIdentity,
                sourceText,
                projectState.ParseOptions!,
                projectState.LanguageServices,
                // Just compute the checksum from the source text passed in.
                originalSourceTextChecksum: null);
        }

        var projectId = documentIdentity.DocumentId.ProjectId;
        var newTrackerMap = CreateCompilationTrackerMap(projectId, solution.GetProjectDependencyGraph());

        // We want to create a new snapshot with a new compilation tracker that will do this replacement.
        // If we already have an existing tracker we'll just wrap that (so we also are reusing any underlying
        // computations). If we don't have one, we'll create one and then wrap it.
        if (!newTrackerMap.TryGetValue(projectId, out var existingTracker))
        {
            existingTracker = CreateCompilationTracker(projectId, solution);
        }

        newTrackerMap = newTrackerMap.SetItem(
            projectId,
            new GeneratedFileReplacingCompilationTracker(existingTracker, newGeneratedState));

        return this.Branch(
            projectIdToTrackerMap: newTrackerMap,
            frozenSourceGeneratedDocument: newGeneratedState);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SolutionCompilationState solutionState)
    {
        public GeneratorDriver? GetGeneratorDriver(Project project)
            => project.SupportsCompilation ? solutionState.GetCompilationTracker(project.Solution.State, project.Id).GeneratorDriver : null;
    }
}
