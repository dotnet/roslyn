// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private sealed partial class RegularCompilationTracker : ICompilationTracker
        {
            private static readonly Func<ProjectState, string> s_logBuildCompilationAsync =
                state => string.Join(",", state.AssemblyName, state.DocumentStates.Count);

            private static readonly Lazy<Compilation?> s_lazyNullCompilation = new Lazy<Compilation?>(() => null);

            public ProjectState ProjectState { get; }

            /// <summary>
            /// Access via the <see cref="ReadState"/> and <see cref="WriteState"/> methods.
            /// </summary>
            private CompilationTrackerState? _stateDoNotAccessDirectly;

            // guarantees only one thread is building at a time
            private SemaphoreSlim? _buildLock;

            /// <summary>
            /// Intentionally not readonly.  This is a mutable struct.
            /// </summary>
            private SkeletonReferenceCache _skeletonReferenceCache;

            /// <summary>
            /// Set via a feature flag to enable strict validation of the compilations that are produced, in that they match the original states. This validation is expensive, so we don't want it
            /// running in normal production scenarios.
            /// </summary>
            private readonly bool _validateStates;

            private RegularCompilationTracker(
                ProjectState project,
                CompilationTrackerState? state,
                in SkeletonReferenceCache skeletonReferenceCacheToClone)
            {
                Contract.ThrowIfNull(project);

                this.ProjectState = project;
                _stateDoNotAccessDirectly = state;

                _skeletonReferenceCache = skeletonReferenceCacheToClone.Clone();

                _validateStates = project.LanguageServices.SolutionServices.GetRequiredService<IWorkspaceConfigurationService>().Options.ValidateCompilationTrackerStates;

                ValidateState(state);
            }

            /// <summary>
            /// Creates a tracker for the provided project.  The tracker will be in the 'empty' state
            /// and will have no extra information beyond the project itself.
            /// </summary>
            public RegularCompilationTracker(ProjectState project)
                : this(project, state: null, skeletonReferenceCacheToClone: new())
            {
            }

            private CompilationTrackerState? ReadState()
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
                    return state?.GeneratorInfo.Driver;
                }
            }

            public bool ContainsAssemblyOrModuleOrDynamic(
                ISymbol symbol, bool primary,
                [NotNullWhen(true)] out Compilation? compilation,
                out MetadataReferenceInfo? referencedThrough)
            {
                Debug.Assert(symbol.Kind is SymbolKind.Assembly or SymbolKind.NetModule or SymbolKind.DynamicType);
                if (this.ReadState() is not FinalCompilationTrackerState finalState)
                {
                    // this was not a tracker that has handed out a compilation (all compilations handed out must be
                    // owned by a 'FinalState').  So this symbol could not be from us.
                    compilation = null;
                    referencedThrough = null;
                    return false;
                }

                return finalState.RootedSymbolSet.ContainsAssemblyOrModuleOrDynamic(symbol, primary, out compilation, out referencedThrough);
            }

            /// <summary>
            /// Creates a new instance of the compilation info, retaining any already built
            /// compilation state as the now 'old' state
            /// </summary>
            public ICompilationTracker Fork(
                ProjectState newProjectState,
                TranslationAction? translate)
            {
                var forkedTrackerState = ForkTrackerState();

                // We should never fork into a FinalCompilationTrackerState.  We must always be at some state prior to
                // it since some change has happened, and we may now need to run generators.
                Contract.ThrowIfTrue(forkedTrackerState is FinalCompilationTrackerState);
                Contract.ThrowIfFalse(forkedTrackerState is null or InProgressState);
                return new RegularCompilationTracker(
                    newProjectState,
                    forkedTrackerState,
                    skeletonReferenceCacheToClone: _skeletonReferenceCache);

                CompilationTrackerState? ForkTrackerState()
                {
                    var state = this.ReadState();
                    if (state is null)
                        return null;

                    var (compilationWithoutGeneratedDocuments, staleCompilationWithGeneratedDocuments) = state switch
                    {
                        InProgressState inProgressState => (inProgressState.LazyCompilationWithoutGeneratedDocuments, inProgressState.LazyStaleCompilationWithGeneratedDocuments),
                        FinalCompilationTrackerState finalState => (new Lazy<Compilation>(() => finalState.CompilationWithoutGeneratedDocuments), new Lazy<Compilation?>(() => finalState.FinalCompilationWithGeneratedDocuments)),
                        _ => throw ExceptionUtilities.UnexpectedValue(state.GetType()),
                    };

                    var newState = new InProgressState(
                        state.CreationPolicy,
                        compilationWithoutGeneratedDocuments,
                        state.GeneratorInfo,
                        staleCompilationWithGeneratedDocuments,
                        GetPendingTranslationActions(state));

                    return newState;
                }

                ImmutableList<TranslationAction> GetPendingTranslationActions(CompilationTrackerState state)
                {
                    var pendingTranslationActions = state switch
                    {
                        InProgressState inProgressState => inProgressState.PendingTranslationActions,
                        FinalCompilationTrackerState => [],
                        _ => throw ExceptionUtilities.UnexpectedValue(state.GetType()),
                    };

                    if (translate is null)
                        return pendingTranslationActions;

                    // We have a translation action; are we able to merge it with the prior one?
                    if (!pendingTranslationActions.IsEmpty)
                    {
                        var priorAction = pendingTranslationActions.Last();
                        var mergedTranslation = translate.TryMergeWithPrior(priorAction);
                        if (mergedTranslation != null)
                        {
                            // We can replace the prior action with this new one
                            return pendingTranslationActions.SetItem(
                                pendingTranslationActions.Count - 1,
                                mergedTranslation);
                        }
                    }

                    // Just add it to the end
                    return pendingTranslationActions.Add(translate);
                }
            }

            /// <summary>
            /// Gets the final compilation if it is available.
            /// </summary>
            public bool TryGetCompilation([NotNullWhen(true)] out Compilation? compilation)
            {
                var state = ReadState();
                if (state is FinalCompilationTrackerState finalState)
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

            private async Task<FinalCompilationTrackerState> GetOrBuildFinalStateAsync(
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
                        if (state is FinalCompilationTrackerState finalState)
                            return finalState;

                        var buildLock = InterlockedOperations.Initialize(
                            ref _buildLock,
                            static () => new SemaphoreSlim(initialCount: 1));

                        // Otherwise, we actually have to build it.  Ensure that only one thread is trying to
                        // build this compilation at a time.
                        using (await buildLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                        {
                            return await BuildFinalStateAsync().ConfigureAwait(false);
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
                async Task<FinalCompilationTrackerState> BuildFinalStateAsync()
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = ReadState();

                    // if we already have a compilation, we must be already done!  This can happen if two
                    // threads were waiting to build, and we came in after the other succeeded.
                    if (state is FinalCompilationTrackerState finalState)
                        return finalState;

                    // Transition from wherever we're currently at to an in-progress-state.
                    var expandedInProgressState = state switch
                    {
                        // We're already there, so no transition needed.
                        InProgressState inProgressState => inProgressState,

                        // We've got nothing.  Build it from scratch :(
                        null => BuildInProgressStateFromNoCompilationState(),

                        _ => throw ExceptionUtilities.UnexpectedValue(state.GetType())
                    };

                    // Now do the final step of transitioning from the 'all trees parsed' state to the final state.
                    var collapsedInProgressState = await CollapseInProgressStateAsync(expandedInProgressState).ConfigureAwait(false);
                    return await FinalizeCompilationAsync(collapsedInProgressState).ConfigureAwait(false);
                }

                [PerformanceSensitive(
                    "https://github.com/dotnet/roslyn/issues/23582",
                    Constraint = "Avoid calling " + nameof(Compilation.AddSyntaxTrees) + " in a loop due to allocation overhead.")]

                InProgressState BuildInProgressStateFromNoCompilationState()
                {
                    try
                    {
                        // Create a chain of translation steps where we add a chunk of documents at a time to an
                        // initially empty compilation.  This allows us to then process that chain of actions like we
                        // would do any other.  It also means that if we're in the process of parsing documents in that
                        // chain, that we'll see the results of how far we've gotten if someone asks for a frozen
                        // snapshot midway through.
                        var initialProjectState = this.ProjectState.RemoveAllNormalDocuments();
                        var initialCompilation = this.CreateEmptyCompilation();

                        var translationActionsBuilder = ImmutableList.CreateBuilder<TranslationAction>();

                        var oldProjectState = initialProjectState;
                        foreach (var chunk in this.ProjectState.DocumentStates.GetStatesInCompilationOrder().Chunk(TranslationAction.AddDocumentsAction.AddDocumentsBatchSize))
                        {
                            var documentStates = ImmutableCollectionsMarshal.AsImmutableArray(chunk);
                            var newProjectState = oldProjectState.AddDocuments(documentStates);
                            translationActionsBuilder.Add(new TranslationAction.AddDocumentsAction(
                                oldProjectState, newProjectState, documentStates));

                            oldProjectState = newProjectState;
                        }

                        var compilationWithoutGeneratedDocuments = CreateEmptyCompilation();

                        // We only got here when we had no compilation state at all.  So we couldn't have gotten here
                        // from a frozen state (as a frozen state always ensures we have at least an InProgressState).
                        // As such, we want to start initially in the state where we will both run generators and create
                        // skeleton references for p2p references.  That will ensure the most correct state for our
                        // compilation the first time we create it.
                        var allSyntaxTreesParsedState = new InProgressState(
                            CreationPolicy.Create,
                            new Lazy<Compilation>(CreateEmptyCompilation),
                            CompilationTrackerGeneratorInfo.Empty,
                            staleCompilationWithGeneratedDocuments: s_lazyNullCompilation,
                            pendingTranslationActions: translationActionsBuilder.ToImmutable());

                        WriteState(allSyntaxTreesParsedState);
                        return allSyntaxTreesParsedState;
                    }
                    catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                    {
                        throw ExceptionUtilities.Unreachable();
                    }
                }

                async Task<InProgressState> CollapseInProgressStateAsync(InProgressState initialState)
                {
                    try
                    {
                        // Only bother keeping track of staleCompilationWithGeneratedDocuments for projects that
                        // actually have generators in them.
                        var hasSourceGenerators = await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false);
                        var currentState = initialState;

                        // Then, we serially process the chain while that parsing is happening concurrently.
                        while (currentState.PendingTranslationActions.Count > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // We have a list of transformations to get to our final compilation; take the first transformation and apply it.
                            var (compilationWithoutGeneratedDocuments, staleCompilationWithGeneratedDocuments, generatorInfo) =
                                await ApplyFirstTransformationAsync(currentState, hasSourceGenerators).ConfigureAwait(false);

                            // We have updated state, so store this new result; this allows us to drop the intermediate state we already processed
                            // even if we were to get cancelled at a later point.
                            //
                            // As long as we have intermediate projects, we'll still keep creating InProgressStates.  But
                            // once it becomes empty we'll produce an AllSyntaxTreesParsedState and we'll break the loop.
                            //
                            // Preserve the current frozen bit.  Specifically, once states become frozen, we continually make
                            // all states forked from those states frozen as well.  This ensures we don't attempt to move
                            // generator docs back to the uncomputed state from that point onwards.  We'll just keep
                            // whateverZ generated docs we have.
                            currentState = new InProgressState(
                                currentState.CreationPolicy,
                                compilationWithoutGeneratedDocuments,
                                generatorInfo,
                                staleCompilationWithGeneratedDocuments,
                                currentState.PendingTranslationActions.RemoveAt(0));
                            this.WriteState(currentState);
                        }

                        return currentState;
                    }
                    catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
                    {
                        throw ExceptionUtilities.Unreachable();
                    }

                    async Task<(Compilation compilationWithoutGeneratedDocuments, Compilation? staleCompilationWithGeneratedDocuments, CompilationTrackerGeneratorInfo generatorInfo)>
                        ApplyFirstTransformationAsync(InProgressState inProgressState, bool hasSourceGenerators)
                    {
                        Contract.ThrowIfTrue(inProgressState.PendingTranslationActions.IsEmpty);
                        var translationAction = inProgressState.PendingTranslationActions[0];

                        var compilationWithoutGeneratedDocuments = inProgressState.CompilationWithoutGeneratedDocuments;
                        var staleCompilationWithGeneratedDocuments = inProgressState.LazyStaleCompilationWithGeneratedDocuments.Value;

                        // If staleCompilationWithGeneratedDocuments is the same as compilationWithoutGeneratedDocuments,
                        // then it means a prior run of generators didn't produce any files. In that case, we'll just make
                        // staleCompilationWithGeneratedDocuments null so we avoid doing any transformations of it multiple
                        // times. Otherwise the transformations below and in FinalizeCompilationAsync will try to update
                        // both at once, which is functionally fine but just unnecessary work. This function is always
                        // allowed to return null for AllSyntaxTreesParsedState.StaleCompilationWithGeneratedDocuments in
                        // the end, so there's no harm there.
                        if (staleCompilationWithGeneratedDocuments == compilationWithoutGeneratedDocuments)
                            staleCompilationWithGeneratedDocuments = null;

                        compilationWithoutGeneratedDocuments = await translationAction.TransformCompilationAsync(compilationWithoutGeneratedDocuments, cancellationToken).ConfigureAwait(false);

                        if (staleCompilationWithGeneratedDocuments != null)
                        {
                            // Also transform the compilation that has generated files; we won't do that though if the transformation either would cause problems with
                            // the generated documents, or if don't have any source generators in the first place.
                            if (translationAction.CanUpdateCompilationWithStaleGeneratedTreesIfGeneratorsGiveSameOutput &&
                                hasSourceGenerators)
                            {
                                staleCompilationWithGeneratedDocuments = await translationAction.TransformCompilationAsync(staleCompilationWithGeneratedDocuments, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                staleCompilationWithGeneratedDocuments = null;
                            }
                        }

                        var generatorInfo = inProgressState.GeneratorInfo;
                        if (generatorInfo.Driver != null)
                            generatorInfo = generatorInfo with { Driver = translationAction.TransformGeneratorDriver(generatorInfo.Driver) };

                        return (compilationWithoutGeneratedDocuments, staleCompilationWithGeneratedDocuments, generatorInfo);
                    }
                }

                // <summary>
                // Add all appropriate references to the compilation and set it as our final compilation state.
                // </summary>
                async Task<FinalCompilationTrackerState> FinalizeCompilationAsync(InProgressState inProgressState)
                {
                    try
                    {
                        return await FinalizeCompilationWorkerAsync(inProgressState).ConfigureAwait(false);
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

                async Task<FinalCompilationTrackerState> FinalizeCompilationWorkerAsync(InProgressState inProgressState)
                {
                    // Caller should collapse the in progress state first.
                    Contract.ThrowIfTrue(inProgressState.PendingTranslationActions.Count > 0);

                    var creationPolicy = inProgressState.CreationPolicy;
                    var generatorInfo = inProgressState.GeneratorInfo;
                    var compilationWithoutGeneratedDocuments = inProgressState.CompilationWithoutGeneratedDocuments;
                    var staleCompilationWithGeneratedDocuments = inProgressState.LazyStaleCompilationWithGeneratedDocuments.Value;

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
                        if (referencedProject is null)
                            continue;

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

                                staleCompilationWithGeneratedDocuments = staleCompilationWithGeneratedDocuments?.WithScriptCompilationInfo(
                                    staleCompilationWithGeneratedDocuments.ScriptCompilationInfo!.WithPreviousScriptCompilation(previousSubmissionCompilation!));
                            }
                        }
                        else
                        {
                            // Not a submission.  Add as a metadata reference.

                            if (creationPolicy.SkeletonReferenceCreationPolicy is SkeletonReferenceCreationPolicy.Create)
                            {
                                // Client always wants an up to date metadata reference.  Produce one for this project
                                // reference.  Because the policy is to always 'Create' here, we include cross language
                                // references, producing skeletons for them if necessary.
                                var metadataReference = await compilationState.GetMetadataReferenceAsync(
                                    projectReference, this.ProjectState, includeCrossLanguage: true, cancellationToken).ConfigureAwait(false);
                                AddMetadataReference(projectReference, metadataReference);
                            }
                            else
                            {
                                Contract.ThrowIfFalse(creationPolicy.SkeletonReferenceCreationPolicy is SkeletonReferenceCreationPolicy.CreateIfAbsent or SkeletonReferenceCreationPolicy.DoNotCreate);

                                // Client does not want to force a skeleton reference to be created.  Try to get a
                                // metadata reference cheaply in the case where this is a reference to the same
                                // language.  If that fails, also attempt to get a reference to a skeleton assembly
                                // produced from one of our prior stale compilations.
                                var metadataReference = await compilationState.GetMetadataReferenceAsync(
                                    projectReference, this.ProjectState, includeCrossLanguage: false, cancellationToken).ConfigureAwait(false);
                                if (metadataReference is null)
                                {
                                    var inProgressCompilationNotRef = staleCompilationWithGeneratedDocuments ?? compilationWithoutGeneratedDocuments;
                                    metadataReference = inProgressCompilationNotRef.ExternalReferences.FirstOrDefault(
                                        r => GetProjectId(inProgressCompilationNotRef.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol) == projectReference.ProjectId);
                                }

                                // If we still failed, but our policy is to create when absent, then do the work to
                                // create a real skeleton here.
                                if (metadataReference is null && creationPolicy.SkeletonReferenceCreationPolicy is SkeletonReferenceCreationPolicy.CreateIfAbsent)
                                {
                                    metadataReference = await compilationState.GetMetadataReferenceAsync(
                                        projectReference, this.ProjectState, includeCrossLanguage: true, cancellationToken).ConfigureAwait(false);
                                }

                                AddMetadataReference(projectReference, metadataReference);
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
                        staleCompilationWithGeneratedDocuments = staleCompilationWithGeneratedDocuments?.WithReferences(newReferences);
                    }

                    // We will finalize the compilation by adding full contents here.
                    var (compilationWithGeneratedDocuments, nextGeneratorInfo) = await AddExistingOrComputeNewGeneratorInfoAsync(
                        creationPolicy,
                        compilationState,
                        compilationWithoutGeneratedDocuments,
                        generatorInfo,
                        staleCompilationWithGeneratedDocuments,
                        cancellationToken).ConfigureAwait(false);

                    // Our generated documents are up to date if we just created them.  Note: when in balanced mode, we
                    // will then change our creation policy below to DoNotCreate.  This means that any successive forks
                    // will move us to an in-progress-state that is not running generators.  And the next time we get
                    // here and produce a final compilation, this will then be 'false' since we'll be reusing old
                    // generated docs.
                    //
                    // This flag can then be used later when we hear about external user events (like save/build) to
                    // decide if we need to do anything.  If the generated documents are up to date, then we don't need
                    // to do anything in that case.
                    var generatedDocumentsUpToDate = creationPolicy.GeneratedDocumentCreationPolicy == GeneratedDocumentCreationPolicy.Create;

                    // If the user has the option set to only run generators to something other than 'automatic' then we
                    // want to set ourselves to not run generators again now that generators have run.  That way, any
                    // further *automatic* changes to the solution will not run generators again.  Instead, when one of
                    // those external events happen, we'll grab the workspace's solution, transition all states *out* of
                    // this state and then let the next 'GetCompilationAsync' operation cause generators to run.
                    //
                    // Similarly, we don't want to automatically create skeletons at this point (unless they're missing
                    // entirely).

                    var workspacePreference = compilationState.Services.GetRequiredService<IWorkspaceConfigurationService>().Options.SourceGeneratorExecution;
                    if (workspacePreference != SourceGeneratorExecutionPreference.Automatic)
                    {
                        if (creationPolicy.GeneratedDocumentCreationPolicy == GeneratedDocumentCreationPolicy.Create)
                            creationPolicy = creationPolicy with { GeneratedDocumentCreationPolicy = GeneratedDocumentCreationPolicy.DoNotCreate };

                        if (creationPolicy.SkeletonReferenceCreationPolicy == SkeletonReferenceCreationPolicy.Create)
                            creationPolicy = creationPolicy with { SkeletonReferenceCreationPolicy = SkeletonReferenceCreationPolicy.CreateIfAbsent };
                    }

                    var finalState = FinalCompilationTrackerState.Create(
                        creationPolicy,
                        generatedDocumentsUpToDate,
                        compilationWithGeneratedDocuments,
                        compilationWithoutGeneratedDocuments,
                        hasSuccessfullyLoaded,
                        nextGeneratorInfo,
                        this.ProjectState.Id,
                        metadataReferenceToProjectId);

                    this.WriteState(finalState);

                    return finalState;

                    void AddMetadataReference(ProjectReference projectReference, MetadataReference? metadataReference)
                    {
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

            public Task<bool> HasSuccessfullyLoadedAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                return this.ReadState() is FinalCompilationTrackerState finalState
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

            public ICompilationTracker WithCreateCreationPolicy(bool forceRegeneration)
            {
                var state = this.ReadState();

                var desiredCreationPolicy = CreationPolicy.Create;

                // If we've computed no state yet there's nothing to do.  This state will automatically transition 
                // to an InProgressState with a creation policy of 'Create' anyways.
                if (state is null)
                    return this;

                // If we're not forcing regeneration, we can bail out from doing work in a few cases.
                if (!forceRegeneration)
                {
                    // First If we're *already* in the state where we are running generators and skeletons we don't need
                    // to do anything and can just return ourselves. The next request to create the compilation will do
                    // so fully.
                    if (state.CreationPolicy == desiredCreationPolicy)
                        return this;

                    // Second, if we know we are already in a final compilation state where the generated documents were
                    // produced, then clearly we don't need to do anything.  Nothing changed between then and now, so we
                    // can reuse the final compilation as is.
                    if (state is FinalCompilationTrackerState { GeneratedDocumentsUpToDate: true })
                        return this;
                }

                // If we're forcing regeneration then we have to drop whatever driver we have so that we'll start from
                // scratch next time around.
                var desiredGeneratorInfo = forceRegeneration ? state.GeneratorInfo with { Driver = null } : state.GeneratorInfo;

                var newState = state switch
                {
                    InProgressState inProgressState => new InProgressState(
                        desiredCreationPolicy,
                        inProgressState.LazyCompilationWithoutGeneratedDocuments,
                        desiredGeneratorInfo,
                        inProgressState.LazyStaleCompilationWithGeneratedDocuments,
                        inProgressState.PendingTranslationActions),
                    // Transition the final frozen state we have back to an in-progress state that will then compute
                    // generators and skeletons.
                    FinalCompilationTrackerState finalState => new InProgressState(
                        desiredCreationPolicy,
                        finalState.CompilationWithoutGeneratedDocuments,
                        desiredGeneratorInfo,
                        finalState.FinalCompilationWithGeneratedDocuments,
                        pendingTranslationActions: []),
                    _ => throw ExceptionUtilities.UnexpectedValue(state.GetType()),
                };

                return new RegularCompilationTracker(
                    this.ProjectState,
                    newState,
                    skeletonReferenceCacheToClone: _skeletonReferenceCache);
            }

            public ICompilationTracker WithDoNotCreateCreationPolicy(CancellationToken cancellationToken)
            {
                var state = this.ReadState();

                // We're freezing the solution for features where latency performance is paramount.  Do not run SGs or
                // create skeleton references at this point.  Just use whatever we've already generated for each in the
                // past.
                var desiredCreationPolicy = CreationPolicy.DoNotCreate;

                if (state is FinalCompilationTrackerState finalState)
                {
                    var newFinalState = finalState.WithCreationPolicy(desiredCreationPolicy);
                    return newFinalState == finalState
                        ? this
                        : new RegularCompilationTracker(this.ProjectState, newFinalState, skeletonReferenceCacheToClone: _skeletonReferenceCache);
                }

                // Non-final state currently.  Produce an in-progress-state containing the forked change. Note: we
                // transition to in-progress-state here (and not final-state) as we still want to leverage all the
                // final-state-transition logic contained in FinalizeCompilationAsync (for example, properly setting
                // up all references).
                if (state is null)
                {
                    // We may have already parsed some of the documents in this compilation.  For example, if we're
                    // partway through the logic in BuildInProgressStateFromNoCompilationStateAsync.  If so, move those
                    // parsed documents over to the new project state so we can preserve as much information as
                    // possible.

                    // Note: this count may be inaccurate as parsing may be going on in the background.  However, it
                    // acts as a reasonable lower bound for the number of documents we'll be adding.
                    var alreadyParsedCount = this.ProjectState.DocumentStates.States.Count(static s => s.Value.TryGetSyntaxTree(out _));

                    // Specifically an ImmutableArray.Builder as we can presize reasonably and we want to convert to an
                    // ImmutableArray at the end.
                    var documentsWithTreesBuilder = ImmutableArray.CreateBuilder<DocumentState>(alreadyParsedCount);
                    var alreadyParsedTreesBuilder = ImmutableArray.CreateBuilder<SyntaxTree>(alreadyParsedCount);

                    foreach (var documentState in this.ProjectState.DocumentStates.GetStatesInCompilationOrder())
                    {
                        if (documentState.TryGetSyntaxTree(out var alreadyParsedTree))
                        {
                            documentsWithTreesBuilder.Add(documentState);
                            alreadyParsedTreesBuilder.Add(alreadyParsedTree);
                        }
                    }

                    // Transition us to a state that only has documents for the files we've already parsed.
                    var frozenProjectState = this.ProjectState
                        .RemoveAllNormalDocuments()
                        .AddDocuments(documentsWithTreesBuilder.ToImmutableAndClear());

                    // Defer creating these compilations.  It's common to freeze projects (as part of a solution freeze)
                    // that are then never examined.  Creating compilations can be a little costly, so this saves doing
                    // that to the point where it is truly needed.
                    var alreadyParsedTrees = alreadyParsedTreesBuilder.ToImmutableAndClear();
                    var lazyCompilationWithoutGeneratedDocuments = new Lazy<Compilation>(() => this.CreateEmptyCompilation().AddSyntaxTrees(alreadyParsedTrees));

                    // Safe cast to appease NRT system.
                    var lazyCompilationWithGeneratedDocuments = (Lazy<Compilation?>)lazyCompilationWithoutGeneratedDocuments!;

                    return new RegularCompilationTracker(
                        frozenProjectState,
                        new InProgressState(
                            desiredCreationPolicy,
                            lazyCompilationWithoutGeneratedDocuments,
                            CompilationTrackerGeneratorInfo.Empty,
                            lazyCompilationWithGeneratedDocuments,
                            pendingTranslationActions: []),
                        skeletonReferenceCacheToClone: _skeletonReferenceCache);
                }
                else if (state is InProgressState inProgressState)
                {
                    // If we have an in progress state with no steps, then we're just at the current project state.
                    // Otherwise, reset us to whatever state the InProgressState had currently transitioned to.

                    var frozenProjectState = inProgressState.PendingTranslationActions.IsEmpty
                        ? this.ProjectState
                        : inProgressState.PendingTranslationActions.First().OldProjectState;

                    // Grab whatever is in the in-progress-state so far, add any generated docs, and snap 
                    // us to a frozen state with that information.
                    var generatorInfo = inProgressState.GeneratorInfo;
                    var compilationWithoutGeneratedDocuments = inProgressState.LazyCompilationWithoutGeneratedDocuments;
                    var compilationWithGeneratedDocuments = new Lazy<Compilation?>(() => compilationWithoutGeneratedDocuments.Value.AddSyntaxTrees(
                        generatorInfo.Documents.States.Values.Select(state => state.GetSyntaxTree(cancellationToken))));

                    return new RegularCompilationTracker(
                        frozenProjectState,
                        new InProgressState(
                            desiredCreationPolicy,
                            compilationWithoutGeneratedDocuments,
                            generatorInfo,
                            compilationWithGeneratedDocuments,
                            pendingTranslationActions: []),
                        skeletonReferenceCacheToClone: _skeletonReferenceCache);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(state.GetType());
                }
            }

            public async ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(
                SolutionCompilationState compilationState, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken)
            {
                // Note: withFrozenSourceGeneratedDocuments has no impact on is.  We're always returning real generated
                // docs, not frozen docs.  Frozen docs are only involved with a
                // WithFrozenSourceGeneratedDocumentsCompilationTracker

                // If we don't have any generators, then we know we have no generated files, so we can skip the computation entirely.
                if (!await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false))
                    return TextDocumentStates<SourceGeneratedDocumentState>.Empty;

                var finalState = await GetOrBuildFinalStateAsync(
                    compilationState, cancellationToken: cancellationToken).ConfigureAwait(false);
                return finalState.GeneratorInfo.Documents;
            }

            public async ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
                SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (!await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false))
                    return [];

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
                    if (!result.Diagnostics.IsDefaultOrEmpty)
                    {
                        builder.AddRange(result.Diagnostics);
                    }
                }

                return builder.ToImmutableAndClear();
            }

            public async ValueTask<GeneratorDriverRunResult?> GetSourceGeneratorRunResultAsync(SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                if (!await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false))
                    return null;

                var finalState = await GetOrBuildFinalStateAsync(
                    compilationState, cancellationToken).ConfigureAwait(false);

                return finalState.GeneratorInfo.Driver?.GetRunResult();
            }

            public SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(DocumentId documentId)
            {
                var state = ReadState();

                // If we are in FinalState, then we have correctly ran generators and then know the final contents of the
                // Compilation. The GeneratedDocuments can be filled for intermediate states, but those aren't guaranteed to be
                // correct and can be re-ran later.
                return state is FinalCompilationTrackerState finalState ? finalState.GeneratorInfo.Documents.GetState(documentId) : null;
            }

            public SkeletonReferenceCache GetClonedSkeletonReferenceCache()
                => _skeletonReferenceCache.Clone();

            public Task<MetadataReference?> GetOrBuildSkeletonReferenceAsync(SolutionCompilationState compilationState, MetadataReferenceProperties properties, CancellationToken cancellationToken)
                => _skeletonReferenceCache.GetOrBuildReferenceAsync(this, compilationState, properties, cancellationToken);

            /// <summary>
            /// Validates the compilation is consistent and we didn't have a bug in producing it. This only runs under a feature flag.
            /// </summary>
            private void ValidateState(CompilationTrackerState? state)
            {
                if (state is null)
                    return;

                if (!_validateStates)
                    return;

                if (state is FinalCompilationTrackerState finalState)
                {
                    ValidateCompilationTreesMatchesProjectState(finalState.FinalCompilationWithGeneratedDocuments, ProjectState, finalState.GeneratorInfo);
                }
                else if (state is InProgressState inProgressState)
                {
                    var projectState = inProgressState.PendingTranslationActions is [var translationAction, ..]
                        ? translationAction.OldProjectState
                        : this.ProjectState;

                    ValidateCompilationTreesMatchesProjectState(inProgressState.CompilationWithoutGeneratedDocuments, projectState, generatorInfo: null);

                    if (inProgressState.LazyStaleCompilationWithGeneratedDocuments.Value != null)
                    {
                        ValidateCompilationTreesMatchesProjectState(inProgressState.LazyStaleCompilationWithGeneratedDocuments.Value, projectState, inProgressState.GeneratorInfo);
                    }
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(state.GetType());
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
            /// This is just the same as <see cref="Contract.ThrowIfFalse(bool, string, int, string)"/> but throws a custom exception type to make this easier to find in telemetry since the exception type
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
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(
                        ref _lazyDependentVersion,
                        AsyncLazy.Create(static (arg, c) =>
                            arg.self.ComputeDependentVersionAsync(arg.compilationState, c),
                            arg: (self: this, compilationState)),
                        null);
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
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(
                        ref _lazyDependentSemanticVersion,
                        AsyncLazy.Create(static (arg, c) =>
                            arg.self.ComputeDependentSemanticVersionAsync(arg.compilationState, c),
                            arg: (self: this, compilationState))
                        , null);
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
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(
                        ref _lazyDependentChecksum,
                        AsyncLazy.Create(static (arg, c) =>
                            arg.self.ComputeDependentChecksumAsync(arg.SolutionState, c),
                            arg: (self: this, compilationState.SolutionState)),
                        null);
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
