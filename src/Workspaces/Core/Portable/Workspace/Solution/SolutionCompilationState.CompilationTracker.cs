// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionCompilationState
    {
        /// <summary>
        /// Tracks the changes made to a project and provides the facility to get a lazily built
        /// compilation for that project.  As the compilation is being built, the partial results are
        /// stored as well so that they can be used in the 'in progress' workspace snapshot.
        /// </summary>
        private partial class CompilationTracker : ICompilationTracker
        {
            private static readonly Func<ProjectState, string> s_logBuildCompilationAsync =
                state => string.Join(",", state.AssemblyName, state.DocumentStates.Count);

            public ProjectState ProjectState { get; }

            /// <summary>
            /// Access via the <see cref="ReadState"/> and <see cref="WriteState"/> methods.
            /// </summary>
            private CompilationTrackerState _stateDoNotAccessDirectly;

            // guarantees only one thread is building at a time
            private readonly SemaphoreSlim _buildLock = new(initialCount: 1);

            public SkeletonReferenceCache SkeletonReferenceCache { get; }

            /// <summary>
            /// Set via a feature flag to enable strict validation of the compilations that are produced, in that they match the original states. This validation is expensive, so we don't want it
            /// running in normal production scenarios.
            /// </summary>
            private readonly bool _validateStates;

            private CompilationTracker(
                ProjectState project,
                CompilationTrackerState state,
                SkeletonReferenceCache cachedSkeletonReferences)
            {
                Contract.ThrowIfNull(project);

                this.ProjectState = project;
                _stateDoNotAccessDirectly = state;
                this.SkeletonReferenceCache = cachedSkeletonReferences;

                _validateStates = project.LanguageServices.SolutionServices.GetRequiredService<IWorkspaceConfigurationService>().Options.ValidateCompilationTrackerStates;

                ValidateState(state);
            }

            /// <summary>
            /// Creates a tracker for the provided project.  The tracker will be in the 'empty' state
            /// and will have no extra information beyond the project itself.
            /// </summary>
            public CompilationTracker(ProjectState project)
                : this(project, CompilationTrackerState.Empty, cachedSkeletonReferences: new())
            {
            }

            private CompilationTrackerState ReadState()
                => Volatile.Read(ref _stateDoNotAccessDirectly);

            private void WriteState(CompilationTrackerState state)
            {
                Volatile.Write(ref _stateDoNotAccessDirectly, state);
                ValidateState(state);
            }

            public GeneratorDriver? GeneratorDriver
            {
                get
                {
                    var state = this.ReadState();
                    return state.GeneratorInfo.Driver;
                }
            }

            public bool ContainsAssemblyOrModuleOrDynamic(ISymbol symbol, bool primary)
            {
                Debug.Assert(symbol.Kind is SymbolKind.Assembly or
                             SymbolKind.NetModule or
                             SymbolKind.DynamicType);
                var state = this.ReadState();

                var unrootedSymbolSet = (state as FinalState)?.UnrootedSymbolSet;
                if (unrootedSymbolSet == null)
                {
                    // this was not a tracker that has handed out a compilation (all compilations handed out must be
                    // owned by a 'FinalState').  So this symbol could not be from us.
                    return false;
                }

                return unrootedSymbolSet.Value.ContainsAssemblyOrModuleOrDynamic(symbol, primary);
            }

            /// <summary>
            /// Creates a new instance of the compilation info, retaining any already built
            /// compilation state as the now 'old' state
            /// </summary>
            public ICompilationTracker Fork(
                ProjectState newProjectState,
                CompilationAndGeneratorDriverTranslationAction? translate)
            {
                return new CompilationTracker(
                    newProjectState,
                    ForkState(oldProjectState: this.ProjectState, ReadState(), translate),
                    this.SkeletonReferenceCache.Clone());

                static CompilationTrackerState ForkState(
                    ProjectState oldProjectState,
                    CompilationTrackerState state,
                    CompilationAndGeneratorDriverTranslationAction? translate)
                {
                    if (state is WithCompilationTrackerState withCompilationState)
                    {
                        var intermediateProjects = state is InProgressState inProgressState
                            ? inProgressState.IntermediateProjects
                            : [];
                        var finalCompilationWithGeneratedDocuments = state is FinalState finalState
                            ? finalState.FinalCompilationWithGeneratedDocuments
                            : null;

                        if (translate is not null)
                        {
                            // We have a translation action; are we able to merge it with the prior one?
                            var merged = false;
                            if (!intermediateProjects.IsEmpty)
                            {
                                var (priorState, priorAction) = intermediateProjects.Last();
                                var mergedTranslation = translate.TryMergeWithPrior(priorAction);
                                if (mergedTranslation != null)
                                {
                                    // We can replace the prior action with this new one
                                    intermediateProjects = intermediateProjects.SetItem(
                                        intermediateProjects.Count - 1,
                                        (oldState: priorState, mergedTranslation));
                                    merged = true;
                                }
                            }

                            if (!merged)
                            {
                                // Just add it to the end
                                intermediateProjects = intermediateProjects.Add((oldProjectState, translate));
                            }
                        }

                        var newState = CompilationTrackerState.Create(
                            withCompilationState.CompilationWithoutGeneratedDocuments,
                            state.GeneratorInfo,
                            finalCompilationWithGeneratedDocuments,
                            intermediateProjects);
                        return newState;
                    }
                    else if (state is NoCompilationState)
                    {
                        // We may still have a cached generator; we'll have to remember to run generators again since we are making some
                        // change here. We'll also need to update the other state of the driver if appropriate.

                        // The no compilation state can never be in the 'DocumentsAreFinal' state.  The only place where
                        // we start with the NoCompilationState is the 'Empty' instance (where DocumentsAreFinal=false).
                        // And then this is the only place where we get a NoCompilationState and create a new instance.
                        // So there is no way to ever transition this to the DocumentsAreFinal=true state.
                        Contract.ThrowIfTrue(state.GeneratorInfo.DocumentsAreFinal);
                        var generatorInfo = state.GeneratorInfo;
                        if (generatorInfo.Driver != null && translate != null)
                            generatorInfo = generatorInfo.WithDriver(translate.TransformGeneratorDriver(generatorInfo.Driver));

                        return new NoCompilationState(generatorInfo);
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(state.GetType());
                    }
                }
            }

            public ICompilationTracker FreezePartialStateWithTree(
                SolutionCompilationState compilationState,
                DocumentState docState,
                SyntaxTree tree,
                CancellationToken cancellationToken)
            {
                GetPartialCompilationState(
                    compilationState, docState.Id,
                    out var inProgressProject,
                    out var compilationPair,
                    out var generatorInfo,
                    out var metadataReferenceToProjectId,
                    cancellationToken);

                // Ensure we actually have the tree we need in there; note that if the tree is present, then we know the document must also be
                // present in inProgressProject, since those are both updated in parallel.
                //
                // the tree that we have been given was directly returned from the document state that we're also being passed --
                // the only reason we're requesting it earlier is this code is running under a lock in
                // SolutionState.WithFrozenPartialCompilationIncludingSpecificDocument.
                if (!compilationPair.CompilationWithoutGeneratedDocuments.ContainsSyntaxTree(tree))
                {
                    // We do not have the exact tree. It either means this document was recently added, or the tree was recently changed.
                    // We now need to update both the inProgressState and the compilation. There are several possibilities we want to consider:
                    //
                    // 1. An earlier version of the document is present in the compilation, and we just need to update it to the current version
                    // 2. The tree wasn't present in the original snapshot at all, and we just need to add the tree.
                    // 3. The tree wasn't present in the original snapshot, but an older file had been removed that had the same file path.
                    //    As a heuristic, we remove the old one so we don't end up with duplicate trees.
                    //
                    // Note it's possible that we simply had never tried to produce a compilation yet for this project at all, in that case
                    // GetPartialCompilationState would have produced an empty compilation, and it would have updated inProgressProject to
                    // remove all the documents. Thus, that is no different than the "add" case above.
                    if (inProgressProject.DocumentStates.TryGetState(docState.Id, out var oldState))
                    {
                        // Scenario 1. The document had been previously parsed and it's there, so we can update it with our current state
                        // This call should be instant, since the compilation already must exist that contains this tree. Note if no compilation existed
                        // GetPartialCompilationState would have produced an empty one, and removed any documents, so inProgressProject.DocumentStates would
                        // have been empty originally.
                        var oldTree = oldState.GetSyntaxTree(cancellationToken);

                        compilationPair = compilationPair.ReplaceSyntaxTree(oldTree, tree);
                        inProgressProject = inProgressProject.UpdateDocument(docState, contentChanged: true);
                    }
                    else
                    {
                        // We're in either scenario 2 or 3. Do we have an existing tree to try replacing? Note: the file path here corresponds to Document.FilePath.
                        // If a document's file path is null, we then substitute Document.Name, so we usually expect there to be a unique string regardless.
                        var oldTree = compilationPair.CompilationWithoutGeneratedDocuments.SyntaxTrees.FirstOrDefault(t => t.FilePath == tree.FilePath);
                        if (oldTree == null)
                        {
                            // Scenario 2.
                            compilationPair = compilationPair.AddSyntaxTree(tree);
                            inProgressProject = inProgressProject.AddDocuments([docState]);
                        }
                        else
                        {
                            // Scenario 3.
                            compilationPair = compilationPair.ReplaceSyntaxTree(oldTree, tree);

                            // The old tree came from some other document with a different ID then we started with -- if the document ID still existed we would have
                            // been in the Scenario 1 case instead. We'll find the old document ID, remove that state, and then add ours.
                            var oldDocumentId = DocumentState.GetDocumentIdForTree(oldTree);
                            Contract.ThrowIfNull(oldDocumentId, $"{nameof(oldTree)} came from the compilation produced by the workspace, so the document ID should have existed.");
                            inProgressProject = inProgressProject
                                .RemoveDocuments([oldDocumentId])
                                .AddDocuments([docState]);
                        }
                    }
                }

                // At this point, we now absolutely should have our tree in the compilation
                Contract.ThrowIfFalse(compilationPair.CompilationWithoutGeneratedDocuments.ContainsSyntaxTree(tree));

                // Mark whatever generator state we have as not only final, but frozen as well.  We'll want to keep
                // whatever we have here through whatever future transformations occur.
                generatorInfo = generatorInfo.WithDocumentsAreFinalAndFrozen();

                // The user is asking for an in progress snap.  We don't want to create it and then
                // have the compilation immediately disappear.  So we force it to stay around with a ConstantValueSource.
                // As a policy, all partial-state projects are said to have incomplete references, since the state has no guarantees.
                var finalState = FinalState.Create(
                    finalCompilationWithGeneratedDocuments: compilationPair.CompilationWithGeneratedDocuments,
                    compilationWithoutGeneratedDocuments: compilationPair.CompilationWithoutGeneratedDocuments,
                    hasSuccessfullyLoaded: false,
                    generatorInfo,
                    finalCompilation: compilationPair.CompilationWithGeneratedDocuments,
                    this.ProjectState.Id,
                    metadataReferenceToProjectId);

                return new CompilationTracker(inProgressProject, finalState, this.SkeletonReferenceCache.Clone());
            }

            /// <summary>
            /// Tries to get the latest snapshot of the compilation without waiting for it to be fully built. This
            /// method takes advantage of the progress side-effect produced during <see
            /// cref="GetOrBuildFinalStateAsync"/>. It will either return the already built compilation, any in-progress
            /// compilation or any known old compilation in that order of preference. The compilation state that is
            /// returned will have a compilation that is retained so that it cannot disappear.
            /// </summary>
            private void GetPartialCompilationState(
                SolutionCompilationState compilationState,
                DocumentId id,
                out ProjectState inProgressProject,
                out CompilationPair compilations,
                out CompilationTrackerGeneratorInfo generatorInfo,
                out Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId,
                CancellationToken cancellationToken)
            {
                var state = ReadState();
                var compilationWithoutGeneratedDocuments = state is WithCompilationTrackerState withCompilationState
                    ? withCompilationState.CompilationWithoutGeneratedDocuments
                    : null;

                // check whether we can bail out quickly for typing case
                var inProgressState = state as InProgressState;

                generatorInfo = state.GeneratorInfo;
                inProgressProject = inProgressState != null ? inProgressState.IntermediateProjects.First().oldState : this.ProjectState;

                // all changes left for this document is modifying the given document; since the compilation is already fully up to date
                // we don't need to do any further checking of it's references
                if (inProgressState != null &&
                    compilationWithoutGeneratedDocuments != null &&
                    inProgressState.IntermediateProjects.All(t => IsTouchDocumentActionForDocument(t.action, id)))
                {
                    // We'll add in whatever generated documents we do have; these may be from a prior run prior to some changes
                    // being made to the project, but it's the best we have so we'll use it.
                    compilations = new CompilationPair(
                        compilationWithoutGeneratedDocuments,
                        compilationWithoutGeneratedDocuments.AddSyntaxTrees(generatorInfo.Documents.States.Values.Select(state => state.GetSyntaxTree(cancellationToken))));

                    // This is likely a bug.  It seems possible to pass out a partial compilation state that we don't
                    // properly record assembly symbols for.
                    metadataReferenceToProjectId = null;
                    return;
                }

                // if we already have a final compilation we are done.
                if (compilationWithoutGeneratedDocuments != null && state is FinalState finalState)
                {
                    var finalCompilation = finalState.FinalCompilationWithGeneratedDocuments;
                    Contract.ThrowIfNull(finalCompilation, "We have a FinalState, so we must have a non-null final compilation");

                    compilations = new CompilationPair(compilationWithoutGeneratedDocuments, finalCompilation);

                    // This should hopefully be safe to return as null.  Because we already reached the 'FinalState'
                    // before, we should have already recorded the assembly symbols for it.  So not recording them
                    // again is likely ok (as long as compilations continue to return the same IAssemblySymbols for
                    // the same references across source edits).
                    metadataReferenceToProjectId = null;
                    return;
                }

                // 1) if we have an in-progress compilation use it.  
                // 2) If we don't, then create a simple empty compilation/project. 
                // 3) then, make sure that all it's p2p refs and whatnot are correct.
                if (compilationWithoutGeneratedDocuments == null)
                {
                    inProgressProject = inProgressProject.RemoveAllDocuments();
                    compilationWithoutGeneratedDocuments = CreateEmptyCompilation();
                }

                compilations = new CompilationPair(
                    compilationWithoutGeneratedDocuments,
                    compilationWithoutGeneratedDocuments.AddSyntaxTrees(generatorInfo.Documents.States.Values.Select(state => state.GetSyntaxTree(cancellationToken))));

                // Now add in back a consistent set of project references.  For project references
                // try to get either a CompilationReference or a SkeletonReference. This ensures
                // that the in-progress project only reports a reference to another project if it
                // could actually get a reference to that project's metadata.
                var metadataReferences = new List<MetadataReference>();
                var newProjectReferences = new List<ProjectReference>();
                metadataReferences.AddRange(this.ProjectState.MetadataReferences);

                metadataReferenceToProjectId = new Dictionary<MetadataReference, ProjectId>();

                foreach (var projectReference in this.ProjectState.ProjectReferences)
                {
                    var referencedProject = compilationState.SolutionState.GetProjectState(projectReference.ProjectId);
                    if (referencedProject != null)
                    {
                        if (referencedProject.IsSubmission)
                        {
                            var previousScriptCompilation = compilationState.GetCompilationAsync(
                                projectReference.ProjectId, cancellationToken).WaitAndGetResult(cancellationToken);

                            // previous submission project must support compilation:
                            RoslynDebug.Assert(previousScriptCompilation != null);

                            compilations = compilations.WithPreviousScriptCompilation(previousScriptCompilation);
                        }
                        else
                        {
                            // get the latest metadata for the partial compilation of the referenced project.
                            var metadata = compilationState.GetPartialMetadataReference(projectReference, this.ProjectState);

                            if (metadata == null)
                            {
                                // if we failed to get the metadata, check to see if we previously had existing metadata and reuse it instead.
                                var inProgressCompilationNotRef = compilations.CompilationWithGeneratedDocuments;
                                metadata = inProgressCompilationNotRef.ExternalReferences.FirstOrDefault(
                                    r => SolutionCompilationState.GetProjectId(inProgressCompilationNotRef.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol) == projectReference.ProjectId);
                            }

                            if (metadata != null)
                            {
                                newProjectReferences.Add(projectReference);
                                metadataReferences.Add(metadata);
                                metadataReferenceToProjectId.Add(metadata, projectReference.ProjectId);
                            }
                        }
                    }
                }

                inProgressProject = inProgressProject.WithProjectReferences(newProjectReferences);

                if (!Enumerable.SequenceEqual(compilations.CompilationWithoutGeneratedDocuments.ExternalReferences, metadataReferences))
                {
                    compilations = compilations.WithReferences(metadataReferences);
                }
            }

            private static bool IsTouchDocumentActionForDocument(CompilationAndGeneratorDriverTranslationAction action, DocumentId id)
                => action is CompilationAndGeneratorDriverTranslationAction.TouchDocumentAction touchDocumentAction &&
                   touchDocumentAction.DocumentId == id;

            /// <summary>
            /// Gets the final compilation if it is available.
            /// </summary>
            public bool TryGetCompilation([NotNullWhen(true)] out Compilation? compilation)
            {
                var state = ReadState();
                if (state is FinalState finalState)
                {
                    compilation = finalState.FinalCompilationWithGeneratedDocuments;
                    Contract.ThrowIfNull(compilation);
                    return true;
                }
                else
                {
                    compilation = null;
                    return false;
                }
            }

            public Task<Compilation> GetCompilationAsync(SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (this.TryGetCompilation(out var compilation))
                {
                    return Task.FromResult(compilation);
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    // Handle early cancellation here to avoid throwing/catching cancellation exceptions in the async
                    // state machines. This helps reduce the total number of First Chance Exceptions occurring in IDE
                    // typing scenarios.
                    return Task.FromCanceled<Compilation>(cancellationToken);
                }
                else
                {
                    return GetCompilationSlowAsync(compilationState, cancellationToken);
                }
            }

            private async Task<Compilation> GetCompilationSlowAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                var finalState = await GetOrBuildFinalStateAsync(compilationState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return finalState.FinalCompilationWithGeneratedDocuments;
            }

            private async Task<FinalState> GetOrBuildFinalStateAsync(
                SolutionCompilationState compilationState,
                CancellationToken cancellationToken)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.Workspace_Project_CompilationTracker_BuildCompilationAsync,
                                           s_logBuildCompilationAsync, ProjectState, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var state = ReadState();

                        // Try to get the built compilation.  If it exists, then we can just return that.
                        if (state is FinalState finalState)
                            return finalState;

                        // Otherwise, we actually have to build it.  Ensure that only one thread is trying to
                        // build this compilation at a time.
                        using (await _buildLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                        {
                            return await BuildFinalStateAsync(compilationState, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }

                // <summary>
                // Builds the compilation matching the project state. In the process of building, also
                // produce in progress snapshots that can be accessed from other threads.
                // </summary>
                async Task<FinalState> BuildFinalStateAsync(
                    SolutionCompilationState compilationState,
                    CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = ReadState();

                    return state switch
                    {
                        // if we already have a compilation, we must be already done!  This can happen if two
                        // threads were waiting to build, and we came in after the other succeeded.
                        FinalState finalState
                            => finalState,

                        // We've got nothing.  Build it from scratch :(
                        NoCompilationState noCompilationState
                            => await BuildFinalStateFromScratchAsync(
                                compilationState,
                                noCompilationState,
                                cancellationToken).ConfigureAwait(false),

                        // We have a declaration compilation, use it to reconstruct the final compilation
                        AllSyntaxTreesParsedState allSyntaxTreesParsedState
                            => await FinalizeCompilationAsync(
                                compilationState,
                                allSyntaxTreesParsedState,
                                compilationWithStaleGeneratedTrees: null,
                                cancellationToken).ConfigureAwait(false),

                        // We must have an in progress compilation. Build off of that.
                        InProgressState inProgressState
                            => await BuildFinalStateFromInProgressStateAsync(
                                compilationState,
                                inProgressState,
                                cancellationToken).ConfigureAwait(false),

                        _ => throw ExceptionUtilities.UnexpectedValue(state.GetType()),
                    };

                    async Task<FinalState> BuildFinalStateFromScratchAsync(
                        SolutionCompilationState compilationState,
                        NoCompilationState noCompilationState,
                        CancellationToken cancellationToken)
                    {
                        Contract.ThrowIfTrue(noCompilationState.GeneratorInfo.DocumentsAreFinal);

                        try
                        {
                            var allSyntaxTreesParsedState = await BuildAllSyntaxTreesParsedStateFromScratchAsync(
                                noCompilationState, cancellationToken).ConfigureAwait(false);

                            return await FinalizeCompilationAsync(
                                compilationState,
                                allSyntaxTreesParsedState,
                                compilationWithStaleGeneratedTrees: null,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    }

                    [PerformanceSensitive(
                        "https://github.com/dotnet/roslyn/issues/23582",
                        Constraint = "Avoid calling " + nameof(Compilation.AddSyntaxTrees) + " in a loop due to allocation overhead.")]
                    async Task<AllSyntaxTreesParsedState> BuildAllSyntaxTreesParsedStateFromScratchAsync(
                        NoCompilationState noCompilationState,
                        CancellationToken cancellationToken)
                    {
                        try
                        {
                            var compilation = CreateEmptyCompilation();

                            using var _ = ArrayBuilder<SyntaxTree>.GetInstance(ProjectState.DocumentStates.Count, out var trees);
                            foreach (var documentState in ProjectState.DocumentStates.GetStatesInCompilationOrder())
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                // Include the tree even if the content of the document failed to load.
                                trees.Add(await documentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                            }

                            compilation = compilation.AddSyntaxTrees(trees);
                            var allSyntaxTreesParsedState = new AllSyntaxTreesParsedState(compilation, noCompilationState.GeneratorInfo, compilationWithGeneratedDocuments: null);
                            WriteState(allSyntaxTreesParsedState);
                            return allSyntaxTreesParsedState;
                        }
                        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    }

                    async Task<FinalState> BuildFinalStateFromInProgressStateAsync(
                        SolutionCompilationState compilationState, InProgressState inProgressState, CancellationToken cancellationToken)
                    {
                        try
                        {
                            var allSyntaxTreesParsedState = await BuildAllSyntaxTreesParsedStateFromInProgressStateAsync(
                                inProgressState, cancellationToken).ConfigureAwait(false);

                            return await FinalizeCompilationAsync(
                                compilationState,
                                allSyntaxTreesParsedState,
                                allSyntaxTreesParsedState.CompilationWithGeneratedDocuments,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    }
                }
            }

            private Compilation CreateEmptyCompilation()
            {
                var compilationFactory = this.ProjectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                if (this.ProjectState.IsSubmission)
                {
                    return compilationFactory.CreateSubmissionCompilation(
                        this.ProjectState.AssemblyName,
                        this.ProjectState.CompilationOptions!,
                        this.ProjectState.HostObjectType);
                }
                else
                {
                    return compilationFactory.CreateCompilation(
                        this.ProjectState.AssemblyName,
                        this.ProjectState.CompilationOptions!);
                }
            }

            private async Task<AllSyntaxTreesParsedState> BuildAllSyntaxTreesParsedStateFromInProgressStateAsync(
                InProgressState state, CancellationToken cancellationToken)
            {
                try
                {
                    var compilationWithoutGeneratedDocuments = state.CompilationWithoutGeneratedDocuments;
                    var compilationWithGeneratedDocuments = state.CompilationWithGeneratedDocuments;
                    var generatorDriver = state.GeneratorInfo.Driver;

                    // If compilationWithGenerators is the same as compilationWithoutGenerators, then it means a prior run of generators
                    // didn't produce any files. In that case, we'll just make compilationWithGenerators null so we avoid doing any
                    // transformations of it multiple times. Otherwise the transformations below and in FinalizeCompilationAsync will try
                    // to update both at once, which is functionally fine but just unnecessary work. This function is always allowed to return
                    // null for compilationWithGenerators in the end, so there's no harm there.
                    if (compilationWithGeneratedDocuments == compilationWithoutGeneratedDocuments)
                    {
                        compilationWithGeneratedDocuments = null;
                    }

                    var intermediateProjects = state.IntermediateProjects;

                    // This is guaranteed by an in progress state.  Which means we know we'll get into the while loop below.
                    Contract.ThrowIfTrue(intermediateProjects.Count == 0);
                    AllSyntaxTreesParsedState? resultState = null;

                    while (!intermediateProjects.IsEmpty)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // We have a list of transformations to get to our final compilation; take the first transformation and apply it.
                        var (oldState, action) = intermediateProjects[0];

                        compilationWithoutGeneratedDocuments = await action.TransformCompilationAsync(compilationWithoutGeneratedDocuments, cancellationToken).ConfigureAwait(false);

                        if (compilationWithGeneratedDocuments != null)
                        {
                            // Also transform the compilation that has generated files; we won't do that though if the transformation either would cause problems with
                            // the generated documents, or if don't have any source generators in the first place.
                            if (action.CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput &&
                                oldState.SourceGenerators.Any())
                            {
                                compilationWithGeneratedDocuments = await action.TransformCompilationAsync(compilationWithGeneratedDocuments, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                compilationWithGeneratedDocuments = null;
                            }
                        }

                        if (generatorDriver != null)
                        {
                            generatorDriver = action.TransformGeneratorDriver(generatorDriver);
                        }

                        // We have updated state, so store this new result; this allows us to drop the intermediate state we already processed
                        // even if we were to get cancelled at a later point.
                        intermediateProjects = intermediateProjects.RemoveAt(0);

                        // As long as we have intermediate projects, we'll still keep creating InProgressStates.  But
                        // once it becomes empty we'll produce an AllSyntaxTreesParsedState and we'll break the loop.
                        var currentState = CompilationTrackerState.Create(
                            compilationWithoutGeneratedDocuments, state.GeneratorInfo.WithDriver(generatorDriver), compilationWithGeneratedDocuments, intermediateProjects);
                        this.WriteState(currentState);

                        Contract.ThrowIfTrue(intermediateProjects.Count > 0 && currentState is not InProgressState);
                        Contract.ThrowIfTrue(intermediateProjects.Count == 0 && currentState is not AllSyntaxTreesParsedState);

                        resultState = currentState as AllSyntaxTreesParsedState;
                    }

                    Contract.ThrowIfNull(resultState);
                    return resultState;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            /// <summary>
            /// Add all appropriate references to the compilation and set it as our final compilation
            /// state.
            /// </summary>
            /// <param name="compilationWithStaleGeneratedTrees">The compilation from a prior run that contains
            /// generated trees, which match the states included in <paramref name="allSyntaxTreesParsedState"/>'s
            /// generator info. If a generator run here produces the same set of generated documents as are in <paramref
            /// name="allSyntaxTreesParsedState"/>'s generator info, and we don't need to make any other changes to
            /// references, we can then use this compilation instead of re-adding source generated files again to the
            /// compilation that <paramref name="allSyntaxTreesParsedState"/> points to.</param>
            private async Task<FinalState> FinalizeCompilationAsync(
                SolutionCompilationState compilationState,
                AllSyntaxTreesParsedState allSyntaxTreesParsedState,
                Compilation? compilationWithStaleGeneratedTrees,
                CancellationToken cancellationToken)
            {
                try
                {
                    var generatorInfo = allSyntaxTreesParsedState.GeneratorInfo;
                    var compilationWithoutGeneratedDocuments = allSyntaxTreesParsedState.CompilationWithoutGeneratedDocuments;

                    // Project is complete only if the following are all true:
                    //  1. HasAllInformation flag is set for the project
                    //  2. Either the project has non-zero metadata references OR this is the corlib project.
                    //     For the latter, we use a heuristic if the underlying compilation defines "System.Object" type.
                    var hasSuccessfullyLoaded = this.ProjectState.HasAllInformation &&
                        (this.ProjectState.MetadataReferences.Count > 0 ||
                         compilationWithoutGeneratedDocuments.GetTypeByMetadataName("System.Object") != null);

                    var newReferences = new List<MetadataReference>();
                    var metadataReferenceToProjectId = new Dictionary<MetadataReference, ProjectId>();
                    newReferences.AddRange(this.ProjectState.MetadataReferences);

                    foreach (var projectReference in this.ProjectState.ProjectReferences)
                    {
                        var referencedProject = compilationState.SolutionState.GetProjectState(projectReference.ProjectId);

                        // Even though we're creating a final compilation (vs. an in progress compilation),
                        // it's possible that the target project has been removed.
                        if (referencedProject != null)
                        {
                            // If both projects are submissions, we'll count this as a previous submission link
                            // instead of a regular metadata reference
                            if (referencedProject.IsSubmission)
                            {
                                // if the referenced project is a submission project must be a submission as well:
                                Debug.Assert(this.ProjectState.IsSubmission);

                                // We now need to (potentially) update the prior submission compilation. That Compilation is held in the
                                // ScriptCompilationInfo that we need to replace as a unit.
                                var previousSubmissionCompilation =
                                    await compilationState.GetCompilationAsync(
                                        projectReference.ProjectId, cancellationToken).ConfigureAwait(false);

                                if (compilationWithoutGeneratedDocuments.ScriptCompilationInfo!.PreviousScriptCompilation != previousSubmissionCompilation)
                                {
                                    compilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments.WithScriptCompilationInfo(
                                        compilationWithoutGeneratedDocuments.ScriptCompilationInfo!.WithPreviousScriptCompilation(previousSubmissionCompilation!));

                                    compilationWithStaleGeneratedTrees = compilationWithStaleGeneratedTrees?.WithScriptCompilationInfo(
                                        compilationWithStaleGeneratedTrees.ScriptCompilationInfo!.WithPreviousScriptCompilation(previousSubmissionCompilation!));
                                }
                            }
                            else
                            {
                                var metadataReference = await compilationState.GetMetadataReferenceAsync(
                                    projectReference, this.ProjectState, cancellationToken).ConfigureAwait(false);

                                // A reference can fail to be created if a skeleton assembly could not be constructed.
                                if (metadataReference != null)
                                {
                                    newReferences.Add(metadataReference);
                                    metadataReferenceToProjectId.Add(metadataReference, projectReference.ProjectId);
                                }
                                else
                                {
                                    hasSuccessfullyLoaded = false;
                                }
                            }
                        }
                    }

                    // Now that we know the set of references this compilation should have, update them if they're not already.
                    // Generators cannot add references, so we can use the same set of references both for the compilation
                    // that doesn't have generated files, and the one we're trying to reuse that has generated files.
                    // Since we updated both of these compilations together in response to edits, we only have to check one
                    // for a potential mismatch.
                    if (!Enumerable.SequenceEqual(compilationWithoutGeneratedDocuments.ExternalReferences, newReferences))
                    {
                        compilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments.WithReferences(newReferences);
                        compilationWithStaleGeneratedTrees = compilationWithStaleGeneratedTrees?.WithReferences(newReferences);
                    }

                    // We will finalize the compilation by adding full contents here.
                    var (compilationWithGeneratedDocuments, nextGeneratorInfo) = await AddExistingOrComputeNewGeneratorInfoAsync(
                        compilationState,
                        compilationWithoutGeneratedDocuments,
                        generatorInfo,
                        compilationWithStaleGeneratedTrees,
                        cancellationToken).ConfigureAwait(false);

                    // After producing the sg documents, we must always be in the final state for the generator data.
                    Contract.ThrowIfFalse(nextGeneratorInfo.DocumentsAreFinal);

                    var finalState = FinalState.Create(
                        compilationWithGeneratedDocuments,
                        compilationWithoutGeneratedDocuments,
                        hasSuccessfullyLoaded,
                        nextGeneratorInfo,
                        compilationWithGeneratedDocuments,
                        this.ProjectState.Id,
                        metadataReferenceToProjectId);

                    this.WriteState(finalState);

                    return finalState;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Explicitly force a yield point here.  This addresses a problem on .net framework where it's
                    // possible that cancelling this task chain ends up stack overflowing as the TPL attempts to
                    // synchronously recurse through the tasks to execute antecedent work.  This will force continuations
                    // here to run asynchronously preventing the stack overflow.
                    // See https://github.com/dotnet/roslyn/issues/56356 for more details.
                    // note: this can be removed if this code only needs to run on .net core (as the stack overflow issue
                    // does not exist there).
                    await Task.Yield().ConfigureAwait(false);
                    throw;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            /// <summary>
            /// Attempts to get (without waiting) a metadata reference to a possibly in progress
            /// compilation. Only actual compilation references are returned. Could potentially 
            /// return null if nothing can be provided.
            /// </summary>
            public MetadataReference? GetPartialMetadataReference(ProjectState fromProject, ProjectReference projectReference)
            {
                var state = ReadState();

                if (ProjectState.LanguageServices == fromProject.LanguageServices)
                {
                    // if we have a compilation and its the correct language, use a simple compilation reference in any
                    // state it happens to be in right now
                    if (state is WithCompilationTrackerState withCompilationState)
                        return withCompilationState.CompilationWithoutGeneratedDocuments.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                }
                else
                {
                    // Cross project reference.  We need a skeleton reference.  Skeletons are too expensive to
                    // generate on demand.  So just try to see if we can grab the last generated skeleton for that
                    // project.
                    var properties = new MetadataReferenceProperties(aliases: projectReference.Aliases, embedInteropTypes: projectReference.EmbedInteropTypes);
                    return this.SkeletonReferenceCache.TryGetAlreadyBuiltMetadataReference(properties);
                }

                return null;
            }

            public Task<bool> HasSuccessfullyLoadedAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                return this.ReadState() is FinalState finalState
                    ? finalState.HasSuccessfullyLoaded ? SpecializedTasks.True : SpecializedTasks.False
                    : HasSuccessfullyLoadedSlowAsync(compilationState, cancellationToken);
            }

            private async Task<bool> HasSuccessfullyLoadedSlowAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                var finalState = await GetOrBuildFinalStateAsync(
                    compilationState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return finalState.HasSuccessfullyLoaded;
            }

            public async ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                // If we don't have any generators, then we know we have no generated files, so we can skip the computation entirely.
                if (!this.ProjectState.SourceGenerators.Any())
                {
                    return TextDocumentStates<SourceGeneratedDocumentState>.Empty;
                }

                var finalState = await GetOrBuildFinalStateAsync(
                    compilationState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return finalState.GeneratorInfo.Documents;
            }

            public async ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (!this.ProjectState.SourceGenerators.Any())
                {
                    return [];
                }

                var finalState = await GetOrBuildFinalStateAsync(
                    compilationState, cancellationToken: cancellationToken).ConfigureAwait(false);

                var driverRunResult = finalState.GeneratorInfo.Driver?.GetRunResult();
                if (driverRunResult is null)
                {
                    return [];
                }

                using var _ = ArrayBuilder<Diagnostic>.GetInstance(capacity: driverRunResult.Diagnostics.Length, out var builder);

                foreach (var result in driverRunResult.Results)
                {
                    if (!IsGeneratorRunResultToIgnore(result))
                    {
                        builder.AddRange(result.Diagnostics);
                    }
                }

                return builder.ToImmutableAndClear();
            }

            public SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(DocumentId documentId)
            {
                var state = ReadState();

                // If we are in FinalState, then we have correctly ran generators and then know the final contents of the
                // Compilation. The GeneratedDocuments can be filled for intermediate states, but those aren't guaranteed to be
                // correct and can be re-ran later.
                return state is FinalState finalState ? finalState.GeneratorInfo.Documents.GetState(documentId) : null;
            }

            // HACK HACK HACK HACK around a problem introduced by https://github.com/dotnet/sdk/pull/24928. The Razor generator is
            // controlled by a flag that lives in an .editorconfig file; in the IDE we generally don't run the generator and instead use
            // the design-time files added through the legacy IDynamicFileInfo API. When we're doing Hot Reload we then
            // remove those legacy files and remove the .editorconfig file that is supposed to disable the generator, for the Hot
            // Reload pass we then are running the generator. This is done in the CompileTimeSolutionProvider.
            //
            // https://github.com/dotnet/sdk/pull/24928 introduced an issue where even though the Razor generator is being told to not
            // run, it still runs anyways. As a tactical fix rather than reverting that PR, for Visual Studio 17.3 Preview 2 we are going
            // to do a hack here which is to rip out generated files.

            private bool IsGeneratorRunResultToIgnore(GeneratorRunResult result)
            {
                var globalOptions = this.ProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;

                // This matches the implementation in https://github.com/chsienki/sdk/blob/4696442a24e3972417fb9f81f182420df0add107/src/RazorSdk/SourceGenerators/RazorSourceGenerator.RazorProviders.cs#L27-L28
                var suppressGenerator = globalOptions.TryGetValue("build_property.SuppressRazorSourceGenerator", out var option) && option == "true";

                if (!suppressGenerator)
                    return false;

                var generatorType = result.Generator.GetGeneratorType();
                return generatorType.FullName == "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator" &&
                       generatorType.Assembly.GetName().Name is "Microsoft.NET.Sdk.Razor.SourceGenerators" or
                            "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators" or
                            "Microsoft.CodeAnalysis.Razor.Compiler";
            }

            // END HACK HACK HACK HACK, or the setup of it at least; once this hack is removed the calls to IsGeneratorRunResultToIgnore
            // need to be cleaned up.

            /// <summary>
            /// Validates the compilation is consistent and we didn't have a bug in producing it. This only runs under a feature flag.
            /// </summary>
            private void ValidateState(CompilationTrackerState state)
            {
                if (!_validateStates)
                    return;

                if (state is FinalState finalState)
                {
                    ValidateCompilationTreesMatchesProjectState(finalState.FinalCompilationWithGeneratedDocuments, ProjectState, state.GeneratorInfo);
                }
                else if (state is InProgressState inProgressState)
                {
                    ValidateCompilationTreesMatchesProjectState(inProgressState.CompilationWithoutGeneratedDocuments, inProgressState.IntermediateProjects[0].oldState, generatorInfo: null);

                    if (inProgressState.CompilationWithGeneratedDocuments != null)
                    {
                        ValidateCompilationTreesMatchesProjectState(inProgressState.CompilationWithGeneratedDocuments, inProgressState.IntermediateProjects[0].oldState, inProgressState.GeneratorInfo);
                    }
                }
            }

            private static void ValidateCompilationTreesMatchesProjectState(Compilation compilation, ProjectState projectState, CompilationTrackerGeneratorInfo? generatorInfo)
            {
                // We'll do this all in a try/catch so it makes validations easy to do with ThrowExceptionIfFalse().
                try
                {
                    // Assert that all the trees we expect to see are in the Compilation...
                    var syntaxTreesInWorkspaceStates = new HashSet<SyntaxTree>(
#if NET
                        capacity: projectState.DocumentStates.Count + generatorInfo?.Documents.Count ?? 0
#endif
                        );

                    foreach (var documentInProjectState in projectState.DocumentStates.States)
                    {
                        ThrowExceptionIfFalse(documentInProjectState.Value.TryGetSyntaxTree(out var tree), "We should have a tree since we have a compilation that should contain it.");
                        syntaxTreesInWorkspaceStates.Add(tree);
                        ThrowExceptionIfFalse(compilation.ContainsSyntaxTree(tree), "The tree in the ProjectState should have been in the compilation.");
                    }

                    if (generatorInfo != null)
                    {
                        foreach (var generatedDocument in generatorInfo.Value.Documents.States)
                        {
                            ThrowExceptionIfFalse(generatedDocument.Value.TryGetSyntaxTree(out var tree), "We should have a tree since we have a compilation that should contain it.");
                            syntaxTreesInWorkspaceStates.Add(tree);
                            ThrowExceptionIfFalse(compilation.ContainsSyntaxTree(tree), "The tree for the generated document should have been in the compilation.");
                        }
                    }

                    // ...and that the reverse is true too.
                    foreach (var tree in compilation.SyntaxTrees)
                        ThrowExceptionIfFalse(syntaxTreesInWorkspaceStates.Contains(tree), "The tree in the Compilation should have been from the workspace.");
                }
                catch (Exception e) when (FatalError.ReportWithDumpAndCatch(e, ErrorSeverity.Critical))
                {
                }
            }

            /// <summary>
            /// This is just the same as <see cref="Contract.ThrowIfFalse(bool, string, int)"/> but throws a custom exception type to make this easier to find in telemetry since the exception type
            /// is easily seen in telemetry.
            /// </summary>
            private static void ThrowExceptionIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, string message)
            {
                if (!condition)
                {
                    throw new CompilationTrackerValidationException(message);
                }
            }

            public class CompilationTrackerValidationException : Exception
            {
                public CompilationTrackerValidationException() { }
                public CompilationTrackerValidationException(string message) : base(message) { }
                public CompilationTrackerValidationException(string message, Exception inner) : base(message, inner) { }
            }

            #region Versions and Checksums

            // Dependent Versions are stored on compilation tracker so they are more likely to survive when unrelated solution branching occurs.

            private AsyncLazy<VersionStamp>? _lazyDependentVersion;
            private AsyncLazy<VersionStamp>? _lazyDependentSemanticVersion;
            private AsyncLazy<Checksum>? _lazyDependentChecksum;

            public Task<VersionStamp> GetDependentVersionAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (_lazyDependentVersion == null)
                {
                    // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    var compilationStateCapture = compilationState;
                    Interlocked.CompareExchange(ref _lazyDependentVersion, AsyncLazy.Create(
                        c => ComputeDependentVersionAsync(compilationStateCapture, c)), null);
                }

                return _lazyDependentVersion.GetValueAsync(cancellationToken);
            }

            private async Task<VersionStamp> ComputeDependentVersionAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                var projectState = this.ProjectState;
                var projVersion = projectState.Version;
                var docVersion = await projectState.GetLatestDocumentVersionAsync(cancellationToken).ConfigureAwait(false);

                var version = docVersion.GetNewerVersion(projVersion);
                foreach (var dependentProjectReference in projectState.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (compilationState.SolutionState.ContainsProject(dependentProjectReference.ProjectId))
                    {
                        var dependentProjectVersion = await compilationState.GetDependentVersionAsync(dependentProjectReference.ProjectId, cancellationToken).ConfigureAwait(false);
                        version = dependentProjectVersion.GetNewerVersion(version);
                    }
                }

                return version;
            }

            public Task<VersionStamp> GetDependentSemanticVersionAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (_lazyDependentSemanticVersion == null)
                {
                    // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    var compilationStateCapture = compilationState;
                    Interlocked.CompareExchange(ref _lazyDependentSemanticVersion, AsyncLazy.Create(
                        c => ComputeDependentSemanticVersionAsync(compilationStateCapture, c)), null);
                }

                return _lazyDependentSemanticVersion.GetValueAsync(cancellationToken);
            }

            private async Task<VersionStamp> ComputeDependentSemanticVersionAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                var projectState = this.ProjectState;
                var version = await projectState.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                foreach (var dependentProjectReference in projectState.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (compilationState.SolutionState.ContainsProject(dependentProjectReference.ProjectId))
                    {
                        var dependentProjectVersion = await compilationState.GetDependentSemanticVersionAsync(
                            dependentProjectReference.ProjectId, cancellationToken).ConfigureAwait(false);
                        version = dependentProjectVersion.GetNewerVersion(version);
                    }
                }

                return version;
            }

            public Task<Checksum> GetDependentChecksumAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (_lazyDependentChecksum == null)
                {
                    var tmp = compilationState.SolutionState; // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(ref _lazyDependentChecksum, AsyncLazy.Create(c => ComputeDependentChecksumAsync(tmp, c)), null);
                }

                return _lazyDependentChecksum.GetValueAsync(cancellationToken);
            }

            private async Task<Checksum> ComputeDependentChecksumAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<Checksum>.GetInstance(out var tempChecksumArray);

                // Get the checksum for the project itself.
                var projectChecksum = await this.ProjectState.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                tempChecksumArray.Add(projectChecksum);

                // Calculate a checksum this project and for each dependent project that could affect semantics for
                // this project. Ensure that the checksum calculation orders the projects consistently so that we get
                // the same checksum across sessions of VS.  Note: we use the project filepath+name as a unique way
                // to reference a project.  This matches the logic in our persistence-service implemention as to how
                // information is associated with a project.
                var transitiveDependencies = solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(this.ProjectState.Id);
                var orderedProjectIds = transitiveDependencies.OrderBy(id =>
                {
                    var depProject = solution.GetRequiredProjectState(id);
                    return (depProject.FilePath, depProject.Name);
                });

                foreach (var projectId in orderedProjectIds)
                {
                    var referencedProject = solution.GetRequiredProjectState(projectId);

                    // Note that these checksums should only actually be calculated once, if the project is unchanged
                    // the same checksum will be returned.
                    var referencedProjectChecksum = await referencedProject.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                    tempChecksumArray.Add(referencedProjectChecksum);
                }

                return Checksum.Create(tempChecksumArray);
            }

            #endregion
        }
    }
}
