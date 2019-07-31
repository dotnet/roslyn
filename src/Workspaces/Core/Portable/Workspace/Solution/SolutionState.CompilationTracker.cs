// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
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
        private partial class CompilationTracker
        {
            private static readonly Func<ProjectState, string> s_logBuildCompilationAsync = LogBuildCompilationAsync;

            public ProjectState ProjectState { get; }

            /// <summary>
            /// Access via the <see cref="ReadState"/> and <see cref="WriteState"/> methods.
            /// </summary>
            private State _stateDoNotAccessDirectly;

            // guarantees only one thread is building at a time
            private readonly SemaphoreSlim _buildLock = new SemaphoreSlim(initialCount: 1);

            private CompilationTracker(
                ProjectState project,
                State state)
            {
                Contract.ThrowIfNull(project);

                this.ProjectState = project;
                _stateDoNotAccessDirectly = state;
            }

            /// <summary>
            /// Creates a tracker for the provided project.  The tracker will be in the 'empty' state
            /// and will have no extra information beyond the project itself.
            /// </summary>
            public CompilationTracker(ProjectState project)
                : this(project, State.Empty)
            {
            }

            private State ReadState()
            {
                return Volatile.Read(ref _stateDoNotAccessDirectly);
            }

            private void WriteState(State state, SolutionState solution)
            {
                if (solution._solutionServices.SupportsCachingRecoverableObjects)
                {
                    // Allow the cache service to create a strong reference to the compilation
                    solution._solutionServices.CacheService.CacheObjectIfCachingEnabledForKey(this.ProjectState.Id, state, state.Compilation.GetValue());
                }

                Volatile.Write(ref _stateDoNotAccessDirectly, state);
            }

            /// <summary>
            /// Returns true if this tracker currently either points to a compilation, has an in-progress
            /// compilation being computed, or has a skeleton reference.  Note: this is simply a weak
            /// statement about the tracker at this exact moment in time.  Immediately after this returns
            /// the tracker might change and may no longer have a final compilation (for example, if the
            /// retainer let go of it) or might not have an in-progress compilation (for example, if the
            /// background compiler finished with it).
            /// 
            /// Because of the above limitations, this should only be used by clients as a weak form of
            /// information about the tracker.  For example, a client may see that a tracker has no
            /// compilation and may choose to throw it away knowing that it could be reconstructed at a
            /// later point if necessary.
            /// </summary>
            public bool HasCompilation
            {
                get
                {
                    var state = this.ReadState();
                    return state.Compilation.HasValue || state.DeclarationOnlyCompilation != null;
                }
            }

            /// <summary>
            /// Creates a new instance of the compilation info, retaining any already built
            /// compilation state as the now 'old' state
            /// </summary>
            public CompilationTracker Fork(
                ProjectState newProject,
                CompilationTranslationAction translate = null,
                bool clone = false,
                CancellationToken cancellationToken = default)
            {
                var state = this.ReadState();

                var baseCompilationSource = state.Compilation;
                var baseCompilation = baseCompilationSource.GetValue(cancellationToken);
                if (baseCompilation != null)
                {
                    // We have some pre-calculated state to incrementally update
                    var newInProgressCompilation = clone
                        ? baseCompilation.Clone()
                        : baseCompilation;

                    var intermediateProjects = state is InProgressState
                        ? ((InProgressState)state).IntermediateProjects
                        : ImmutableArray.Create<ValueTuple<ProjectState, CompilationTranslationAction>>();

                    var newIntermediateProjects = translate == null
                         ? intermediateProjects
                         : intermediateProjects.Add(ValueTuple.Create(this.ProjectState, translate));

                    var newState = State.Create(newInProgressCompilation, newIntermediateProjects);

                    return new CompilationTracker(newProject, newState);
                }

                var declarationOnlyCompilation = state.DeclarationOnlyCompilation;
                if (declarationOnlyCompilation != null)
                {
                    if (translate != null)
                    {
                        var intermediateProjects =
                            ImmutableArray.Create<ValueTuple<ProjectState, CompilationTranslationAction>>(ValueTuple.Create(this.ProjectState, translate));

                        return new CompilationTracker(newProject, new InProgressState(declarationOnlyCompilation, intermediateProjects));
                    }

                    return new CompilationTracker(newProject, new LightDeclarationState(declarationOnlyCompilation));
                }

                // We have nothing.  Just make a tracker that only points to the new project.  We'll have
                // to rebuild its compilation from scratch if anyone asks for it.
                return new CompilationTracker(newProject);
            }

            /// <summary>
            /// Creates a fork with the same final project.
            /// </summary>
            public CompilationTracker Clone()
            {
                return this.Fork(this.ProjectState, clone: true);
            }

            public CompilationTracker FreezePartialStateWithTree(SolutionState solution, DocumentState docState, SyntaxTree tree, CancellationToken cancellationToken)
            {
                GetPartialCompilationState(solution, docState.Id, out var inProgressProject, out var inProgressCompilation, cancellationToken);

                if (!inProgressCompilation.SyntaxTrees.Contains(tree))
                {
                    var existingTree = inProgressCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == tree.FilePath);
                    if (existingTree != null)
                    {
                        inProgressCompilation = inProgressCompilation.ReplaceSyntaxTree(existingTree, tree);
                        inProgressProject = inProgressProject.UpdateDocument(docState, textChanged: false, recalculateDependentVersions: false);
                    }
                    else
                    {
                        inProgressCompilation = inProgressCompilation.AddSyntaxTrees(tree);
                        Debug.Assert(!inProgressProject.DocumentIds.Contains(docState.Id));
                        inProgressProject = inProgressProject.AddDocuments(ImmutableArray.Create(docState));
                    }
                }

                // The user is asking for an in progress snap.  We don't want to create it and then
                // have the compilation immediately disappear.  So we force it to stay around with a ConstantValueSource.
                // As a policy, all partial-state projects are said to have incomplete references, since the state has no guarantees.
                return new CompilationTracker(inProgressProject,
                    new FinalState(new ConstantValueSource<Compilation>(inProgressCompilation), hasSuccessfullyLoaded: false));
            }

            /// <summary>
            /// Tries to get the latest snapshot of the compilation without waiting for it to be
            /// fully built. This method takes advantage of the progress side-effect produced during
            /// <see cref="BuildCompilationInfoAsync(SolutionState, CancellationToken)"/>. It will either return the already built compilation, any
            /// in-progress compilation or any known old compilation in that order of preference.
            /// The compilation state that is returned will have a compilation that is retained so
            /// that it cannot disappear.
            /// </summary>
            private void GetPartialCompilationState(
                SolutionState solution,
                DocumentId id,
                out ProjectState inProgressProject,
                out Compilation inProgressCompilation,
                CancellationToken cancellationToken)
            {
                var state = this.ReadState();
                inProgressCompilation = state.Compilation.GetValue(cancellationToken);

                // check whether we can bail out quickly for typing case
                var inProgressState = state as InProgressState;

                // all changes left for this document is modifying the given document.
                // we can use current state as it is since we will replace the document with latest document anyway.
                if (inProgressState != null &&
                    inProgressCompilation != null &&
                    inProgressState.IntermediateProjects.All(t => IsTouchDocumentActionForDocument(t, id)))
                {
                    inProgressProject = this.ProjectState;

                    SolutionLogger.UseExistingPartialProjectState();
                    return;
                }

                inProgressProject = inProgressState != null ? inProgressState.IntermediateProjects.First().Item1 : this.ProjectState;

                // if we already have a final compilation we are done.
                if (inProgressCompilation != null && state is FinalState)
                {
                    SolutionLogger.UseExistingFullProjectState();
                    return;
                }

                // 1) if we have an in-progress compilation use it.  
                // 2) If we don't, then create a simple empty compilation/project. 
                // 3) then, make sure that all it's p2p refs and whatnot are correct.
                if (inProgressCompilation == null)
                {
                    inProgressProject = inProgressProject.RemoveAllDocuments();
                    inProgressCompilation = this.CreateEmptyCompilation();
                }

                // first remove all project from the project and compilation.
                inProgressProject = inProgressProject.WithProjectReferences(ImmutableArray.Create<ProjectReference>());

                // Now add in back a consistent set of project references.  For project references
                // try to get either a CompilationReference or a SkeletonReference. This ensures
                // that the in-progress project only reports a reference to another project if it
                // could actually get a reference to that project's metadata.
                var metadataReferences = new List<MetadataReference>();
                var newProjectReferences = new List<ProjectReference>();
                metadataReferences.AddRange(this.ProjectState.MetadataReferences);

                var metadataReferenceToProjectId = new Dictionary<MetadataReference, ProjectId>();

                foreach (var projectReference in this.ProjectState.ProjectReferences)
                {
                    var referencedProject = solution.GetProjectState(projectReference.ProjectId);
                    if (referencedProject != null)
                    {
                        if (referencedProject.IsSubmission)
                        {
                            var compilation = solution.GetCompilationAsync(projectReference.ProjectId, cancellationToken).WaitAndGetResult(cancellationToken);
                            inProgressCompilation = inProgressCompilation.WithScriptCompilationInfo(inProgressCompilation.ScriptCompilationInfo.WithPreviousScriptCompilation(compilation));
                        }
                        else
                        {
                            // get the latest metadata for the partial compilation of the referenced project.
                            var metadata = solution.GetPartialMetadataReference(projectReference, this.ProjectState, cancellationToken);

                            if (metadata == null)
                            {
                                // if we failed to get the metadata, check to see if we previously had existing metadata and reuse it instead.
                                var inProgressCompilationNotRef = inProgressCompilation;
                                metadata = inProgressCompilationNotRef.ExternalReferences.FirstOrDefault(
                                    r => solution.GetProjectState(inProgressCompilationNotRef.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol, cancellationToken)?.Id == projectReference.ProjectId);
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

                inProgressProject = inProgressProject.AddProjectReferences(newProjectReferences);
                inProgressCompilation = UpdateCompilationWithNewReferencesAndRecordAssemblySymbols(inProgressCompilation, metadataReferences, metadataReferenceToProjectId);

                SolutionLogger.CreatePartialProjectState();
            }

            private static bool IsTouchDocumentActionForDocument(ValueTuple<ProjectState, CompilationTranslationAction> tuple, DocumentId id)
            {
                var touchDocumentAction = tuple.Item2 as CompilationTranslationAction.TouchDocumentAction;
                return touchDocumentAction != null && touchDocumentAction.DocumentId == id;
            }

            /// <summary>
            /// Gets the final compilation if it is available.
            /// </summary>
            public bool TryGetCompilation(out Compilation compilation)
            {
                var state = this.ReadState();
                return state.FinalCompilation.TryGetValue(out compilation) && compilation != null;
            }

            public Task<Compilation> GetCompilationAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (this.TryGetCompilation(out var compilation))
                {
                    // PERF: This is a hot code path and Task<TResult> isn't cheap,
                    // so cache the completed tasks to reduce allocations. We also
                    // need to avoid keeping a strong reference to the Compilation,
                    // so use a ConditionalWeakTable.
                    return SpecializedTasks.FromResult(compilation);
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

            private static string LogBuildCompilationAsync(ProjectState state)
            {
                return string.Join(",", state.AssemblyName, state.DocumentIds.Count);
            }

            private async Task<Compilation> GetOrBuildDeclarationCompilationAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (await _buildLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var state = this.ReadState();

                        // we are already in the final stage. just return it.
                        var compilation = state.FinalCompilation.GetValue(cancellationToken);
                        if (compilation != null)
                        {
                            return compilation;
                        }

                        compilation = state.Compilation.GetValue(cancellationToken);
                        if (compilation == null)
                        {
                            // let's see whether we have declaration only compilation
                            if (state.DeclarationOnlyCompilation != null)
                            {
                                // okay, move to full declaration state. do this so that declaration only compilation never
                                // realize symbols.
                                var declarationOnlyCompilation = state.DeclarationOnlyCompilation.Clone();
                                this.WriteState(new FullDeclarationState(declarationOnlyCompilation), solution);
                                return declarationOnlyCompilation;
                            }

                            // We've got nothing.  Build it from scratch :(
                            return await BuildDeclarationCompilationFromScratchAsync(solution, cancellationToken).ConfigureAwait(false);
                        }
                        else if (state is FullDeclarationState)
                        {
                            // we have full declaration, just use it.
                            return state.Compilation.GetValue(cancellationToken);
                        }
                        else if (state is InProgressState inProgress)
                        {
                            // We have an in progress compilation.  Build off of that.
                            return await BuildDeclarationCompilationFromInProgressAsync(solution, inProgress, compilation, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<CompilationInfo> GetOrBuildCompilationInfoAsync(
                SolutionState solution,
                bool lockGate,
                CancellationToken cancellationToken)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.Workspace_Project_CompilationTracker_BuildCompilationAsync,
                                           s_logBuildCompilationAsync, this.ProjectState, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var state = this.ReadState();

                        // Try to get the built compilation.  If it exists, then we can just return that.
                        var finalCompilation = state.FinalCompilation.GetValue(cancellationToken);
                        if (finalCompilation != null)
                        {
                            return new CompilationInfo(finalCompilation, state.HasSuccessfullyLoaded.Value);
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
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            /// <summary>
            /// Builds the compilation matching the project state. In the process of building, also
            /// produce in progress snapshots that can be accessed from other threads.
            /// </summary>
            private Task<CompilationInfo> BuildCompilationInfoAsync(
                SolutionState solution,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var state = this.ReadState();

                // if we already have a compilation, we must be already done!  This can happen if two
                // threads were waiting to build, and we came in after the other succeeded.
                var compilation = state.FinalCompilation.GetValue(cancellationToken);
                if (compilation != null)
                {
                    return Task.FromResult(new CompilationInfo(compilation, state.HasSuccessfullyLoaded.Value));
                }

                compilation = state.Compilation.GetValue(cancellationToken);
                if (compilation == null)
                {
                    // this can happen if compilation is already kicked out from the cache.
                    // check whether the state we have support declaration only compilation
                    if (state.DeclarationOnlyCompilation != null)
                    {
                        // we have declaration only compilation. build final one from it.
                        return FinalizeCompilationAsync(solution, state.DeclarationOnlyCompilation, cancellationToken);
                    }

                    // We've got nothing.  Build it from scratch :(
                    return BuildCompilationInfoFromScratchAsync(solution, state, cancellationToken);
                }
                else if (state is FullDeclarationState)
                {
                    // We have a declaration compilation, use it to reconstruct the final compilation
                    return this.FinalizeCompilationAsync(solution, compilation, cancellationToken);
                }
                else if (state is InProgressState inProgress)
                {
                    // We have an in progress compilation.  Build off of that.
                    return BuildFinalStateFromInProgressStateAsync(solution, inProgress, compilation, cancellationToken);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<CompilationInfo> BuildCompilationInfoFromScratchAsync(
                SolutionState solution, State state, CancellationToken cancellationToken)
            {
                try
                {
                    var compilation = await BuildDeclarationCompilationFromScratchAsync(solution, cancellationToken).ConfigureAwait(false);
                    return await FinalizeCompilationAsync(solution, compilation, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            [PerformanceSensitive(
                "https://github.com/dotnet/roslyn/issues/23582",
                Constraint = "Avoid calling " + nameof(Compilation.AddSyntaxTrees) + " in a loop due to allocation overhead.")]
            private async Task<Compilation> BuildDeclarationCompilationFromScratchAsync(
                SolutionState solution, CancellationToken cancellationToken)
            {
                try
                {
                    var compilation = CreateEmptyCompilation();

                    var trees = new SyntaxTree[ProjectState.DocumentIds.Count];
                    var index = 0;
                    foreach (var document in this.ProjectState.OrderedDocumentStates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        trees[index] = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        index++;
                    }

                    compilation = compilation.AddSyntaxTrees(trees);
                    this.WriteState(new FullDeclarationState(compilation), solution);
                    return compilation;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private Compilation CreateEmptyCompilation()
            {
                var compilationFactory = this.ProjectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                if (this.ProjectState.IsSubmission)
                {
                    return compilationFactory.CreateSubmissionCompilation(
                        this.ProjectState.AssemblyName,
                        this.ProjectState.CompilationOptions,
                        this.ProjectState.HostObjectType);
                }
                else
                {
                    return compilationFactory.CreateCompilation(
                        this.ProjectState.AssemblyName,
                        this.ProjectState.CompilationOptions);
                }
            }

            private async Task<CompilationInfo> BuildFinalStateFromInProgressStateAsync(
                SolutionState solution, InProgressState state, Compilation inProgressCompilation, CancellationToken cancellationToken)
            {
                try
                {
                    var compilation = await BuildDeclarationCompilationFromInProgressAsync(solution, state, inProgressCompilation, cancellationToken).ConfigureAwait(false);
                    return await FinalizeCompilationAsync(solution, compilation, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<Compilation> BuildDeclarationCompilationFromInProgressAsync(
                SolutionState solution, InProgressState state, Compilation inProgressCompilation, CancellationToken cancellationToken)
            {
                try
                {
                    Debug.Assert(inProgressCompilation != null);
                    var intermediateProjects = state.IntermediateProjects;

                    while (intermediateProjects.Length > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var intermediateProject = intermediateProjects[0];
                        var inProgressProject = intermediateProject.Item1;
                        var action = intermediateProject.Item2;

                        inProgressCompilation = await action.InvokeAsync(inProgressCompilation, cancellationToken).ConfigureAwait(false);
                        intermediateProjects = intermediateProjects.RemoveAt(0);

                        this.WriteState(State.Create(inProgressCompilation, intermediateProjects), solution);
                    }

                    return inProgressCompilation;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private struct CompilationInfo
            {
                public Compilation Compilation { get; }
                public bool HasSuccessfullyLoaded { get; }

                public CompilationInfo(Compilation compilation, bool hasSuccessfullyLoaded)
                {
                    this.Compilation = compilation;
                    this.HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                }
            }

            /// <summary>
            /// Add all appropriate references to the compilation and set it as our final compilation
            /// state.
            /// </summary>
            private async Task<CompilationInfo> FinalizeCompilationAsync(
                SolutionState solution,
                Compilation compilation,
                CancellationToken cancellationToken)
            {
                try
                {
                    // if HasAllInformation is false, then this project is always not completed.
                    var hasSuccessfullyLoaded = this.ProjectState.HasAllInformation;

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

                                var previousSubmissionCompilation =
                                    await solution.GetCompilationAsync(projectReference.ProjectId, cancellationToken).ConfigureAwait(false);

                                compilation = compilation.WithScriptCompilationInfo(
                                    compilation.ScriptCompilationInfo.WithPreviousScriptCompilation(previousSubmissionCompilation));
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

                    compilation = UpdateCompilationWithNewReferencesAndRecordAssemblySymbols(compilation, newReferences, metadataReferenceToProjectId);

                    this.WriteState(new FinalState(State.CreateValueSource(compilation, solution.Services), hasSuccessfullyLoaded), solution);

                    return new CompilationInfo(compilation, hasSuccessfullyLoaded);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private Compilation UpdateCompilationWithNewReferencesAndRecordAssemblySymbols(Compilation compilation, List<MetadataReference> newReferences, Dictionary<MetadataReference, ProjectId> metadataReferenceToProjectId)
            {
                if (!Enumerable.SequenceEqual(compilation.ExternalReferences, newReferences))
                {
                    compilation = compilation.WithReferences(newReferences);
                }

                // TODO: Record source assembly to project mapping
                // RecordSourceOfAssemblySymbol(compilation.Assembly, this.ProjectState.Id);

                foreach (var kvp in metadataReferenceToProjectId)
                {
                    var metadataReference = kvp.Key;
                    var projectId = kvp.Value;

                    var symbol = compilation.GetAssemblyOrModuleSymbol(metadataReference);

                    RecordSourceOfAssemblySymbol(symbol, projectId);
                }

                return compilation;
            }

            /// <summary>
            /// Get a metadata reference to this compilation info's compilation with respect to
            /// another project. For cross language references produce a skeletal assembly. If the
            /// compilation is not available, it is built. If a skeletal assembly reference is
            /// needed and does not exist, it is also built.
            /// </summary>
            public async Task<MetadataReference> GetMetadataReferenceAsync(
                SolutionState solution,
                ProjectState fromProject,
                ProjectReference projectReference,
                CancellationToken cancellationToken)
            {
                try
                {

                    // if we already have the compilation and its right kind then use it.
                    if (this.ProjectState.LanguageServices == fromProject.LanguageServices
                        && this.TryGetCompilation(out var compilation))
                    {
                        return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                    }

                    // If same language then we can wrap the other project's compilation into a compilation reference
                    if (this.ProjectState.LanguageServices == fromProject.LanguageServices)
                    {
                        // otherwise, base it off the compilation by building it first.
                        compilation = await this.GetCompilationAsync(solution, cancellationToken).ConfigureAwait(false);
                        return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                    }
                    else
                    {
                        // otherwise get a metadata only image reference that is built by emitting the metadata from the referenced project's compilation and re-importing it.
                        return await this.GetMetadataOnlyImageReferenceAsync(solution, projectReference, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            /// <summary>
            /// Attempts to get (without waiting) a metadata reference to a possibly in progress
            /// compilation. Only actual compilation references are returned. Could potentially 
            /// return null if nothing can be provided.
            /// </summary>
            public MetadataReference GetPartialMetadataReference(SolutionState solution, ProjectState fromProject, ProjectReference projectReference, CancellationToken cancellationToken)
            {
                var state = this.ReadState();
                // get compilation in any state it happens to be in right now.
                if (state.Compilation.TryGetValue(out var compilation)
                    && compilation != null
                    && this.ProjectState.LanguageServices == fromProject.LanguageServices)
                {
                    // if we have a compilation and its the correct language, use a simple compilation reference
                    return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                }

                return null;
            }

            /// <summary>
            /// Gets a metadata reference to the metadata-only-image corresponding to the compilation.
            /// </summary>
            private async Task<MetadataReference> GetMetadataOnlyImageReferenceAsync(
                SolutionState solution, ProjectReference projectReference, CancellationToken cancellationToken)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_GetMetadataOnlyImage, cancellationToken))
                    {
                        var version = await this.GetDependentSemanticVersionAsync(solution, cancellationToken).ConfigureAwait(false);

                        // get or build compilation up to declaration state. this compilation will be used to provide live xml doc comment
                        var declarationCompilation = await this.GetOrBuildDeclarationCompilationAsync(solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        solution.Workspace.LogTestMessage($"Looking for a cached skeleton assembly for {projectReference.ProjectId} before taking the lock...");

                        if (!MetadataOnlyReference.TryGetReference(solution, projectReference, declarationCompilation, version, out var reference))
                        {
                            // using async build lock so we don't get multiple consumers attempting to build metadata-only images for the same compilation.
                            using (await _buildLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                            {
                                solution.Workspace.LogTestMessage($"Build lock taken for {ProjectState.Id}...");

                                // okay, we still don't have one. bring the compilation to final state since we are going to use it to create skeleton assembly
                                var compilationInfo = await this.GetOrBuildCompilationInfoAsync(solution, lockGate: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                                reference = MetadataOnlyReference.GetOrBuildReference(solution, projectReference, compilationInfo.Compilation, version, cancellationToken);
                            }
                        }
                        else
                        {
                            solution.Workspace.LogTestMessage($"Reusing the already cached skeleton assembly for {projectReference.ProjectId}");
                        }

                        return reference;
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            /// <summary>
            /// check whether the compilation contains any declaration symbol from syntax trees with
            /// given name
            /// </summary>
            public bool? ContainsSymbolsWithNameFromDeclarationOnlyCompilation(string name, SymbolFilter filter, CancellationToken cancellationToken)
            {
                // DO NOT expose declaration only compilation to outside since it can be held alive long time, we don't want to create any symbol from the declaration only compilation.
                var state = this.ReadState();
                return state.DeclarationOnlyCompilation == null
                    ? default(bool?)
                    : state.DeclarationOnlyCompilation.ContainsSymbolsWithName(name, filter, cancellationToken);
            }

            /// <summary>
            /// check whether the compilation contains any declaration symbol from syntax trees with given name
            /// </summary>
            public bool? ContainsSymbolsWithNameFromDeclarationOnlyCompilation(Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
            {
                // DO NOT expose declaration only compilation to outside since it can be held alive long time, we don't want to create any symbol from the declaration only compilation.
                var state = this.ReadState();
                return state.DeclarationOnlyCompilation == null
                    ? default(bool?)
                    : state.DeclarationOnlyCompilation.ContainsSymbolsWithName(predicate, filter, cancellationToken);
            }

            /// <summary>
            /// get all syntax trees that contain declaration node with the given name
            /// </summary>
            public IEnumerable<SyntaxTree> GetSyntaxTreesWithNameFromDeclarationOnlyCompilation(Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
            {
                var state = this.ReadState();
                if (state.DeclarationOnlyCompilation == null)
                {
                    return null;
                }

                // DO NOT expose declaration only compilation to outside since it can be held alive long time, we don't want to create any symbol from the declaration only compilation.

                // use cloned compilation since this will cause symbols to be created.
                var clone = state.DeclarationOnlyCompilation.Clone();
                return clone.GetSymbolsWithName(predicate, filter, cancellationToken).SelectMany(s => s.DeclaringSyntaxReferences.Select(r => r.SyntaxTree));
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

            #region Versions

            // Dependent Versions are stored on compilation tracker so they are more likely to survive when unrelated solution branching occurs.

            private AsyncLazy<VersionStamp> _lazyDependentVersion;
            private AsyncLazy<VersionStamp> _lazyDependentSemanticVersion;

            public Task<VersionStamp> GetDependentVersionAsync(SolutionState solution, CancellationToken cancellationToken)
            {
                if (_lazyDependentVersion == null)
                {
                    var tmp = solution; // temp. local to avoid a closure allocation for the fast path
                    // note: solution is captured here, but it will go away once GetValueAsync executes.
                    Interlocked.CompareExchange(ref _lazyDependentVersion, new AsyncLazy<VersionStamp>(c => ComputeDependentVersionAsync(tmp, c), cacheResult: true), null);
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
                    Interlocked.CompareExchange(ref _lazyDependentSemanticVersion, new AsyncLazy<VersionStamp>(c => ComputeDependentSemanticVersionAsync(tmp, c), cacheResult: true), null);
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
            #endregion
        }
    }
}
