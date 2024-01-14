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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
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

    // Lock for the partial compilation state listed below.
    private NonReentrantLock? _stateLockBackingField;
    private NonReentrantLock StateLock => LazyInitializer.EnsureInitialized(ref _stateLockBackingField, NonReentrantLock.Factory);

    private WeakReference<SolutionCompilationState>? _latestSolutionWithPartialCompilation;
    private DateTime _timeOfLatestSolutionWithPartialCompilation;
    private DocumentId? _documentIdOfLatestSolutionWithPartialCompilation;

    private SolutionCompilationState(
        SolutionState solution,
        bool partialSemanticsEnabled,
        ImmutableDictionary<ProjectId, ICompilationTracker> projectIdToTrackerMap,
        SourceGeneratedDocumentState? frozenSourceGeneratedDocument)
    {
        SolutionState = solution;
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

    public SolutionServices Services => this.SolutionState.Services;

    // Only run this in debug builds; even the .Any() call across all projects can be expensive when there's a lot of them.
    [Conditional("DEBUG")]
    private void CheckInvariants()
    {
        // An id shouldn't point at a tracker for a different project.
        Contract.ThrowIfTrue(_projectIdToTrackerMap.Any(kvp => kvp.Key != kvp.Value.ProjectState.Id));
    }

    public SourceGeneratedDocumentState? FrozenSourceGeneratedDocumentState => _frozenSourceGeneratedDocumentState;

    private SolutionCompilationState Branch(
        SolutionState newSolutionState,
        ImmutableDictionary<ProjectId, ICompilationTracker>? projectIdToTrackerMap = null,
        Optional<SourceGeneratedDocumentState?> frozenSourceGeneratedDocument = default)
    {
        projectIdToTrackerMap ??= _projectIdToTrackerMap;
        var newFrozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument.HasValue ? frozenSourceGeneratedDocument.Value : _frozenSourceGeneratedDocumentState;

        if (newSolutionState == this.SolutionState &&
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
    private SolutionCompilationState ForkProject(
        StateChange stateChange,
        Func<StateChange, CompilationAndGeneratorDriverTranslationAction?>? translate,
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
        Func<StateChange, TArg, CompilationAndGeneratorDriverTranslationAction?> translate,
        bool forkTracker,
        TArg arg)
    {
        // If the solution didn't actually change, there's no need to change us.
        if (stateChange.NewSolutionState == this.SolutionState)
            return this;

        return ForceForkProject(stateChange, translate.Invoke(stateChange, arg), forkTracker);
    }

    /// <summary>
    /// Same as <see cref="ForkProject(StateChange, Func{StateChange, CompilationAndGeneratorDriverTranslationAction?}?,
    /// bool)"/> except that it will still fork even if newSolutionState is unchanged from <see cref="SolutionState"/>.
    /// </summary>
    private SolutionCompilationState ForceForkProject(
        StateChange stateChange,
        CompilationAndGeneratorDriverTranslationAction? translate,
        bool forkTracker)
    {
        var newSolutionState = stateChange.NewSolutionState;
        var newProjectState = stateChange.NewProjectState;
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
    public SolutionCompilationState AddProject(ProjectInfo projectInfo)
    {
        var newSolutionState = this.SolutionState.AddProject(projectInfo);
        var newTrackerMap = CreateCompilationTrackerMap(projectInfo.Id, newSolutionState.GetProjectDependencyGraph());

        return Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap);
    }

    /// <inheritdoc cref="SolutionState.RemoveProject(ProjectId)"/>
    public SolutionCompilationState RemoveProject(ProjectId projectId)
    {
        var newSolutionState = this.SolutionState.RemoveProject(projectId);
        var newTrackerMap = CreateCompilationTrackerMap(projectId, newSolutionState.GetProjectDependencyGraph());

        return this.Branch(
            newSolutionState,
            projectIdToTrackerMap: newTrackerMap.Remove(projectId));
    }

    /// <inheritdoc cref="SolutionState.WithProjectAssemblyName"/>
    public SolutionCompilationState WithProjectAssemblyName(
        ProjectId projectId, string assemblyName)
    {
        return ForkProject(
            this.SolutionState.WithProjectAssemblyName(projectId, assemblyName),
            static (stateChange, assemblyName) => new CompilationAndGeneratorDriverTranslationAction.ProjectAssemblyNameAction(assemblyName),
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
            static stateChange => new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(stateChange.NewProjectState, isParseOptionChange: false),
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
        ProjectId projectId, CompilationOptions options)
    {
        return ForkProject(
            this.SolutionState.WithProjectCompilationOptions(projectId, options),
            static stateChange => new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(stateChange.NewProjectState, isAnalyzerConfigChange: false),
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithProjectParseOptions"/>
    public SolutionCompilationState WithProjectParseOptions(
        ProjectId projectId, ParseOptions options)
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
                static stateChange => new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(stateChange.NewProjectState, isParseOptionChange: true),
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
            static stateChange => new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(stateChange.NewProjectState, isParseOptionChange: false),
            forkTracker: true);
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
            static (stateChange, analyzerReferences) => new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(
                stateChange.OldProjectState.Language, referencesToAdd: analyzerReferences),
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
            static (stateChange, analyzerReference) => new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(
                stateChange.OldProjectState.Language, referencesToRemove: ImmutableArray.Create(analyzerReference)),
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

                return new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(
                    stateChange.OldProjectState.Language, referencesToAdd: addedReferences, referencesToRemove: removedReferences);
            },
            forkTracker: true);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentName"/>
    public SolutionCompilationState WithDocumentName(
        DocumentId documentId, string name)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentName(documentId, name), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentFolders"/>
    public SolutionCompilationState WithDocumentFolders(
        DocumentId documentId, IReadOnlyList<string> folders)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentFolders(documentId, folders), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentFilePath"/>
    public SolutionCompilationState WithDocumentFilePath(
        DocumentId documentId, string? filePath)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentFilePath(documentId, filePath), documentId);
    }

    /// <inheritdoc cref="SolutionState.WithDocumentText(DocumentId, SourceText, PreservationMode)"/>
    public SolutionCompilationState WithDocumentText(
        DocumentId documentId, SourceText text, PreservationMode mode)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentText(documentId, text, mode), documentId);
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

    /// <inheritdoc cref="SolutionState.WithDocumentSyntaxRoot"/>
    public SolutionCompilationState WithDocumentSyntaxRoot(
        DocumentId documentId, SyntaxNode root, PreservationMode mode)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentSyntaxRoot(documentId, root, mode), documentId);
    }

    public SolutionCompilationState WithDocumentContentsFrom(
        DocumentId documentId, DocumentState documentState)
    {
        return UpdateDocumentState(
            this.SolutionState.WithDocumentContentsFrom(documentId, documentState), documentId);
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

                return new CompilationAndGeneratorDriverTranslationAction.TouchDocumentAction(oldDocument, newDocument);
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

                return new CompilationAndGeneratorDriverTranslationAction.TouchAdditionalDocumentAction(oldDocument, newDocument);
            },
            forkTracker: true,
            arg: documentId);
    }

    private SolutionCompilationState UpdateAnalyzerConfigDocumentState(StateChange stateChange)
    {
        return ForkProject(
            stateChange,
            static stateChange => stateChange.NewProjectState.CompilationOptions != null
                ? new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(stateChange.NewProjectState, isAnalyzerConfigChange: true)
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

    private bool TryGetCompilationTracker(ProjectId projectId, [NotNullWhen(returnValue: true)] out ICompilationTracker? tracker)
        => _projectIdToTrackerMap.TryGetValue(projectId, out tracker);

    private static readonly Func<ProjectId, SolutionState, CompilationTracker> s_createCompilationTrackerFunction = CreateCompilationTracker;

    private static CompilationTracker CreateCompilationTracker(ProjectId projectId, SolutionState solution)
    {
        var projectState = solution.GetProjectState(projectId);
        Contract.ThrowIfNull(projectState);
        return new CompilationTracker(projectState);
    }

    private ICompilationTracker GetCompilationTracker(ProjectId projectId)
    {
        if (!_projectIdToTrackerMap.TryGetValue(projectId, out var tracker))
        {
            tracker = ImmutableInterlocked.GetOrAdd(ref _projectIdToTrackerMap, projectId, s_createCompilationTrackerFunction, this.SolutionState);
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
    public ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(
        ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetSourceGeneratedDocumentStatesAsync(this, cancellationToken)
            : new(TextDocumentStates<SourceGeneratedDocumentState>.Empty);
    }

    public ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
        ProjectState project, CancellationToken cancellationToken)
    {
        return project.SupportsCompilation
            ? GetCompilationTracker(project.Id).GetSourceGeneratorDiagnosticsAsync(this, cancellationToken)
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
        return GetCompilationTracker(documentId.ProjectId).TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
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
                var compilation = await tracker.GetCompilationAsync(this, cancellationToken).ConfigureAwait(false);
                return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
            }

            // otherwise get a metadata only image reference that is built by emitting the metadata from the
            // referenced project's compilation and re-importing it.
            using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_GetMetadataOnlyImage, cancellationToken))
            {
                var properties = new MetadataReferenceProperties(aliases: projectReference.Aliases, embedInteropTypes: projectReference.EmbedInteropTypes);
                return await tracker.SkeletonReferenceCache.GetOrBuildReferenceAsync(
                    tracker, this, properties, cancellationToken).ConfigureAwait(false);
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
            var tracker = this.GetCompilationTracker(projectReference.ProjectId);
            return GetMetadataReferenceAsync(tracker, fromProject, projectReference, cancellationToken);
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
    public SolutionCompilationState WithoutFrozenSourceGeneratedDocuments()
    {
        // If there's nothing frozen, there's nothing to do.
        if (_frozenSourceGeneratedDocumentState == null)
            return this;

        var projectId = _frozenSourceGeneratedDocumentState.Id.ProjectId;

        // Since we previously froze this document, we should have a CompilationTracker entry for it, and it should be a
        // GeneratedFileReplacingCompilationTracker. To undo the operation, we'll just restore the original CompilationTracker.
        var newTrackerMap = CreateCompilationTrackerMap(projectId, this.SolutionState.GetProjectDependencyGraph());
        Contract.ThrowIfFalse(newTrackerMap.TryGetValue(projectId, out var existingTracker));
        var replacingItemTracker = existingTracker as GeneratedFileReplacingCompilationTracker;
        Contract.ThrowIfNull(replacingItemTracker);
        newTrackerMap = newTrackerMap.SetItem(projectId, replacingItemTracker.UnderlyingTracker);

        return this.Branch(
            // TODO(cyrusn): Is it ok to preserve the same solution here?
            this.SolutionState,
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

        var existingGeneratedState = TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentIdentity.DocumentId);
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
            var projectState = this.SolutionState.GetRequiredProjectState(documentIdentity.DocumentId.ProjectId);
            newGeneratedState = SourceGeneratedDocumentState.Create(
                documentIdentity,
                sourceText,
                projectState.ParseOptions!,
                projectState.LanguageServices,
                // Just compute the checksum from the source text passed in.
                originalSourceTextChecksum: null);
        }

        var projectId = documentIdentity.DocumentId.ProjectId;
        var newTrackerMap = CreateCompilationTrackerMap(projectId, this.SolutionState.GetProjectDependencyGraph());

        // We want to create a new snapshot with a new compilation tracker that will do this replacement.
        // If we already have an existing tracker we'll just wrap that (so we also are reusing any underlying
        // computations). If we don't have one, we'll create one and then wrap it.
        if (!newTrackerMap.TryGetValue(projectId, out var existingTracker))
        {
            existingTracker = CreateCompilationTracker(projectId, this.SolutionState);
        }

        newTrackerMap = newTrackerMap.SetItem(
            projectId,
            new GeneratedFileReplacingCompilationTracker(existingTracker, newGeneratedState));

        return this.Branch(
            // TODO(cyrusn): Is it ok to just pass this.Solution along here?
            this.SolutionState,
            projectIdToTrackerMap: newTrackerMap,
            frozenSourceGeneratedDocument: newGeneratedState);
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
    /// Creates a branch of the solution that has its compilations frozen in whatever state they are in at the time, assuming a background compiler is
    /// busy building this compilations.
    ///
    /// A compilation for the project containing the specified document id will be guaranteed to exist with at least the syntax tree for the document.
    ///
    /// This not intended to be the public API, use Document.WithFrozenPartialSemantics() instead.
    /// </summary>
    public SolutionCompilationState WithFrozenPartialCompilationIncludingSpecificDocument(
        DocumentId documentId, CancellationToken cancellationToken)
    {
        try
        {
            var allDocumentIds = this.SolutionState.GetRelatedDocumentIds(documentId);
            using var _ = ArrayBuilder<(DocumentState, SyntaxTree)>.GetInstance(allDocumentIds.Length, out var builder);

            foreach (var currentDocumentId in allDocumentIds)
            {
                var document = this.SolutionState.GetRequiredDocumentState(currentDocumentId);
                builder.Add((document, document.GetSyntaxTree(cancellationToken)));
            }

            using (this.StateLock.DisposableWait(cancellationToken))
            {
                // in progress solutions are disabled for some testing
                if (Services.GetService<IWorkspacePartialSolutionsTestHook>()?.IsPartialSolutionDisabled == true)
                {
                    return this;
                }

                SolutionCompilationState? currentPartialSolution = null;
                _latestSolutionWithPartialCompilation?.TryGetTarget(out currentPartialSolution);

                var reuseExistingPartialSolution =
                    (DateTime.UtcNow - _timeOfLatestSolutionWithPartialCompilation).TotalSeconds < 0.1 &&
                    _documentIdOfLatestSolutionWithPartialCompilation == documentId;

                if (reuseExistingPartialSolution && currentPartialSolution != null)
                {
                    SolutionLogger.UseExistingPartialSolution();
                    return currentPartialSolution;
                }

                var newIdToProjectStateMap = this.SolutionState.ProjectStates;
                var newIdToTrackerMap = _projectIdToTrackerMap;

                foreach (var (doc, tree) in builder)
                {
                    // if we don't have one or it is stale, create a new partial solution
                    var tracker = this.GetCompilationTracker(doc.Id.ProjectId);
                    var newTracker = tracker.FreezePartialStateWithTree(this, doc, tree, cancellationToken);

                    Contract.ThrowIfFalse(newIdToProjectStateMap.ContainsKey(doc.Id.ProjectId));
                    newIdToProjectStateMap = newIdToProjectStateMap.SetItem(doc.Id.ProjectId, newTracker.ProjectState);
                    newIdToTrackerMap = newIdToTrackerMap.SetItem(doc.Id.ProjectId, newTracker);
                }

                var newState = this.SolutionState.Branch(
                    idToProjectStateMap: newIdToProjectStateMap,
                    dependencyGraph: SolutionState.CreateDependencyGraph(this.SolutionState.ProjectIds, newIdToProjectStateMap));
                var newCompilationState = this.Branch(
                    newState,
                    newIdToTrackerMap);

                _latestSolutionWithPartialCompilation = new WeakReference<SolutionCompilationState>(newCompilationState);
                _timeOfLatestSolutionWithPartialCompilation = DateTime.UtcNow;
                _documentIdOfLatestSolutionWithPartialCompilation = documentId;

                SolutionLogger.CreatePartialSolution();
                return newCompilationState;
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public SolutionCompilationState AddDocuments(ImmutableArray<DocumentInfo> documentInfos)
    {
        return AddDocumentsToMultipleProjects(documentInfos,
            static (documentInfo, project) => project.CreateDocument(documentInfo, project.ParseOptions, new LoadTextOptions(project.ChecksumAlgorithm)),
            static (oldProject, documents) => (oldProject.AddDocuments(documents), new CompilationAndGeneratorDriverTranslationAction.AddDocumentsAction(documents)));
    }

    public SolutionCompilationState AddAdditionalDocuments(ImmutableArray<DocumentInfo> documentInfos)
    {
        return AddDocumentsToMultipleProjects(documentInfos,
            static (documentInfo, project) => new AdditionalDocumentState(project.LanguageServices.SolutionServices, documentInfo, new LoadTextOptions(project.ChecksumAlgorithm)),
            static (projectState, documents) => (projectState.AddAdditionalDocuments(documents), new CompilationAndGeneratorDriverTranslationAction.AddAdditionalDocumentsAction(documents)));
    }

    public SolutionCompilationState AddAnalyzerConfigDocuments(ImmutableArray<DocumentInfo> documentInfos)
    {
        return AddDocumentsToMultipleProjects(documentInfos,
            static (documentInfo, project) => new AnalyzerConfigDocumentState(project.LanguageServices.SolutionServices, documentInfo, new LoadTextOptions(project.ChecksumAlgorithm)),
            static (oldProject, documents) =>
            {
                var newProject = oldProject.AddAnalyzerConfigDocuments(documents);
                return (newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true));
            });
    }

    public SolutionCompilationState RemoveDocuments(ImmutableArray<DocumentId> documentIds)
    {
        return RemoveDocumentsFromMultipleProjects(documentIds,
            static (projectState, documentId) => projectState.DocumentStates.GetRequiredState(documentId),
            static (projectState, documentIds, documentStates) => (projectState.RemoveDocuments(documentIds), new CompilationAndGeneratorDriverTranslationAction.RemoveDocumentsAction(documentStates)));
    }

    public SolutionCompilationState RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
    {
        return RemoveDocumentsFromMultipleProjects(documentIds,
            static (projectState, documentId) => projectState.AdditionalDocumentStates.GetRequiredState(documentId),
            static (projectState, documentIds, documentStates) => (projectState.RemoveAdditionalDocuments(documentIds), new CompilationAndGeneratorDriverTranslationAction.RemoveAdditionalDocumentsAction(documentStates)));
    }

    public SolutionCompilationState RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
    {
        return RemoveDocumentsFromMultipleProjects(documentIds,
            static (projectState, documentId) => projectState.AnalyzerConfigDocumentStates.GetRequiredState(documentId),
            static (oldProject, documentIds, _) =>
            {
                var newProject = oldProject.RemoveAnalyzerConfigDocuments(documentIds);
                return (newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true));
            });
    }

    /// <summary>
    /// Core helper that takes a set of <see cref="DocumentInfo" />s and does the application of the appropriate documents to each project.
    /// </summary>
    /// <param name="documentInfos">The set of documents to add.</param>
    /// <param name="addDocumentsToProjectState">Returns the new <see cref="ProjectState"/> with the documents added,
    /// and the <see cref="SolutionCompilationState.CompilationAndGeneratorDriverTranslationAction"/> needed as
    /// well.</param>
    private SolutionCompilationState AddDocumentsToMultipleProjects<T>(
        ImmutableArray<DocumentInfo> documentInfos,
        Func<DocumentInfo, ProjectState, T> createDocumentState,
        Func<ProjectState, ImmutableArray<T>, (ProjectState newState, CompilationAndGeneratorDriverTranslationAction translationAction)> addDocumentsToProjectState)
        where T : TextDocumentState
    {
        if (documentInfos.IsDefault)
            throw new ArgumentNullException(nameof(documentInfos));

        if (documentInfos.IsEmpty)
            return this;

        // The documents might be contributing to multiple different projects; split them by project and then we'll process
        // project-at-a-time.
        var documentInfosByProjectId = documentInfos.ToLookup(d => d.Id.ProjectId);

        var newCompilationState = this;

        foreach (var documentInfosInProject in documentInfosByProjectId)
        {
            this.SolutionState.CheckContainsProject(documentInfosInProject.Key);
            var oldProjectState = this.SolutionState.GetProjectState(documentInfosInProject.Key)!;

            var newDocumentStatesForProjectBuilder = ArrayBuilder<T>.GetInstance();

            foreach (var documentInfo in documentInfosInProject)
            {
                newDocumentStatesForProjectBuilder.Add(createDocumentState(documentInfo, oldProjectState));
            }

            var newDocumentStatesForProject = newDocumentStatesForProjectBuilder.ToImmutableAndFree();

            var (newProjectState, compilationTranslationAction) = addDocumentsToProjectState(oldProjectState, newDocumentStatesForProject);

            var stateChange = newCompilationState.SolutionState.ForkProject(
                oldProjectState,
                newProjectState,
                // intentionally accessing this.Solution here not newSolutionState
                newFilePathToDocumentIdsMap: this.SolutionState.CreateFilePathToDocumentIdsMapWithAddedDocuments(newDocumentStatesForProject));

            newCompilationState = newCompilationState.ForkProject(
                stateChange,
                static (_, compilationTranslationAction) => compilationTranslationAction,
                forkTracker: true,
                arg: compilationTranslationAction);
        }

        return newCompilationState;
    }

    private SolutionCompilationState RemoveDocumentsFromMultipleProjects<T>(
        ImmutableArray<DocumentId> documentIds,
        Func<ProjectState, DocumentId, T> getExistingTextDocumentState,
        Func<ProjectState, ImmutableArray<DocumentId>, ImmutableArray<T>, (ProjectState newState, SolutionCompilationState.CompilationAndGeneratorDriverTranslationAction translationAction)> removeDocumentsFromProjectState)
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
            var oldProjectState = this.SolutionState.GetProjectState(documentIdsInProject.Key);

            if (oldProjectState == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentIdsInProject.Key));
            }

            var removedDocumentStatesBuilder = ArrayBuilder<T>.GetInstance();

            foreach (var documentId in documentIdsInProject)
            {
                removedDocumentStatesBuilder.Add(getExistingTextDocumentState(oldProjectState, documentId));
            }

            var removedDocumentStatesForProject = removedDocumentStatesBuilder.ToImmutableAndFree();

            var (newProjectState, compilationTranslationAction) = removeDocumentsFromProjectState(oldProjectState, documentIdsInProject.ToImmutableArray(), removedDocumentStatesForProject);

            var stateChange = newCompilationState.SolutionState.ForkProject(
                oldProjectState,
                newProjectState,
                // Intentionally using this.Solution here and not newSolutionState
                newFilePathToDocumentIdsMap: this.SolutionState.CreateFilePathToDocumentIdsMapWithRemovedDocuments(removedDocumentStatesForProject));

            newCompilationState = newCompilationState.ForkProject(
                stateChange,
                static (_, compilationTranslationAction) => compilationTranslationAction,
                forkTracker: true,
                arg: compilationTranslationAction);
        }

        return newCompilationState;
    }

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
            translate: new CompilationAndGeneratorDriverTranslationAction.ReplaceGeneratorDriverAction(
                tracker.GeneratorDriver,
                newProjectState: projectToUpdateState),
            forkTracker: true);

        return newCompilationState;
    }

    /// <summary>
    /// Creates a new solution instance with all the documents specified updated to have the same specified text.
    /// </summary>
    public SolutionCompilationState WithDocumentText(IEnumerable<DocumentId?> documentIds, SourceText text, PreservationMode mode)
    {
        var result = this;

        foreach (var documentId in documentIds)
        {
            // This API has always allowed null document IDs and documents IDs not contained within the solution. So
            // skip those if we run into that (otherwise the call to WithDocumentText will throw, as it is more
            // restrictive).
            if (documentId is null)
                continue;

            var documentState = this.SolutionState.GetProjectState(documentId.ProjectId)?.DocumentStates.GetState(documentId);
            if (documentState != null)
                result = result.WithDocumentText(documentId, text, mode);
        }

        return result;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SolutionCompilationState compilationState)
    {
        public GeneratorDriver? GetGeneratorDriver(Project project)
            => project.SupportsCompilation ? compilationState.GetCompilationTracker(project.Id).GeneratorDriver : null;
    }
}
