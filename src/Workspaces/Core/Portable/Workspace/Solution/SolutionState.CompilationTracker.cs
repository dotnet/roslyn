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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
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

            private CompilationTracker(
                ProjectState project,
                CompilationTrackerState state,
                SkeletonReferenceCache cachedSkeletonReferences)
            {
                Contract.ThrowIfNull(project);

                this.ProjectState = project;
                _stateDoNotAccessDirectly = state;
                this.SkeletonReferenceCache = cachedSkeletonReferences;
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
                => Volatile.Write(ref _stateDoNotAccessDirectly, state);

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
                ProjectState newProject,
                CompilationAndGeneratorDriverTranslationAction? translate)
            {
                var state = ReadState();

                var baseCompilation = state.CompilationWithoutGeneratedDocuments;
                if (baseCompilation != null)
                {
                    var intermediateProjects = state is InProgressState inProgressState
                        ? inProgressState.IntermediateProjects
                        : ImmutableList<(ProjectState oldState, CompilationAndGeneratorDriverTranslationAction action)>.Empty;

                    if (translate is not null)
                    {
                        // We have a translation action; are we able to merge it with the prior one?
                        var merged = false;
                        if (intermediateProjects.Any())
                        {
                            var (priorState, priorAction) = intermediateProjects.Last();
                            var mergedTranslation = translate.TryMergeWithPrior(priorAction);
                            if (mergedTranslation != null)
                            {
                                // We can replace the prior action with this new one
                                intermediateProjects = intermediateProjects.SetItem(intermediateProjects.Count - 1,
                                    (oldState: priorState, mergedTranslation));
                                merged = true;
                            }
                        }

                        if (!merged)
                        {
                            // Just add it to the end
                            intermediateProjects = intermediateProjects.Add((oldState: this.ProjectState, translate));
                        }
                    }

                    var newState = CompilationTrackerState.Create(
                        baseCompilation, state.GeneratorInfo, state.FinalCompilationWithGeneratedDocuments, intermediateProjects);

                    return new CompilationTracker(newProject, newState, this.SkeletonReferenceCache.Clone());
                }
                else
                {
                    // We may still have a cached generator; we'll have to remember to run generators again since we are making some
                    // change here. We'll also need to update the other state of the driver if appropriate.
                    var generatorInfo = state.GeneratorInfo.WithDocumentsAreFinal(false);

                    if (generatorInfo.Driver != null && translate != null)
                    {
                        generatorInfo = generatorInfo.WithDriver(translate.TransformGeneratorDriver(generatorInfo.Driver));
                    }

                    var newState = new NoCompilationState(generatorInfo);
                    return new CompilationTracker(newProject, newState, this.SkeletonReferenceCache.Clone());
                }
            }

            public ICompilationTracker FreezePartialStateWithTree(SolutionState solution, DocumentState docState, SyntaxTree tree, CancellationToken cancellationToken)
            {
                GetPartialCompilationState(
                    solution, docState.Id,
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
                            inProgressProject = inProgressProject.AddDocuments(ImmutableArray.Create(docState));
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
                                .RemoveDocuments(ImmutableArray.Create(oldDocumentId))
                                .AddDocuments(ImmutableArray.Create(docState));
                        }
                    }
                }

                // At this point, we now absolutely should have our tree in the compilation
                Contract.ThrowIfFalse(compilationPair.CompilationWithoutGeneratedDocuments.ContainsSyntaxTree(tree));

                // The user is asking for an in progress snap.  We don't want to create it and then
                // have the compilation immediately disappear.  So we force it to stay around with a ConstantValueSource.
                // As a policy, all partial-state projects are said to have incomplete references, since the state has no guarantees.
                var finalState = FinalState.Create(
                    finalCompilationSource: compilationPair.CompilationWithGeneratedDocuments,
                    compilationWithoutGeneratedFiles: compilationPair.CompilationWithoutGeneratedDocuments,
                    hasSuccessfullyLoaded: false,
                    generatorInfo,
                    finalCompilation: compilationPair.CompilationWithGeneratedDocuments,
                    this.ProjectState.Id,
                    metadataReferenceToProjectId);

                return new CompilationTracker(inProgressProject, finalState, this.SkeletonReferenceCache.Clone());
            }

            /// <summary>
            /// Tries to get the latest snapshot of the compilation without waiting for it to be
            /// fully built. This method takes advantage of the progress side-effect produced during
            /// <see cref="BuildCompilationInfoAsync(SolutionState, CancellationToken)"/>.
            /// It will either return the already built compilation, any
            /// in-progress compilation or any known old compilation in that order of preference.
            /// The compilation state that is returned will have a compilation that is retained so
            /// that it cannot disappear.
            /// </summary>
            private void GetPartialCompilationState(
                SolutionState solution,
                DocumentId id,
                out ProjectState inProgressProject,
                out CompilationPair compilations,
                out CompilationTrackerGeneratorInfo generatorInfo,
                out Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId,
                CancellationToken cancellationToken)
            {
                var state = ReadState();
                var compilationWithoutGeneratedDocuments = state.CompilationWithoutGeneratedDocuments;

                // check whether we can bail out quickly for typing case
                var inProgressState = state as InProgressState;

                generatorInfo = state.GeneratorInfo.WithDocumentsAreFinalAndFrozen();
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
                    SolutionLogger.UseExistingPartialProjectState();
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
                    SolutionLogger.UseExistingFullProjectState();
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
                    var referencedProject = solution.GetProjectState(projectReference.ProjectId);
                    if (referencedProject != null)
                    {
                        if (referencedProject.IsSubmission)
                        {
                            var previousScriptCompilation = solution.GetCompilationAsync(projectReference.ProjectId, cancellationToken).WaitAndGetResult(cancellationToken);

                            // previous submission project must support compilation:
                            RoslynDebug.Assert(previousScriptCompilation != null);

                            compilations = compilations.WithPreviousScriptCompilation(previousScriptCompilation);
                        }
                        else
                        {
                            // get the latest metadata for the partial compilation of the referenced project.
                            var metadata = solution.GetPartialMetadataReference(projectReference, this.ProjectState);

                            if (metadata == null)
                            {
                                // if we failed to get the metadata, check to see if we previously had existing metadata and reuse it instead.
                                var inProgressCompilationNotRef = compilations.CompilationWithGeneratedDocuments;
                                metadata = inProgressCompilationNotRef.ExternalReferences.FirstOrDefault(
                                    r => solution.GetProjectState(inProgressCompilationNotRef.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol)?.Id == projectReference.ProjectId);
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

                SolutionLogger.CreatePartialProjectState();
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
                compilation = state.FinalCompilationWithGeneratedDocuments;
                return compilation != null;
            }

            public Task<Compilation> GetCompilationAsync(SolutionState solution, CancellationToken cancellationToken)
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
                    return GetCompilationSlowAsync(solution, cancellationToken);
                }
            }

            private async Task<Compilation> GetCompilationSlowAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var compilationInfo = await GetOrBuildCompilationInfoAsync(solution, lockGate: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return compilationInfo.Compilation;
            }

            private async Task<CompilationInfo> GetOrBuildCompilationInfoAsync(
                SolutionState solution,
                bool lockGate,
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
                        var finalCompilation = state.FinalCompilationWithGeneratedDocuments;
                        if (finalCompilation != null)
                        {
                            RoslynDebug.Assert(state.HasSuccessfullyLoaded.HasValue);
                            return new CompilationInfo(finalCompilation, state.HasSuccessfullyLoaded.Value, state.GeneratorInfo);
                        }

                        // Otherwise, we actually have to build it.  Ensure that only one thread is trying to
                        // build this compilation at a time.
                        if (lockGate)
                        {
                            using (await _buildLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                            {
                                return await BuildCompilationInfoAsync(solution, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            return await BuildCompilationInfoAsync(solution, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            /// <summary>
            /// Builds the compilation matching the project state. In the process of building, also
            /// produce in progress snapshots that can be accessed from other threads.
            /// </summary>
            private async Task<CompilationInfo> BuildCompilationInfoAsync(
                SolutionState solution,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var state = ReadState();

                // if we already have a compilation, we must be already done!  This can happen if two
                // threads were waiting to build, and we came in after the other succeeded.
                var compilation = state.FinalCompilationWithGeneratedDocuments;
                if (compilation != null)
                {
                    RoslynDebug.Assert(state.HasSuccessfullyLoaded.HasValue);
                    return new CompilationInfo(compilation, state.HasSuccessfullyLoaded.Value, state.GeneratorInfo);
                }

                compilation = state.CompilationWithoutGeneratedDocuments;

                if (compilation == null)
                {
                    // We've got nothing.  Build it from scratch :(
                    return await BuildCompilationInfoFromScratchAsync(
                        solution,
                        state.GeneratorInfo,
                        cancellationToken).ConfigureAwait(false);
                }

                if (state is AllSyntaxTreesParsedState or FinalState)
                {
                    // We have a declaration compilation, use it to reconstruct the final compilation
                    return await FinalizeCompilationAsync(
                        solution,
                        compilation,
                        state.GeneratorInfo,
                        compilationWithStaleGeneratedTrees: null,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // We must have an in progress compilation. Build off of that.
                    return await BuildFinalStateFromInProgressStateAsync(
                        solution, (InProgressState)state, compilation, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<CompilationInfo> BuildCompilationInfoFromScratchAsync(
                SolutionState solution,
                CompilationTrackerGeneratorInfo generatorInfo,
                CancellationToken cancellationToken)
            {
                try
                {
                    var compilation = await BuildDeclarationCompilationFromScratchAsync(
                        generatorInfo, cancellationToken).ConfigureAwait(false);

                    return await FinalizeCompilationAsync(
                        solution,
                        compilation,
                        generatorInfo,
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
            private async Task<Compilation> BuildDeclarationCompilationFromScratchAsync(
                CompilationTrackerGeneratorInfo generatorInfo,
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
                    WriteState(new AllSyntaxTreesParsedState(compilation, generatorInfo));
                    return compilation;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
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

            private async Task<CompilationInfo> BuildFinalStateFromInProgressStateAsync(
                SolutionState solution, InProgressState state, Compilation inProgressCompilation, CancellationToken cancellationToken)
            {
                try
                {
                    var (compilationWithoutGenerators, compilationWithGenerators, generatorDriver) = await BuildDeclarationCompilationFromInProgressAsync(
                        state, inProgressCompilation, cancellationToken).ConfigureAwait(false);

                    return await FinalizeCompilationAsync(
                        solution,
                        compilationWithoutGenerators,
                        state.GeneratorInfo.WithDriver(generatorDriver),
                        compilationWithGenerators,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private async Task<(Compilation compilationWithoutGenerators, Compilation? compilationWithGenerators, GeneratorDriver? generatorDriver)> BuildDeclarationCompilationFromInProgressAsync(
                InProgressState state, Compilation compilationWithoutGenerators, CancellationToken cancellationToken)
            {
                try
                {
                    var compilationWithGenerators = state.CompilationWithGeneratedDocuments;
                    var generatorDriver = state.GeneratorInfo.Driver;

                    // If compilationWithGenerators is the same as compilationWithoutGenerators, then it means a prior run of generators
                    // didn't produce any files. In that case, we'll just make compilationWithGenerators null so we avoid doing any
                    // transformations of it multiple times. Otherwise the transformations below and in FinalizeCompilationAsync will try
                    // to update both at once, which is functionally fine but just unnecessary work. This function is always allowed to return
                    // null for compilationWithGenerators in the end, so there's no harm there.
                    if (compilationWithGenerators == compilationWithoutGenerators)
                    {
                        compilationWithGenerators = null;
                    }

                    var intermediateProjects = state.IntermediateProjects;

                    while (!intermediateProjects.IsEmpty)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // We have a list of transformations to get to our final compilation; take the first transformation and apply it.
                        var intermediateProject = intermediateProjects[0];

                        compilationWithoutGenerators = await intermediateProject.action.TransformCompilationAsync(compilationWithoutGenerators, cancellationToken).ConfigureAwait(false);

                        if (compilationWithGenerators != null)
                        {
                            // Also transform the compilation that has generated files; we won't do that though if the transformation either would cause problems with
                            // the generated documents, or if don't have any source generators in the first place.
                            if (intermediateProject.action.CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput &&
                                intermediateProject.oldState.SourceGenerators.Any())
                            {
                                compilationWithGenerators = await intermediateProject.action.TransformCompilationAsync(compilationWithGenerators, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                compilationWithGenerators = null;
                            }
                        }

                        if (generatorDriver != null)
                        {
                            generatorDriver = intermediateProject.action.TransformGeneratorDriver(generatorDriver);
                        }

                        // We have updated state, so store this new result; this allows us to drop the intermediate state we already processed
                        // even if we were to get cancelled at a later point.
                        intermediateProjects = intermediateProjects.RemoveAt(0);

                        this.WriteState(CompilationTrackerState.Create(
                            compilationWithoutGenerators, state.GeneratorInfo.WithDriver(generatorDriver), compilationWithGenerators, intermediateProjects));
                    }

                    return (compilationWithoutGenerators, compilationWithGenerators, generatorDriver);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private readonly struct CompilationInfo
            {
                public Compilation Compilation { get; }
                public bool HasSuccessfullyLoaded { get; }
                public CompilationTrackerGeneratorInfo GeneratorInfo { get; }

                public CompilationInfo(Compilation compilation, bool hasSuccessfullyLoaded, CompilationTrackerGeneratorInfo generatorInfo)
                {
                    Compilation = compilation;
                    HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                    GeneratorInfo = generatorInfo;
                }
            }

            /// <summary>
            /// Add all appropriate references to the compilation and set it as our final compilation
            /// state.
            /// </summary>
            /// <param name="generatorInfo">The generator info that contains the last run of the documents, if any exists, as
            /// well as the driver that can be used to run if need to.</param>
            /// <param name="compilationWithStaleGeneratedTrees">The compilation from a prior run that contains generated trees, which
            /// match the states included in <paramref name="generatorInfo"/>. If a generator run here produces
            /// the same set of generated documents as are in <paramref name="generatorInfo"/>, and we don't need to make any other
            /// changes to references, we can then use this compilation instead of re-adding source generated files again to the
            /// <paramref name="compilationWithoutGenerators"/>.</param>
            private async Task<CompilationInfo> FinalizeCompilationAsync(
                SolutionState solution,
                Compilation compilationWithoutGenerators,
                CompilationTrackerGeneratorInfo generatorInfo,
                Compilation? compilationWithStaleGeneratedTrees,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Project is complete only if the following are all true:
                    //  1. HasAllInformation flag is set for the project
                    //  2. Either the project has non-zero metadata references OR this is the corlib project.
                    //     For the latter, we use a heuristic if the underlying compilation defines "System.Object" type.
                    var hasSuccessfullyLoaded = this.ProjectState.HasAllInformation &&
                        (this.ProjectState.MetadataReferences.Count > 0 ||
                         compilationWithoutGenerators.GetTypeByMetadataName("System.Object") != null);

                    var newReferences = new List<MetadataReference>();
                    var metadataReferenceToProjectId = new Dictionary<MetadataReference, ProjectId>();
                    newReferences.AddRange(this.ProjectState.MetadataReferences);

                    foreach (var projectReference in this.ProjectState.ProjectReferences)
                    {
                        var referencedProject = solution.GetProjectState(projectReference.ProjectId);

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
                                    await solution.GetCompilationAsync(projectReference.ProjectId, cancellationToken).ConfigureAwait(false);

                                if (compilationWithoutGenerators.ScriptCompilationInfo!.PreviousScriptCompilation != previousSubmissionCompilation)
                                {
                                    compilationWithoutGenerators = compilationWithoutGenerators.WithScriptCompilationInfo(
                                        compilationWithoutGenerators.ScriptCompilationInfo!.WithPreviousScriptCompilation(previousSubmissionCompilation!));

                                    compilationWithStaleGeneratedTrees = compilationWithStaleGeneratedTrees?.WithScriptCompilationInfo(
                                        compilationWithStaleGeneratedTrees.ScriptCompilationInfo!.WithPreviousScriptCompilation(previousSubmissionCompilation!));
                                }
                            }
                            else
                            {
                                var metadataReference = await solution.GetMetadataReferenceAsync(
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
                    if (!Enumerable.SequenceEqual(compilationWithoutGenerators.ExternalReferences, newReferences))
                    {
                        compilationWithoutGenerators = compilationWithoutGenerators.WithReferences(newReferences);
                        compilationWithStaleGeneratedTrees = compilationWithStaleGeneratedTrees?.WithReferences(newReferences);
                    }

                    // We will finalize the compilation by adding full contents here.
                    Compilation compilationWithGenerators;

                    if (generatorInfo.DocumentsAreFinal)
                    {
                        // We must have ran generators before, but for some reason had to remake the compilation from scratch.
                        // This could happen if the trees were strongly held, but the compilation was entirely garbage collected.
                        // Just add in the trees we already have. We don't want to rerun since the consumer of this Solution
                        // snapshot has already seen the trees and thus needs to ensure identity of them.
                        compilationWithGenerators = compilationWithoutGenerators.AddSyntaxTrees(
                            await generatorInfo.Documents.States.Values.SelectAsArrayAsync(state => state.GetSyntaxTreeAsync(cancellationToken)).ConfigureAwait(false));
                    }
                    else
                    {
                        using var generatedDocumentsBuilder = new TemporaryArray<SourceGeneratedDocumentState>();

                        if (!ProjectState.SourceGenerators.Any())
                        {
                            // We don't have any generators, so if we have a compilation from a previous run with generated files, we definitely can't use it anymore
                            compilationWithStaleGeneratedTrees = null;
                        }
                        else // we have a generator
                        {
                            // If we don't already have a generator driver, we'll have to create one from scratch
                            if (generatorInfo.Driver == null)
                            {
                                var additionalTexts = this.ProjectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText);
                                var compilationFactory = this.ProjectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                                generatorInfo = generatorInfo.WithDriver(compilationFactory.CreateGeneratorDriver(
                                        this.ProjectState.ParseOptions!,
                                        ProjectState.SourceGenerators.ToImmutableArray(),
                                        this.ProjectState.AnalyzerOptions.AnalyzerConfigOptionsProvider,
                                        additionalTexts));
                            }
                            else
                            {
#if DEBUG

                                // Assert that the generator driver is in sync with our additional document states; there's not a public
                                // API to get this, but we'll reflect in DEBUG-only.
                                var driverType = generatorInfo.Driver.GetType();
                                var stateMember = driverType.GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                Contract.ThrowIfNull(stateMember);
                                var additionalTextsMember = stateMember.FieldType.GetField("AdditionalTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                Contract.ThrowIfNull(additionalTextsMember);
                                var state = stateMember.GetValue(generatorInfo.Driver);
                                var additionalTexts = (ImmutableArray<AdditionalText>)additionalTextsMember.GetValue(state)!;

                                Contract.ThrowIfFalse(additionalTexts.Length == this.ProjectState.AdditionalDocumentStates.Count);

#endif
                            }

                            // HACK HACK HACK HACK to address https://github.com/dotnet/roslyn/issues/59818. There, we were running into issues where
                            // a generator being present and consuming syntax was causing all red nodes to be processed. This was problematic when
                            // Razor design time files are also fed in, since those files tend to be quite large. The Razor design time files
                            // aren't produced via a generator, but rather via our legacy IDynamicFileInfo mechanism, so it's also a bit strange
                            // we'd even give them to other generators since that doesn't match the real compiler anyways. This simply removes
                            // all of those trees in an effort to speed things up, and also ensure the design time compilations are a bit more accurate.
                            using var _ = ArrayBuilder<SyntaxTree>.GetInstance(out var treesToRemove);

                            foreach (var documentState in ProjectState.DocumentStates.States)
                            {
                                // This matches the logic in CompileTimeSolutionProvider for which documents are removed when we're
                                // activating the generator.
                                if (documentState.Value.Attributes.DesignTimeOnly)
                                {
                                    treesToRemove.Add(await documentState.Value.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                                }
                            }

                            var compilationToRunGeneratorsOn = compilationWithoutGenerators.RemoveSyntaxTrees(treesToRemove);
                            // END HACK HACK HACK HACK.

                            generatorInfo = generatorInfo.WithDriver(generatorInfo.Driver!.RunGenerators(compilationToRunGeneratorsOn, cancellationToken));

                            solution.Services.GetService<ISourceGeneratorTelemetryCollectorWorkspaceService>()?.CollectRunResult(generatorInfo.Driver!.GetRunResult(), generatorInfo.Driver!.GetTimingInfo(), ProjectState);

                            var runResult = generatorInfo.Driver!.GetRunResult();

                            // We may be able to reuse compilationWithStaleGeneratedTrees if the generated trees are identical. We will assign null
                            // to compilationWithStaleGeneratedTrees if we at any point realize it can't be used. We'll first check the count of trees
                            // if that changed then we absolutely can't reuse it. But if the counts match, we'll then see if each generated tree
                            // content is identical to the prior generation run; if we find a match each time, then the set of the generated trees
                            // and the prior generated trees are identical.
                            if (compilationWithStaleGeneratedTrees != null)
                            {
                                var generatedTreeCount =
                                    runResult.Results.Sum(r => IsGeneratorRunResultToIgnore(r) ? 0 : r.GeneratedSources.Length);

                                if (generatorInfo.Documents.Count != generatedTreeCount)
                                {
                                    compilationWithStaleGeneratedTrees = null;
                                }
                            }

                            foreach (var generatorResult in runResult.Results)
                            {
                                if (IsGeneratorRunResultToIgnore(generatorResult))
                                {
                                    continue;
                                }

                                var generatorAnalyzerReference = this.ProjectState.GetAnalyzerReferenceForGenerator(generatorResult.Generator);

                                foreach (var generatedSource in generatorResult.GeneratedSources)
                                {
                                    var existing = FindExistingGeneratedDocumentState(
                                        generatorInfo.Documents,
                                        generatorResult.Generator,
                                        generatorAnalyzerReference,
                                        generatedSource.HintName);

                                    if (existing != null)
                                    {
                                        var newDocument = existing.WithUpdatedGeneratedContent(
                                                generatedSource.SourceText,
                                                this.ProjectState.ParseOptions!);

                                        generatedDocumentsBuilder.Add(newDocument);

                                        if (newDocument != existing)
                                            compilationWithStaleGeneratedTrees = null;
                                    }
                                    else
                                    {
                                        // NOTE: the use of generatedSource.SyntaxTree to fetch the path and options is OK,
                                        // since the tree is a lazy tree and that won't trigger the parse.
                                        var identity = SourceGeneratedDocumentIdentity.Generate(
                                            ProjectState.Id,
                                            generatedSource.HintName,
                                            generatorResult.Generator,
                                            generatedSource.SyntaxTree.FilePath,
                                            generatorAnalyzerReference);

                                        generatedDocumentsBuilder.Add(
                                            SourceGeneratedDocumentState.Create(
                                                identity,
                                                generatedSource.SourceText,
                                                generatedSource.SyntaxTree.Options,
                                                ProjectState.LanguageServices));

                                        // The count of trees was the same, but something didn't match up. Since we're here, at least one tree
                                        // was added, and an equal number must have been removed. Rather than trying to incrementally update
                                        // this compilation, we'll just toss this and re-add all the trees.
                                        compilationWithStaleGeneratedTrees = null;
                                    }
                                }
                            }
                        }

                        // If we didn't null out this compilation, it means we can actually use it
                        if (compilationWithStaleGeneratedTrees != null)
                        {
                            compilationWithGenerators = compilationWithStaleGeneratedTrees;
                            generatorInfo = generatorInfo.WithDocumentsAreFinal(true);
                        }
                        else
                        {
                            // We produced new documents, so time to create new state for it
                            var generatedDocuments = new TextDocumentStates<SourceGeneratedDocumentState>(generatedDocumentsBuilder.ToImmutableAndClear());
                            compilationWithGenerators = compilationWithoutGenerators.AddSyntaxTrees(
                                await generatedDocuments.States.Values.SelectAsArrayAsync(state => state.GetSyntaxTreeAsync(cancellationToken)).ConfigureAwait(false));
                            generatorInfo = new CompilationTrackerGeneratorInfo(generatedDocuments, generatorInfo.Driver, documentsAreFinal: true);
                        }
                    }

                    var finalState = FinalState.Create(
                        compilationWithGenerators,
                        compilationWithoutGenerators,
                        hasSuccessfullyLoaded,
                        generatorInfo,
                        compilationWithGenerators,
                        this.ProjectState.Id,
                        metadataReferenceToProjectId);

                    this.WriteState(finalState);

                    return new CompilationInfo(compilationWithGenerators, hasSuccessfullyLoaded, generatorInfo);
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

                // Local functions
                static SourceGeneratedDocumentState? FindExistingGeneratedDocumentState(
                    TextDocumentStates<SourceGeneratedDocumentState> states,
                    ISourceGenerator generator,
                    AnalyzerReference analyzerReference,
                    string hintName)
                {
                    var generatorIdentity = new SourceGeneratorIdentity(generator, analyzerReference);

                    foreach (var (_, state) in states.States)
                    {
                        if (state.Identity.Generator != generatorIdentity)
                            continue;

                        if (state.HintName != hintName)
                            continue;

                        return state;
                    }

                    return null;
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
                    if (state.CompilationWithoutGeneratedDocuments is { } compilation)
                        return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
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

            public Task<bool> HasSuccessfullyLoadedAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var state = this.ReadState();

                if (state.HasSuccessfullyLoaded.HasValue)
                {
                    return state.HasSuccessfullyLoaded.Value ? SpecializedTasks.True : SpecializedTasks.False;
                }
                else
                {
                    return HasSuccessfullyLoadedSlowAsync(solution, cancellationToken);
                }
            }

            private async Task<bool> HasSuccessfullyLoadedSlowAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var compilationInfo = await GetOrBuildCompilationInfoAsync(solution, lockGate: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return compilationInfo.HasSuccessfullyLoaded;
            }

            public async ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                // If we don't have any generators, then we know we have no generated files, so we can skip the computation entirely.
                if (!this.ProjectState.SourceGenerators.Any())
                {
                    return TextDocumentStates<SourceGeneratedDocumentState>.Empty;
                }

                var compilationInfo = await GetOrBuildCompilationInfoAsync(solution, lockGate: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return compilationInfo.GeneratorInfo.Documents;
            }

            public async ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (!this.ProjectState.SourceGenerators.Any())
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var compilationInfo = await GetOrBuildCompilationInfoAsync(solution, lockGate: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                var driverRunResult = compilationInfo.GeneratorInfo.Driver?.GetRunResult();
                if (driverRunResult is null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
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
                       generatorType.Assembly.GetName().Name == "Microsoft.NET.Sdk.Razor.SourceGenerators";
            }

            // END HACK HACK HACK HACK, or the setup of it at least; once this hack is removed the calls to IsGeneratorRunResultToIgnore
            // need to be cleaned up.

            #region Versions and Checksums

            // Dependent Versions are stored on compilation tracker so they are more likely to survive when unrelated solution branching occurs.

            private AsyncLazy<VersionStamp>? _lazyDependentVersion;
            private AsyncLazy<VersionStamp>? _lazyDependentSemanticVersion;
            private AsyncLazy<Checksum>? _lazyDependentChecksum;

            public Task<VersionStamp> GetDependentVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (_lazyDependentVersion == null)
                {
                    var tmp = solution; // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(ref _lazyDependentVersion, AsyncLazy.Create(c => ComputeDependentVersionAsync(tmp, c)), null);
                }

                return _lazyDependentVersion.GetValueAsync(cancellationToken);
            }

            private async Task<VersionStamp> ComputeDependentVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var projectState = this.ProjectState;
                var projVersion = projectState.Version;
                var docVersion = await projectState.GetLatestDocumentVersionAsync(cancellationToken).ConfigureAwait(false);

                var version = docVersion.GetNewerVersion(projVersion);
                foreach (var dependentProjectReference in projectState.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (solution.ContainsProject(dependentProjectReference.ProjectId))
                    {
                        var dependentProjectVersion = await solution.GetDependentVersionAsync(dependentProjectReference.ProjectId, cancellationToken).ConfigureAwait(false);
                        version = dependentProjectVersion.GetNewerVersion(version);
                    }
                }

                return version;
            }

            public Task<VersionStamp> GetDependentSemanticVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (_lazyDependentSemanticVersion == null)
                {
                    var tmp = solution; // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(ref _lazyDependentSemanticVersion, AsyncLazy.Create(c => ComputeDependentSemanticVersionAsync(tmp, c)), null);
                }

                return _lazyDependentSemanticVersion.GetValueAsync(cancellationToken);
            }

            private async Task<VersionStamp> ComputeDependentSemanticVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                var projectState = this.ProjectState;
                var version = await projectState.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                foreach (var dependentProjectReference in projectState.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (solution.ContainsProject(dependentProjectReference.ProjectId))
                    {
                        var dependentProjectVersion = await solution.GetDependentSemanticVersionAsync(dependentProjectReference.ProjectId, cancellationToken).ConfigureAwait(false);
                        version = dependentProjectVersion.GetNewerVersion(version);
                    }
                }

                return version;
            }

            public Task<Checksum> GetDependentChecksumAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (_lazyDependentChecksum == null)
                {
                    var tmp = solution; // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(ref _lazyDependentChecksum, AsyncLazy.Create(c => ComputeDependentChecksumAsync(tmp, c)), null);
                }

                return _lazyDependentChecksum.GetValueAsync(cancellationToken);
            }

            private async Task<Checksum> ComputeDependentChecksumAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                using var tempChecksumArray = TemporaryArray<Checksum>.Empty;

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

                return Checksum.Create(tempChecksumArray.ToImmutableAndClear());
            }

            #endregion
        }
    }
}
