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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A workspace provides access to a active set of source code projects and documents and their
/// associated syntax trees, compilations and semantic models. A workspace has a current solution
/// that is an immutable snapshot of the projects and documents. This property may change over time
/// as the workspace is updated either from live interactions in the environment or via call to the
/// workspace's <see cref="TryApplyChanges(Solution)"/> method.
/// </summary>
public abstract partial class Workspace : IDisposable
{
    private readonly string? _workspaceKind;
    private readonly HostWorkspaceServices _services;

    private readonly ILegacyGlobalOptionService _legacyOptions;

    // forces serialization of mutation calls from host (OnXXX methods). Must take this lock before taking stateLock.
    private readonly SemaphoreSlim _serializationLock = new(initialCount: 1);

    // this lock guards all the mutable fields (do not share lock with derived classes)
    private readonly NonReentrantLock _stateLock = new(useThisInstanceForSynchronization: true);

    /// <summary>
    /// Current solution.  Must be locked with <see cref="_serializationLock"/> when writing to it.
    /// </summary>
    private Solution _latestSolution;

    private readonly TaskQueue _taskQueue;

    // test hooks.
    internal static bool TestHookStandaloneProjectsDoNotHoldReferences = false;

    /// <summary>
    /// Determines whether changes made to unchangeable documents will be silently ignored or cause exceptions to be thrown
    /// when they are applied to workspace via <see cref="TryApplyChanges(Solution, IProgress{CodeAnalysisProgress})"/>. 
    /// A document is unchangeable if <see cref="IDocumentOperationService.CanApplyChange"/> is false.
    /// </summary>
    internal virtual bool IgnoreUnchangeableDocumentsWhenApplyingChanges { get; } = false;

    /// <summary>
    /// Constructs a new workspace instance.
    /// </summary>
    /// <param name="host">The <see cref="HostServices"/> this workspace uses</param>
    /// <param name="workspaceKind">A string that can be used to identify the kind of workspace. Usually this matches the name of the class.</param>
    protected Workspace(HostServices host, string? workspaceKind)
    {
        _workspaceKind = workspaceKind;

        _services = host.CreateWorkspaceServices(this);

        _legacyOptions = _services.GetRequiredService<ILegacyWorkspaceOptionService>().LegacyGlobalOptions;
        _legacyOptions.RegisterWorkspace(this);

        // queue used for sending events
        var schedulerProvider = _services.GetRequiredService<ITaskSchedulerProvider>();
        var listenerProvider = _services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>();
        _taskQueue = new TaskQueue(listenerProvider.GetListener(), schedulerProvider.CurrentContextScheduler);

        // initialize with empty solution
        var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());

        var emptyOptions = new SolutionOptionSet(_legacyOptions);

        _latestSolution = CreateSolution(info, emptyOptions, analyzerReferences: []);

        _updateSourceGeneratorsQueue = new AsyncBatchingWorkQueue<(ProjectId? projectId, bool forceRegeneration)>(
            // Idle processing speed
            TimeSpan.FromMilliseconds(1500),
            ProcessUpdateSourceGeneratorRequestAsync,
            EqualityComparer<(ProjectId? projectId, bool forceRegeneration)>.Default,
            listenerProvider.GetListener(FeatureAttribute.SourceGenerators),
            _updateSourceGeneratorsQueueTokenSource.Token);
    }

    /// <summary>
    /// Services provider by the host for implementing workspace features.
    /// </summary>
    public HostWorkspaceServices Services => _services;

    /// <summary>
    /// Override this property if the workspace supports partial semantics for documents.
    /// </summary>
    protected internal virtual bool PartialSemanticsEnabled => false;

    /// <summary>
    /// The kind of the workspace.
    /// This is generally <see cref="WorkspaceKind.Host"/> if originating from the host environment, but may be
    /// any other name used for a specific kind of workspace.
    /// </summary>
    // TODO (https://github.com/dotnet/roslyn/issues/37110): decide if Kind should be non-null
    public string? Kind => _workspaceKind;

    /// <summary>
    /// Create a new empty solution instance associated with this workspace.
    /// </summary>
    protected internal Solution CreateSolution(SolutionInfo solutionInfo)
    {
        var options = new SolutionOptionSet(_legacyOptions);
        return CreateSolution(solutionInfo, options, solutionInfo.AnalyzerReferences);
    }

    /// <summary>
    /// Create a new empty solution instance associated with this workspace, and with the given options.
    /// </summary>
    private Solution CreateSolution(SolutionInfo solutionInfo, SolutionOptionSet options, IReadOnlyList<AnalyzerReference> analyzerReferences)
        => new(this, solutionInfo.Attributes, options, analyzerReferences);

    /// <summary>
    /// Create a new empty solution instance associated with this workspace.
    /// </summary>
    protected internal Solution CreateSolution(SolutionId id)
        => CreateSolution(SolutionInfo.Create(id, VersionStamp.Create()));

    /// <summary>
    /// The current solution.
    ///
    /// The solution is an immutable model of the current set of projects and source documents.
    /// It provides access to source text, syntax trees and semantics.
    ///
    /// This property may change as the workspace reacts to changes in the environment or
    /// after <see cref="TryApplyChanges(Solution)"/> is called.
    /// </summary>
    public Solution CurrentSolution
    {
        get
        {
            return Volatile.Read(ref _latestSolution);
        }
    }

    /// <summary>
    /// Sets the <see cref="CurrentSolution"/> of this workspace. This method does not raise a <see cref="WorkspaceChanged"/> event.
    /// </summary>
    /// <remarks>
    /// This method does not guarantee that linked files will have the same contents. Callers
    /// should enforce that policy before passing in the new solution.
    /// </remarks>
    protected Solution SetCurrentSolution(Solution solution)
        => SetCurrentSolutionEx(solution).newSolution;

    /// <summary>
    /// Sets the <see cref="CurrentSolution"/> of this workspace. This method does not raise a <see
    /// cref="WorkspaceChanged"/> event.  This method should be used <em>sparingly</em>.  As much as possible,
    /// derived types should use the SetCurrentSolution overloads that take a transformation.
    /// </summary>
    /// <remarks>
    /// This method does not guarantee that linked files will have the same contents. Callers
    /// should enforce that policy before passing in the new solution.
    /// </remarks>
    private protected (Solution oldSolution, Solution newSolution) SetCurrentSolutionEx(Solution solution)
    {
        if (solution is null)
            throw new ArgumentNullException(nameof(solution));

        using (_serializationLock.DisposableWait())
        {
            var oldSolution = this.CurrentSolution;
            if (solution == oldSolution)
            {
                // No change
                return (solution, solution);
            }

            _latestSolution = solution.WithNewWorkspace(oldSolution.WorkspaceKind, oldSolution.WorkspaceVersion + 1, oldSolution.Services);
            return (oldSolution, _latestSolution);
        }
    }

    /// <inheritdoc cref="SetCurrentSolution(Func{Solution, Solution}, Func{Solution, Solution, ValueTuple{WorkspaceChangeKind, ProjectId?, DocumentId?}}, Action{Solution, Solution}?, Action{Solution, Solution}?)"/>
    internal bool SetCurrentSolution(
        Func<Solution, Solution> transformation,
        WorkspaceChangeKind changeKind,
        ProjectId? projectId = null,
        DocumentId? documentId = null,
        Action<Solution, Solution>? onBeforeUpdate = null,
        Action<Solution, Solution>? onAfterUpdate = null)
    {
        var (updated, _) = SetCurrentSolution(
            transformation,
            (_, _) => (changeKind, projectId, documentId),
            onBeforeUpdate,
            onAfterUpdate);
        return updated;
    }

    /// <summary>
    /// Applies specified transformation to <see cref="CurrentSolution"/>, updates <see cref="CurrentSolution"/> to
    /// the new value and raises a workspace change event of the specified kind.  All linked documents in the
    /// solution (which normally will have the same content values) will be updated to to have the same content
    /// *identity*.  In other words, they will point at the same <see cref="ITextAndVersionSource"/> instances,
    /// allowing that memory to be shared.
    /// </summary>
    /// <param name="transformation">Solution transformation.</param>
    /// <param name="changeKind">The kind of workspace change event to raise. The id of the project updated by
    /// <paramref name="transformation"/> to be passed to the workspace change event.  And the id of the document
    /// updated by <paramref name="transformation"/> to be passed to the workspace change event.</param>
    /// <returns>True if <see cref="CurrentSolution"/> was set to the transformed solution, false if the
    /// transformation did not change the solution.</returns>
    internal (bool updated, Solution newSolution) SetCurrentSolution(
        Func<Solution, Solution> transformation,
        Func<Solution, Solution, (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId)> changeKind,
        Action<Solution, Solution>? onBeforeUpdate = null,
        Action<Solution, Solution>? onAfterUpdate = null)
    {
#pragma warning disable CA2012 // Use ValueTasks correctly
        var valueTask = SetCurrentSolutionAsync(
            useAsync: false,
            transformation,
            changeKind,
            onBeforeUpdate,
            onAfterUpdate,
            CancellationToken.None);

        return valueTask.VerifyCompleted("Task must have completed synchronously as we passed 'useAsync: false' to SetCurrentSolutionAsync");
#pragma warning restore CA2012 // Use ValueTasks correctly
    }

    internal async ValueTask<(bool updated, Solution newSolution)> SetCurrentSolutionAsync(
        bool useAsync,
        Func<Solution, Solution> transformation,
        Func<Solution, Solution, (WorkspaceChangeKind changeKind, ProjectId? projectId, DocumentId? documentId)> changeKind,
        Action<Solution, Solution>? onBeforeUpdate,
        Action<Solution, Solution>? onAfterUpdate,
        CancellationToken cancellationToken)
    {
        var (oldSolution, newSolution) = await SetCurrentSolutionAsync(
            useAsync,
            data: (@this: this, transformation, onBeforeUpdate, onAfterUpdate, changeKind),
            transformation: static (oldSolution, data) =>
            {
                var newSolution = data.transformation(oldSolution);

                // Attempt to unify the syntax trees in the new solution.
                return UnifyLinkedDocumentContents(oldSolution, newSolution);
            },
            mayRaiseEvents: true,
            onBeforeUpdate: static (oldSolution, newSolution, data) =>
            {
                data.onBeforeUpdate?.Invoke(oldSolution, newSolution);
            },
            onAfterUpdate: static (oldSolution, newSolution, data) =>
            {
                data.onAfterUpdate?.Invoke(oldSolution, newSolution);

                // Queue the event but don't execute its handlers on this thread.
                // Doing so under the serialization lock guarantees the same ordering of the events
                // as the order of the changes made to the solution.
                var (changeKind, projectId, documentId) = data.changeKind(oldSolution, newSolution);
                data.@this.RaiseWorkspaceChangedEventAsync(changeKind, oldSolution, newSolution, projectId, documentId);
            },
            cancellationToken).ConfigureAwait(false);

        return (oldSolution != newSolution, newSolution);

        static Solution UnifyLinkedDocumentContents(Solution oldSolution, Solution newSolution)
        {
            // note: if it turns out this is too expensive, we could consider using the passed in projectId/document
            // to limit the set of changes we look at.  However, GetChanges *should* be fairly fast as it does
            // workspace-green-node identity checks to quickly narrow down what changed.

            var changes = newSolution.GetChanges(oldSolution);

            using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var addedDocumentIds);
            using var _2 = PooledHashSet<DocumentId>.GetInstance(out var changedDocumentIds);

            // For all added documents, see if they link to an existing document.  If so, use that existing documents text/tree.
            foreach (var addedProject in changes.GetAddedProjects())
            {
                // Ignore projects that don't even have syntax trees to share.
                if (!addedProject.SupportsCompilation)
                    continue;

                addedDocumentIds.AddRange(addedProject.DocumentIds);
            }

            foreach (var projectChanges in changes.GetProjectChanges())
            {
                // Ignore projects that don't even have syntax trees to share.
                if (!projectChanges.NewProject.SupportsCompilation)
                    continue;

                // Now do the same for all added and changed documents in a project.
                addedDocumentIds.AddRange(projectChanges.GetAddedDocuments());
                changedDocumentIds.AddRange(projectChanges.GetChangedDocuments());
            }

            var configService = newSolution.Workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
            return configService.Options.OnlyUnifyDocumentsAcrossProjectFlavors
                ? UnifyLinkedDocumentContentsAcrossProjectFlavors(newSolution, addedDocumentIds, changedDocumentIds)
                : UnifyLinkedDocumentContentsAcrossEntireSolution(newSolution, addedDocumentIds, changedDocumentIds);
        }
    }

    private static Solution UnifyLinkedDocumentContentsAcrossProjectFlavors(Solution newSolution, ArrayBuilder<DocumentId> addedDocumentIds, PooledHashSet<DocumentId> changedDocumentIds)
    {
        // Mapping from a project to all its sibling flavored projects.  For example, for a project "Workspaces
        // (netstandard2.0)", this would be "Workspaces (net7.0)", "Workspaces (net8.0)", etc.
        using var _3 = PooledDictionary<ProjectId, ArrayBuilder<ProjectState>>.GetInstance(out var projectToSiblingFlavors);

        newSolution = UpdateAddedDocumentToExistingContentsInSolution(newSolution, addedDocumentIds, projectToSiblingFlavors);

        // now, for any changed document, ensure we go and make all links to it have the same text/tree.
        newSolution = UpdateExistingDocumentsToChangedDocumentContents(newSolution, changedDocumentIds, projectToSiblingFlavors);

        // Free the ArrayBuilders in projectToSiblingFlavors.  The dictionary itself will be automatically freed at the end of this scope.
        projectToSiblingFlavors.FreeValues();

        return newSolution;

        static bool TryGetSiblingFlavoredProjects(
            Solution solution,
            ProjectId projectId,
            Dictionary<ProjectId, ArrayBuilder<ProjectState>> projectIdToSiblingFlavors,
            [NotNullWhen(true)] out ArrayBuilder<ProjectState>? siblingFlavors)
        {
            siblingFlavors = null;

            var projectState = solution.SolutionState.GetRequiredProjectState(projectId);

            // If this project doesn't have any flavors itself, then there's no sibling flavors of it to return.
            if (projectState.NameAndFlavor.flavor is null)
                return false;

            if (!projectIdToSiblingFlavors.TryGetValue(projectId, out siblingFlavors))
            {
                siblingFlavors = ArrayBuilder<ProjectState>.GetInstance();
                projectIdToSiblingFlavors.Add(projectId, siblingFlavors);

                foreach (var (siblingProjectId, siblingProject) in solution.SolutionState.ProjectStates)
                {
                    if (projectId == siblingProjectId)
                        continue;

                    if (siblingProject.NameAndFlavor.name == projectState.NameAndFlavor.name &&
                        siblingProject.NameAndFlavor.flavor != null)
                    {
                        siblingFlavors.Add(siblingProject);
                    }
                }
            }

            return true;
        }

        static Solution UpdateAddedDocumentToExistingContentsInSolution(
            Solution solution,
            ArrayBuilder<DocumentId> addedDocumentIds,
            Dictionary<ProjectId, ArrayBuilder<ProjectState>> projectIdToSiblingFlavors)
        {
            using var _ = ArrayBuilder<(DocumentId, DocumentState)>.GetInstance(out var relatedDocumentIdsAndStates);

            foreach (var group in addedDocumentIds.GroupBy(static d => d.ProjectId))
            {
                var projectId = group.Key;

                if (!TryGetSiblingFlavoredProjects(solution, projectId, projectIdToSiblingFlavors, out var siblingFlavors))
                    continue;

                foreach (var addedDocumentId in group)
                {
                    var addedDocument = solution.SolutionState.GetRequiredDocumentState(addedDocumentId);
                    if (addedDocument.FilePath is null)
                        continue;

                    foreach (var siblingProject in siblingFlavors)
                    {
                        // Should only be searching different projects from the original project.
                        Contract.ThrowIfTrue(siblingProject.Id == projectId);

                        var relatedDocumentId = siblingProject.GetFirstDocumentIdWithFilePath(addedDocument.FilePath);
                        if (relatedDocumentId is null)
                            continue;

                        var relatedDocument = solution.GetRequiredDocument(relatedDocumentId);
                        relatedDocumentIdsAndStates.Add((addedDocumentId, relatedDocument.DocumentState));
                        break;
                    }
                }
            }

            if (relatedDocumentIdsAndStates.IsEmpty)
                return solution;

            return solution.WithDocumentContentsFrom(relatedDocumentIdsAndStates.ToImmutableAndClear(), forceEvenIfTreesWouldDiffer: false);
        }

        static Solution UpdateExistingDocumentsToChangedDocumentContents(
            Solution solution,
            HashSet<DocumentId> changedDocumentIds,
            Dictionary<ProjectId, ArrayBuilder<ProjectState>> projectIdToSiblingFlavors)
        {
            // Changing a document in a linked-doc-chain will end up producing N changed documents.  We only want to
            // process that chain once.
            using var _ = PooledDictionary<DocumentId, DocumentState>.GetInstance(out var relatedDocumentIdsAndStates);

            foreach (var group in changedDocumentIds.GroupBy(static d => d.ProjectId))
            {
                var projectId = group.Key;

                if (!TryGetSiblingFlavoredProjects(solution, projectId, projectIdToSiblingFlavors, out var siblingFlavors))
                    continue;

                foreach (var changedDocumentId in group)
                {
                    var changedDocument = solution.SolutionState.GetRequiredDocumentState(changedDocumentId);
                    if (changedDocument.FilePath is null)
                        continue;

                    foreach (var siblingProject in siblingFlavors)
                    {
                        // Should only be searching different projects from the original project.
                        Contract.ThrowIfTrue(siblingProject.Id == projectId);

                        var relatedDocumentId = siblingProject.GetFirstDocumentIdWithFilePath(changedDocument.FilePath);
                        if (relatedDocumentId is null)
                            continue;

                        if (!changedDocumentIds.Contains(relatedDocumentId))
                            relatedDocumentIdsAndStates[relatedDocumentId] = changedDocument;
                    }
                }
            }

            if (relatedDocumentIdsAndStates.Count == 0)
                return solution;

            var relatedDocumentIdsAndStatesArray = relatedDocumentIdsAndStates.SelectAsArray(static kvp => (kvp.Key, kvp.Value));

            return solution.WithDocumentContentsFrom(relatedDocumentIdsAndStatesArray, forceEvenIfTreesWouldDiffer: false);
        }
    }

    private static Solution UnifyLinkedDocumentContentsAcrossEntireSolution(Solution newSolution, ArrayBuilder<DocumentId> addedDocumentIds, PooledHashSet<DocumentId> changedDocumentIds)
    {
        newSolution = UpdateAddedDocumentToExistingContentsInSolution(newSolution, addedDocumentIds);

        // now, for any changed document, ensure we go and make all links to it have the same text/tree.
        newSolution = UpdateExistingDocumentsToChangedDocumentContents(newSolution, changedDocumentIds);

        return newSolution;

        static Solution UpdateAddedDocumentToExistingContentsInSolution(
            Solution solution, ArrayBuilder<DocumentId> addedDocumentIds)
        {
            ProjectId? relatedProjectIdHint = null;
            using var _ = ArrayBuilder<(DocumentId, DocumentState)>.GetInstance(out var relatedDocumentIdsAndStates);

            foreach (var addedDocumentId in addedDocumentIds)
            {
                // Ensure we don't search in addedDocumentId's project for the related document
                if (addedDocumentId.ProjectId == relatedProjectIdHint)
                    relatedProjectIdHint = null;

                // Look for a related document we can create our contents from.  We only have to look for a single related
                // doc as we'll be done once we update our contents to theirs.  Note: GetFirstRelatedDocumentId will also
                // not search the project that addedDocumentId came from.  So this will help ensure we don't repeatedly add
                // documents to a project, then look for related docs *within that project*, forcing the file-path map in it
                // to be recreated for each document.
                var relatedDocumentId = solution.GetFirstRelatedDocumentId(addedDocumentId, relatedProjectIdHint);

                // Couldn't find a related document.
                if (relatedDocumentId is null)
                    continue;

                var relatedDocument = solution.GetRequiredDocument(relatedDocumentId);

                // Should never return a file as its own related document
                Contract.ThrowIfTrue(relatedDocumentId == addedDocumentId);

                // Related document must come from a distinct project.
                Contract.ThrowIfTrue(relatedDocumentId.ProjectId == addedDocumentId.ProjectId);

                relatedProjectIdHint = relatedDocumentId.ProjectId;
                relatedDocumentIdsAndStates.Add((addedDocumentId, relatedDocument.DocumentState));
            }

            if (relatedDocumentIdsAndStates.IsEmpty)
                return solution;

            return solution.WithDocumentContentsFrom(relatedDocumentIdsAndStates.ToImmutableAndClear(), forceEvenIfTreesWouldDiffer: false);
        }

        static Solution UpdateExistingDocumentsToChangedDocumentContents(Solution solution, HashSet<DocumentId> changedDocumentIds)
        {
            // Changing a document in a linked-doc-chain will end up producing N changed documents.  We only want to
            // process that chain once.
            using var _ = PooledDictionary<DocumentId, DocumentState>.GetInstance(out var relatedDocumentIdsAndStates);

            foreach (var changedDocumentId in changedDocumentIds)
            {
                Document? changedDocument = null;
                var relatedDocumentIds = solution.GetRelatedDocumentIds(changedDocumentId);

                foreach (var relatedDocumentId in relatedDocumentIds)
                {
                    if (relatedDocumentId == changedDocumentId)
                        continue;

                    if (!changedDocumentIds.Contains(relatedDocumentId))
                    {
                        changedDocument ??= solution.GetRequiredDocument(changedDocumentId);
                        relatedDocumentIdsAndStates[relatedDocumentId] = changedDocument.DocumentState;
                    }
                }
            }

            if (relatedDocumentIdsAndStates.Count == 0)
                return solution;

            var relatedDocumentIdsAndStatesArray = relatedDocumentIdsAndStates.SelectAsArray(static kvp => (kvp.Key, kvp.Value));

            return solution.WithDocumentContentsFrom(relatedDocumentIdsAndStatesArray, forceEvenIfTreesWouldDiffer: false);
        }
    }

    /// <summary>
    /// Applies specified transformation to <see cref="CurrentSolution"/>, updates <see cref="CurrentSolution"/> to
    /// the new value and performs a requested callback immediately before and after that update.  The callbacks
    /// will be invoked atomically while while <see cref="_serializationLock"/> is being held.
    /// </summary>
    /// <param name="transformation">Solution transformation. This may be run multiple times.  As such it should be
    /// a purely functional transformation on the solution instance passed to it.  It should not make stateful
    /// changes elsewhere.</param>
    /// <param name="mayRaiseEvents"><see langword="true"/> if this operation may raise observable events;
    /// otherwise, <see langword="false"/>. If <see langword="true"/>, the operation will call
    /// <see cref="EnsureEventListeners"/> to ensure listeners are registered prior to callbacks that may raise
    /// events.</param>
    /// <param name="onBeforeUpdate">Action to perform immediately prior to updating <see cref="CurrentSolution"/>.
    /// The action will be passed the old <see cref="CurrentSolution"/> that will be replaced and the exact solution
    /// it will be replaced with. The latter may be different than the solution returned by <paramref
    /// name="transformation"/> as it will have its <see cref="Solution.WorkspaceVersion"/> updated
    /// accordingly.  This will only be run once.</param>
    /// <param name="onAfterUpdate">Action to perform once <see cref="CurrentSolution"/> has been updated.  The
    /// action will be passed the old <see cref="CurrentSolution"/> that was just replaced and the exact solution it
    /// was replaced with. The latter may be different than the solution returned by <paramref
    /// name="transformation"/> as it will have its <see cref="Solution.WorkspaceVersion"/> updated
    /// accordingly.  This will only be run once.</param>
    private protected (Solution oldSolution, Solution newSolution) SetCurrentSolution<TData>(
        TData data,
        Func<Solution, TData, Solution> transformation,
        bool mayRaiseEvents = true,
        Action<Solution, Solution, TData>? onBeforeUpdate = null,
        Action<Solution, Solution, TData>? onAfterUpdate = null)
    {
#pragma warning disable CA2012 // Use ValueTasks correctly
        var valueTask = SetCurrentSolutionAsync(
            useAsync: false,
            data,
            transformation,
            mayRaiseEvents,
            onBeforeUpdate,
            onAfterUpdate,
            CancellationToken.None);

        return valueTask.VerifyCompleted("Task must have completed synchronously as we passed 'useAsync: false' to SetCurrentSolutionAsync");
#pragma warning restore CA2012 // Use ValueTasks correctly
    }

    /// <inheritdoc cref="SetCurrentSolution{TData}(TData, Func{Solution, TData, Solution}, bool, Action{Solution, Solution, TData}?, Action{Solution, Solution, TData}?)"/>
    private protected async ValueTask<(Solution oldSolution, Solution newSolution)> SetCurrentSolutionAsync<TData>(
        bool useAsync,
        TData data,
        Func<Solution, TData, Solution> transformation,
        bool mayRaiseEvents,
        Action<Solution, Solution, TData>? onBeforeUpdate,
        Action<Solution, Solution, TData>? onAfterUpdate,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(transformation);

        try
        {
            var oldSolution = Volatile.Read(ref _latestSolution);

            if (mayRaiseEvents)
            {
                // Ensure our event handlers are realized prior to taking this lock.  We don't want to deadlock trying
                // to obtain them when calling one of our callbacks. See https://github.com/dotnet/roslyn/issues/64681
                EnsureEventListeners();
            }

            while (true)
            {
                // Run the transformation outside of the lock as it should not be making any state changes to us.
                var newSolution = transformation(oldSolution, data);

                // if it did nothing, then no need to proceed.
                if (oldSolution == newSolution)
                    return (oldSolution, newSolution);

                // Now, take the lock and try to update our internal state.
                using (useAsync ? await _serializationLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false) : _serializationLock.DisposableWait(cancellationToken))
                {
                    if (_latestSolution != oldSolution)
                    {
                        // something else snuck in and wrote to _latestSolution. Restart and try again.
                        oldSolution = _latestSolution;
                        continue;
                    }

                    newSolution = newSolution.WithNewWorkspace(oldSolution.WorkspaceKind, oldSolution.WorkspaceVersion + 1, oldSolution.Services);

                    // Prior to updating the latest solution, let the caller do any other state updates they want.
                    onBeforeUpdate?.Invoke(oldSolution, newSolution, data);

                    _latestSolution = newSolution;

                    // Once we've updated _latestSolution, perform any requested callbacks.
                    onAfterUpdate?.Invoke(oldSolution, newSolution, data);
                    return (oldSolution, newSolution);
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Critical))
        {
            // We'll rethrow the exception to the caller, since this exception could represent a bug in a third-party workspace, and if at this point our workspace
            // is corrupted we want the caller to know.
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Gets or sets the set of all global options and <see cref="Solution.Options"/>.
    /// Setter also force updates the <see cref="CurrentSolution"/> to have the updated <see cref="Solution.Options"/>.
    /// </summary>
    public OptionSet Options
    {
        get
        {
            return this.CurrentSolution.Options;
        }

        [Obsolete(@"Workspace options should be set by invoking 'workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(newOptionSet))'")]
        set
        {
            var changedOptions = value switch
            {
                null => throw new ArgumentNullException(nameof(value)),
                SolutionOptionSet solutionOptionSet => solutionOptionSet.GetChangedOptions(),
                _ => throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_specified_Solution, paramName: nameof(value))
            };

            _legacyOptions.SetOptions(changedOptions.internallyDefined, changedOptions.externallyDefined);
        }
    }

    internal void UpdateCurrentSolutionOnOptionsChanged()
    {
        SetCurrentSolution(
            oldSolution => oldSolution.WithOptions(new SolutionOptionSet(_legacyOptions)),
            WorkspaceChangeKind.SolutionChanged);
    }

    /// <summary>
    /// Executes an action as a background task, as part of a sequential queue of tasks.
    /// </summary>
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    protected internal Task ScheduleTask(Action action, string? taskName = "Workspace.Task")
        => _taskQueue.ScheduleTask(taskName ?? "Workspace.Task", action, CancellationToken.None);

    /// <summary>
    /// Execute a function as a background task, as part of a sequential queue of tasks.
    /// </summary>
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    protected internal Task<T> ScheduleTask<T>(Func<T> func, string? taskName = "Workspace.Task")
        => _taskQueue.ScheduleTask(taskName ?? "Workspace.Task", func, CancellationToken.None);

    /// <summary>
    /// Override this method to act immediately when the text of a document has changed, as opposed
    /// to waiting for the corresponding workspace changed event to fire asynchronously.
    /// </summary>
    protected virtual void OnDocumentTextChanged(Document document)
    {
    }

    /// <summary>
    /// Override this method to act immediately when a document is closing, as opposed
    /// to waiting for the corresponding workspace changed event to fire asynchronously.
    /// </summary>
    protected virtual void OnDocumentClosing(DocumentId documentId)
    {
    }

    /// <summary>
    /// Clears all solution data and empties the current solution.
    /// </summary>
    protected void ClearSolution()
    {
        this.ClearSolution(reportChangeEvent: true);
    }

    /// <param name="reportChangeEvent">Used so that while disposing we can clear the solution without issuing more
    /// events. As we are disposing, we don't want to cause any current listeners to do work on us as we're in the
    /// process of going away.</param>
    private void ClearSolution(bool reportChangeEvent)
    {
        this.SetCurrentSolution(
            data: /*unused*/ 0,
            (oldSolution, _) => this.CreateSolution(oldSolution.Id),
            mayRaiseEvents: reportChangeEvent,
            onBeforeUpdate: (_, _, _) => this.ClearSolutionData(),
            onAfterUpdate: (oldSolution, newSolution, _) =>
            {
                if (reportChangeEvent)
                    this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionCleared, oldSolution, newSolution);
            });
    }

    /// <summary>
    /// This method is called when a solution is cleared.
    /// <para>
    /// Override this method if you want to do additional work when a solution is cleared. Call the base method at
    /// the end of your method.</para>
    /// <para>
    /// This method is called while a lock is held.  Be very careful when overriding as innapropriate work can cause deadlocks.
    /// </para>
    /// </summary>
    protected virtual void ClearSolutionData()
    {
        this.ClearOpenDocuments();
    }

    /// <summary>
    /// This method is called when an individual project is removed.
    ///
    /// Override this method if you want to do additional work when a project is removed.
    /// Call the base method at the end of your method.
    /// </summary>
    protected virtual void ClearProjectData(ProjectId projectId)
        => this.ClearOpenDocuments(projectId);

    /// <summary>
    /// This method is called to clear an individual document is removed.
    ///
    /// Override this method if you want to do additional work when a document is removed.
    /// Call the base method at the end of your method.
    /// </summary>
    protected internal virtual void ClearDocumentData(DocumentId documentId)
        => this.ClearOpenDocument(documentId);

    /// <summary>
    /// Disposes this workspace. The workspace can longer be used after it is disposed.
    /// </summary>
    public void Dispose()
        => this.Dispose(finalize: false);

    /// <summary>
    /// Call this method when the workspace is disposed.
    ///
    /// Override this method to do additional work when the workspace is disposed.
    /// Call this method at the end of your method.
    /// </summary>
    protected virtual void Dispose(bool finalize)
    {
        if (!finalize)
        {
            // Use `reportChangeEvent` as we do not want to issue an event here since we're in the process of
            // tearing ourselves down.
            this.ClearSolution(reportChangeEvent: false);

            this.Services.GetService<IWorkspaceEventListenerService>()?.Stop();
        }

        _legacyOptions.UnregisterWorkspace(this);

        // Directly dispose IRemoteHostClientProvider if necessary. This is a test hook to ensure RemoteWorkspace
        // gets disposed in unit tests as soon as TestWorkspace gets disposed. This would be superseded by direct
        // support for IDisposable in https://github.com/dotnet/roslyn/pull/47951.
        if (Services.GetService<IRemoteHostClientProvider>() is IDisposable disposableService)
        {
            disposableService.Dispose();
        }

        // We're disposing this workspace.  Stop any work to update SG docs in the background.
        _updateSourceGeneratorsQueueTokenSource.Cancel();
    }

    #region Host API

    private static Solution CheckAndAddProjects(Solution solution, IReadOnlyList<ProjectInfo> projects)
    {
        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(projects.Count, out var builder);
        foreach (var project in projects)
        {
            CheckProjectIsNotInSolution(solution, project.Id);
            builder.Add(project);
        }

        return solution.AddProjects(builder);
    }

    private static Solution CheckAndAddProject(Solution newSolution, ProjectInfo project)
    {
        CheckProjectIsNotInSolution(newSolution, project.Id);
        return newSolution.AddProject(project);
    }

    /// <summary>
    /// Call this method to respond to a solution being opened in the host environment.
    /// </summary>
    protected internal void OnSolutionAdded(SolutionInfo solutionInfo)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckSolutionIsEmpty(oldSolution);

                var newSolution = this.CreateSolution(solutionInfo);

                newSolution = CheckAndAddProjects(newSolution, solutionInfo.Projects);

                return newSolution;
            }, WorkspaceChangeKind.SolutionAdded);
    }

    /// <summary>
    /// Call this method to respond to a solution being reloaded in the host environment.
    /// </summary>
    protected internal void OnSolutionReloaded(SolutionInfo reloadedSolutionInfo)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                var newSolution = this.CreateSolution(reloadedSolutionInfo);

                newSolution = CheckAndAddProjects(newSolution, reloadedSolutionInfo.Projects);

                return this.AdjustReloadedSolution(oldSolution, newSolution);
            }, WorkspaceChangeKind.SolutionReloaded);
    }

    /// <summary>
    /// This method is called when the solution is removed from the workspace.
    ///
    /// Override this method if you want to do additional work when the solution is removed.
    /// Call the base method at the end of your method.
    /// Call this method to respond to a solution being removed/cleared/closed in the host environment.
    /// </summary>
    protected internal void OnSolutionRemoved()
    {
        this.SetCurrentSolution(
            _ => this.CreateSolution(SolutionId.CreateNewId()),
            WorkspaceChangeKind.SolutionRemoved,
            onBeforeUpdate: (_, _) => this.ClearSolutionData());
    }

    /// <summary>
    /// Call this method to respond to a project being added/opened in the host environment.
    /// </summary>
    protected internal void OnProjectAdded(ProjectInfo projectInfo)
    {
        this.SetCurrentSolution(
            oldSolution => CheckAndAddProject(oldSolution, projectInfo),
            WorkspaceChangeKind.ProjectAdded, projectId: projectInfo.Id);
    }

    /// <summary>
    /// Call this method to respond to a project being reloaded in the host environment.
    /// </summary>
    protected internal virtual void OnProjectReloaded(ProjectInfo reloadedProjectInfo)
    {
        var projectId = reloadedProjectInfo.Id;
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckProjectIsInSolution(oldSolution, projectId);

                return this.AdjustReloadedProject(
                    oldSolution.GetRequiredProject(projectId),
                    oldSolution.RemoveProject(projectId).AddProject(reloadedProjectInfo).GetRequiredProject(projectId)).Solution;
            }, WorkspaceChangeKind.ProjectReloaded, projectId);
    }

    /// <summary>
    /// Call this method to respond to a project being removed from the host environment.
    /// </summary>
    protected internal virtual void OnProjectRemoved(ProjectId projectId)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckProjectIsInSolution(oldSolution, projectId);
                this.CheckProjectCanBeRemoved(projectId);

                return oldSolution.RemoveProject(projectId);
            },
            WorkspaceChangeKind.ProjectRemoved, projectId,
            onBeforeUpdate: (oldSolution, _) =>
            {
                // Clear out mutable state not associated with the solution snapshot (for example, which documents are
                // currently open).
                this.ClearProjectData(projectId);
            });
    }

    /// <summary>
    /// Currently projects can always be removed, but this method still exists because it's protected and we don't
    /// want to break people who may have derived from <see cref="Workspace"/> and either called it, or overridden it.
    /// </summary>
    protected virtual void CheckProjectCanBeRemoved(ProjectId projectId)
    {
    }

    /// <summary>
    /// Call this method when a project's assembly name is changed in the host environment.
    /// </summary>
    protected internal void OnAssemblyNameChanged(ProjectId projectId, string assemblyName)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectAssemblyName(projectId, assemblyName), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's output file path is changed in the host environment.
    /// </summary>
    protected internal void OnOutputFilePathChanged(ProjectId projectId, string? outputFilePath)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectOutputFilePath(projectId, outputFilePath), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's output ref file path is changed in the host environment.
    /// </summary>
    protected internal void OnOutputRefFilePathChanged(ProjectId projectId, string? outputFilePath)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectOutputRefFilePath(projectId, outputFilePath), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's name is changed in the host environment.
    /// </summary>
    // TODO (https://github.com/dotnet/roslyn/issues/37124): decide if we want to allow "name" to be nullable.
    // As of this writing you can pass null, but rather than updating the project to null it seems it does nothing.
    // I'm leaving this marked as "non-null" so as not to say we actually support that behavior. The underlying
    // requirement is ProjectInfo.ProjectAttributes holds a non-null name, so you can't get a null into this even if you tried.
    protected internal void OnProjectNameChanged(ProjectId projectId, string name, string? filePath)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectName(projectId, name).WithProjectFilePath(projectId, filePath), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's default namespace is changed in the host environment.
    /// </summary>
    internal void OnDefaultNamespaceChanged(ProjectId projectId, string? defaultNamespace)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectDefaultNamespace(projectId, defaultNamespace), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's compilation options are changed in the host environment.
    /// </summary>
    protected internal void OnCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectCompilationOptions(projectId, options), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's parse options are changed in the host environment.
    /// </summary>
    protected internal void OnParseOptionsChanged(ProjectId projectId, ParseOptions options)
        => SetCurrentSolution(oldSolution => oldSolution.WithProjectParseOptions(projectId, options), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project reference is added to a project in the host environment.
    /// </summary>
    protected internal void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectIsInCurrentSolution(projectReference.ProjectId);
            CheckProjectDoesNotHaveProjectReference(projectId, projectReference);

            // Can only add this P2P reference if it would not cause a circularity.
            CheckProjectDoesNotHaveTransitiveProjectReference(projectId, projectReference.ProjectId);

            return oldSolution.AddProjectReference(projectId, projectReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when a project reference is removed from a project in the host environment.
    /// </summary>
    protected internal void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectIsInCurrentSolution(projectReference.ProjectId);
            CheckProjectHasProjectReference(projectId, projectReference);

            return oldSolution.RemoveProjectReference(projectId, projectReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when a metadata reference is added to a project in the host environment.
    /// </summary>
    protected internal void OnMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectDoesNotHaveMetadataReference(projectId, metadataReference);
            return oldSolution.AddMetadataReference(projectId, metadataReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when a metadata reference is removed from a project in the host environment.
    /// </summary>
    protected internal void OnMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectHasMetadataReference(projectId, metadataReference);
            return oldSolution.RemoveMetadataReference(projectId, metadataReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when an analyzer reference is added to a project in the host environment.
    /// </summary>
    protected internal void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectDoesNotHaveAnalyzerReference(projectId, analyzerReference);
            return oldSolution.AddAnalyzerReference(projectId, analyzerReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when an analyzer reference is removed from a project in the host environment.
    /// </summary>
    protected internal void OnAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckProjectHasAnalyzerReference(projectId, analyzerReference);
            return oldSolution.RemoveAnalyzerReference(projectId, analyzerReference);
        }, WorkspaceChangeKind.ProjectChanged, projectId);
    }

    /// <summary>
    /// Call this method when an analyzer reference is added to a project in the host environment.
    /// </summary>
    internal void OnSolutionAnalyzerReferenceAdded(AnalyzerReference analyzerReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckSolutionDoesNotHaveAnalyzerReference(oldSolution, analyzerReference);
            return oldSolution.AddAnalyzerReference(analyzerReference);
        }, WorkspaceChangeKind.SolutionChanged);
    }

    /// <summary>
    /// Call this method when an analyzer reference is removed from a project in the host environment.
    /// </summary>
    internal void OnSolutionAnalyzerReferenceRemoved(AnalyzerReference analyzerReference)
    {
        SetCurrentSolution(oldSolution =>
        {
            CheckSolutionHasAnalyzerReference(oldSolution, analyzerReference);
            return oldSolution.RemoveAnalyzerReference(analyzerReference);
        }, WorkspaceChangeKind.SolutionChanged);
    }

    /// <summary>
    /// Call this method when status of project has changed to incomplete.
    /// See <see cref="ProjectInfo.HasAllInformation"/> for more information.
    /// </summary>
    // TODO: make it public
    internal void OnHasAllInformationChanged(ProjectId projectId, bool hasAllInformation)
        => SetCurrentSolution(oldSolution => oldSolution.WithHasAllInformation(projectId, hasAllInformation), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a project's RunAnalyzers property is changed in the host environment.
    /// </summary>
    internal void OnRunAnalyzersChanged(ProjectId projectId, bool runAnalyzers)
        => SetCurrentSolution(oldSolution => oldSolution.WithRunAnalyzers(projectId, runAnalyzers), WorkspaceChangeKind.ProjectChanged, projectId);

    /// <summary>
    /// Call this method when a document is added to a project in the host environment.
    /// </summary>
    protected internal void OnDocumentAdded(DocumentInfo documentInfo)
    {
        this.SetCurrentSolution(
            oldSolution => oldSolution.AddDocument(documentInfo),
            WorkspaceChangeKind.DocumentAdded, documentId: documentInfo.Id);
    }

    /// <summary>
    /// Call this method when multiple document are added to one or more projects in the host environment.
    /// </summary>
    protected internal void OnDocumentsAdded(ImmutableArray<DocumentInfo> documentInfos)
    {
        this.SetCurrentSolution(
            data: (@this: this, documentInfos),
            static (oldSolution, data) => oldSolution.AddDocuments(data.documentInfos),
            onAfterUpdate: static (oldSolution, newSolution, data) =>
            {
                // Raise ProjectChanged as the event type here. DocumentAdded is presumed by many callers to have a
                // DocumentId associated with it, and we don't want to be raising multiple events.
                foreach (var projectId in data.documentInfos.Select(i => i.Id.ProjectId).Distinct())
                    data.@this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
            });
    }

    /// <summary>
    /// Call this method when a document is reloaded in the host environment.
    /// </summary>
    protected internal void OnDocumentReloaded(DocumentInfo newDocumentInfo)
    {
        var documentId = newDocumentInfo.Id;
        this.SetCurrentSolution(
            oldSolution => oldSolution.RemoveDocument(documentId).AddDocument(newDocumentInfo),
            WorkspaceChangeKind.DocumentReloaded, documentId: documentId);
    }

    /// <summary>
    /// Call this method when a document is removed from a project in the host environment.
    /// </summary>
    protected internal void OnDocumentRemoved(DocumentId documentId)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckDocumentIsInSolution(oldSolution, documentId);
                this.CheckDocumentCanBeRemoved(documentId);

                return oldSolution.RemoveDocument(documentId);
            },
            WorkspaceChangeKind.DocumentRemoved, documentId: documentId,
            onBeforeUpdate: (oldSolution, _) =>
            {
                // Clear out mutable state not associated with teh solution snapshot (for example, which documents are
                // currently open).
                this.ClearDocumentData(documentId);
            });
    }

    protected virtual void CheckDocumentCanBeRemoved(DocumentId documentId)
    {
    }

    /// <summary>
    /// Call this method when the document info changes, such as the name, folders or file path.
    /// </summary>
    protected internal void OnDocumentInfoChanged(DocumentId documentId, DocumentInfo newInfo)
    {
        SetCurrentSolution(
            oldSolution =>
            {
                CheckDocumentIsInSolution(oldSolution, documentId);

                var newSolution = oldSolution;
                var oldAttributes = oldSolution.GetDocument(documentId)!.State.Attributes;

                if (oldAttributes.Name != newInfo.Name)
                {
                    newSolution = newSolution.WithDocumentName(documentId, newInfo.Name);
                }

                if (oldAttributes.Folders != newInfo.Folders)
                {
                    newSolution = newSolution.WithDocumentFolders(documentId, newInfo.Folders);
                }

                if (oldAttributes.FilePath != newInfo.FilePath)
                {
                    // TODO (https://github.com/dotnet/roslyn/issues/37125): Solution.WithDocumentFilePath will throw if
                    // filePath is null, but it's odd because we *do* support null file paths. The suppression here is to silence it
                    // but should be removed when the bug is fixed.
                    newSolution = newSolution.WithDocumentFilePath(documentId, newInfo.FilePath!);
                }

                if (oldAttributes.SourceCodeKind != newInfo.SourceCodeKind)
                {
                    newSolution = newSolution.WithDocumentSourceCodeKind(documentId, newInfo.SourceCodeKind);
                }

                return newSolution;
            },
            WorkspaceChangeKind.DocumentInfoChanged, documentId: documentId);
    }

    /// <summary>
    /// Call this method when the text of a document is updated in the host environment.
    /// </summary>
    protected internal void OnDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
        => OnDocumentTextChanged(documentId, newText, mode, requireDocumentPresent: true);

    private protected void OnDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode, bool requireDocumentPresent)
    {
        OnAnyDocumentTextChanged(
            documentId,
            (newText, mode),
            static (solution, docId) => solution.GetDocument(docId),
            (solution, docId, newTextAndMode) => solution.WithDocumentText(docId, newTextAndMode.newText, newTextAndMode.mode),
            WorkspaceChangeKind.DocumentChanged,
            isCodeDocument: true,
            requireDocumentPresent);
    }

    /// <summary>
    /// Call this method when the text of an additional document is updated in the host environment.
    /// </summary>
    protected internal void OnAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
    {
        OnAnyDocumentTextChanged(
            documentId,
            (newText, mode),
            static (solution, docId) => solution.GetAdditionalDocument(docId),
            (solution, docId, newTextAndMode) => solution.WithAdditionalDocumentText(docId, newTextAndMode.newText, newTextAndMode.mode),
            WorkspaceChangeKind.AdditionalDocumentChanged,
            isCodeDocument: false,
            requireDocumentPresent: true);
    }

    /// <summary>
    /// Call this method when the text of an analyzer config document is updated in the host environment.
    /// </summary>
    protected internal void OnAnalyzerConfigDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
    {
        OnAnyDocumentTextChanged(
            documentId,
            (newText, mode),
            static (solution, docId) => solution.GetAnalyzerConfigDocument(docId),
            (solution, docId, newTextAndMode) => solution.WithAnalyzerConfigDocumentText(docId, newTextAndMode.newText, newTextAndMode.mode),
            WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
            isCodeDocument: false,
            requireDocumentPresent: true);
    }

    /// <summary>
    /// Call this method when the text of a document is changed on disk.
    /// </summary>
    protected internal void OnDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
        => OnDocumentTextLoaderChanged(documentId, loader, requireDocumentPresent: true);

    /// <summary>
    /// Call this method when the text of a document is changed on disk.
    /// </summary>
    private protected void OnDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader, bool requireDocumentPresent)
    {
        OnAnyDocumentTextChanged(
            documentId,
            loader,
            static (solution, docId) => solution.GetDocument(docId),
            (solution, docId, loader) => solution.WithDocumentTextLoader(docId, loader, PreservationMode.PreserveValue),
            WorkspaceChangeKind.DocumentChanged,
            isCodeDocument: true,
            requireDocumentPresent);
    }

    /// <summary>
    /// Call this method when the text of a additional document is changed on disk.
    /// </summary>
    protected internal void OnAdditionalDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
    {
        OnAnyDocumentTextChanged(
            documentId,
            loader,
            static (solution, docId) => solution.GetAdditionalDocument(docId),
            (solution, docId, loader) => solution.WithAdditionalDocumentTextLoader(docId, loader, PreservationMode.PreserveValue),
            WorkspaceChangeKind.AdditionalDocumentChanged,
            isCodeDocument: false,
            requireDocumentPresent: true);
    }

    /// <summary>
    /// Call this method when the text of a analyzer config document is changed on disk.
    /// </summary>
    protected internal void OnAnalyzerConfigDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
    {
        OnAnyDocumentTextChanged(
            documentId,
            loader,
            static (solution, docId) => solution.GetAnalyzerConfigDocument(docId),
            (solution, docId, loader) => solution.WithAnalyzerConfigDocumentTextLoader(docId, loader, PreservationMode.PreserveValue),
            WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
            isCodeDocument: false,
            requireDocumentPresent: true);
    }

    /// <summary>
    /// When a <see cref="Document"/>s text is changed, we need to make sure all of the linked files also have their
    /// content updated in the new solution before applying it to the workspace to avoid the workspace having
    /// solutions with linked files where the contents do not match.
    /// </summary>
    /// <param name="requireDocumentPresent">Allow caller to indicate behavior that should happen if this is a
    /// request to update a document not currently in the workspace.  This should be used only in hosts where there
    /// may be disparate sources of text change info, without an underlying agreed upon synchronization context to
    /// ensure consistency between events.  For example, in an LSP server it might be the case that some events were
    /// being posted by an attached lsp client, while another source of events reported information produced by a
    /// self-hosted project system.  These systems might report events on entirely different cadences, leading to
    /// scenarios where there might be disagreements as to the state of the workspace.  Clients in those cases must
    /// be resilient to those disagreements (for example, by falling back to a misc-workspace if the lsp client
    /// referred to a document no longer in the workspace populated by the project system).</param>
    private void OnAnyDocumentTextChanged<TArg>(
        DocumentId documentId,
        TArg arg,
        Func<Solution, DocumentId, TextDocument?> getDocumentInSolution,
        Func<Solution, DocumentId, TArg, Solution> updateSolutionWithText,
        WorkspaceChangeKind changeKind,
        bool isCodeDocument,
        bool requireDocumentPresent)
    {
        // Data that is updated in the transformation, and read in in onAfterUpdate.  Because SetCurrentSolution may
        // loop, we have to make sure to always clear this each time we enter the loop.
        var updatedDocumentIds = new List<DocumentId>();
        SetCurrentSolution(
            data: (@this: this, documentId, arg, getDocumentInSolution, updateSolutionWithText, changeKind, isCodeDocument, requireDocumentPresent, updatedDocumentIds),
            static (oldSolution, data) =>
            {
                // Ensure this closure data is always clean if we had to restart the the operation.
                var updatedDocumentIds = data.updatedDocumentIds;
                updatedDocumentIds.Clear();

                var @this = data.@this;
                var documentId = data.documentId;

                var document = data.getDocumentInSolution(oldSolution, documentId);
                if (document is null)
                {
                    if (data.requireDocumentPresent)
                    {
                        throw new ArgumentException(string.Format(
                            WorkspacesResources._0_is_not_part_of_the_workspace,
                            data.@this.GetDocumentName(documentId)));
                    }
                    else
                    {
                        return oldSolution;
                    }
                }

                // First, just update the text for the document passed in.
                var newSolution = oldSolution;
                var previousSolution = newSolution;
                newSolution = data.updateSolutionWithText(newSolution, documentId, data.arg);

                if (previousSolution != newSolution)
                {
                    updatedDocumentIds.Add(documentId);

                    // Now go update the linked docs to have the same doc contents.
                    var linkedDocumentIds = oldSolution.GetRelatedDocumentIds(documentId);
                    if (linkedDocumentIds.Length > 0)
                    {
                        // Have the linked documents point *into* the same instance data that the initial document
                        // points at.  This way things like tree data can be shared across docs.

                        var newDocument = newSolution.GetRequiredDocument(documentId);
                        foreach (var linkedDocumentId in linkedDocumentIds)
                        {
                            previousSolution = newSolution;
                            newSolution = newSolution.WithDocumentContentsFrom(linkedDocumentId, newDocument.DocumentState, forceEvenIfTreesWouldDiffer: false);

                            if (previousSolution != newSolution)
                                updatedDocumentIds.Add(linkedDocumentId);
                        }
                    }
                }

                return newSolution;
            },
            onAfterUpdate: static (oldSolution, newSolution, data) =>
            {
                if (data.isCodeDocument)
                {
                    foreach (var updatedDocumentId in data.updatedDocumentIds)
                    {
                        var newDocument = newSolution.GetDocument(updatedDocumentId);
                        Contract.ThrowIfNull(newDocument);
                        data.@this.OnDocumentTextChanged(newDocument);
                    }
                }

                foreach (var updatedDocumentInfo in data.updatedDocumentIds)
                {
                    data.@this.RaiseWorkspaceChangedEventAsync(
                        data.changeKind,
                        oldSolution,
                        newSolution,
                        documentId: updatedDocumentInfo);
                }
            });
    }

    /// <summary>
    /// Call this method when the SourceCodeKind of a document changes in the host environment.
    /// </summary>
    protected internal void OnDocumentSourceCodeKindChanged(DocumentId documentId, SourceCodeKind sourceCodeKind)
    {
        SetCurrentSolution(
            oldSolution =>
            {
                CheckDocumentIsInSolution(oldSolution, documentId);
                return oldSolution.WithDocumentSourceCodeKind(documentId, sourceCodeKind);
            },
            WorkspaceChangeKind.DocumentChanged, documentId: documentId,
            onAfterUpdate: (_, newSolution) => this.OnDocumentTextChanged(newSolution.GetRequiredDocument(documentId)));
    }

    /// <summary>
    /// Call this method when an additional document is added to a project in the host environment.
    /// </summary>
    protected internal void OnAdditionalDocumentAdded(DocumentInfo documentInfo)
    {
        var documentId = documentInfo.Id;
        SetCurrentSolution(
            oldSolution =>
            {
                CheckProjectIsInSolution(oldSolution, documentId.ProjectId);
                CheckAdditionalDocumentIsNotInSolution(oldSolution, documentId);
                return oldSolution.AddAdditionalDocument(documentInfo);
            },
            WorkspaceChangeKind.AdditionalDocumentAdded, documentId: documentId);
    }

    /// <summary>
    /// Call this method when an additional document is removed from a project in the host environment.
    /// </summary>
    protected internal void OnAdditionalDocumentRemoved(DocumentId documentId)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckAdditionalDocumentIsInSolution(oldSolution, documentId);
                this.CheckDocumentCanBeRemoved(documentId);

                return oldSolution.RemoveAdditionalDocument(documentId);
            },
            WorkspaceChangeKind.AdditionalDocumentRemoved, documentId: documentId,
            onBeforeUpdate: (oldSolution, _) =>
            {
                // Clear out mutable state not associated with the solution snapshot (for example, which documents are
                // currently open).
                this.ClearDocumentData(documentId);
            });
    }

    /// <summary>
    /// Call this method when an analyzer config document is added to a project in the host environment.
    /// </summary>
    protected internal void OnAnalyzerConfigDocumentAdded(DocumentInfo documentInfo)
    {
        var documentId = documentInfo.Id;
        SetCurrentSolution(
            oldSolution =>
        {
            CheckProjectIsInSolution(oldSolution, documentId.ProjectId);
            CheckAnalyzerConfigDocumentIsNotInSolution(oldSolution, documentId);

            return oldSolution.AddAnalyzerConfigDocuments([documentInfo]);
        },
        WorkspaceChangeKind.AnalyzerConfigDocumentAdded, documentId: documentId);
    }

    /// <summary>
    /// Call this method when an analyzer config document is removed from a project in the host environment.
    /// </summary>
    protected internal void OnAnalyzerConfigDocumentRemoved(DocumentId documentId)
    {
        this.SetCurrentSolution(
            oldSolution =>
            {
                CheckAnalyzerConfigDocumentIsInSolution(oldSolution, documentId);

                return oldSolution.RemoveAnalyzerConfigDocument(documentId);
            },
            WorkspaceChangeKind.AnalyzerConfigDocumentRemoved, documentId: documentId,
            onBeforeUpdate: (oldSolution, _) =>
            {
                // Clear out mutable state not associated with teh solution snapshot (for example, which documents are
                // currently open).
                this.ClearDocumentData(documentId);
            });
    }

    /// <summary>
    /// Updates all projects to properly reference other projects as project references instead of metadata references.
    /// </summary>
    protected void UpdateReferencesAfterAdd()
    {
        SetCurrentSolution(
            oldSolution => UpdateReferencesAfterAdd(oldSolution),
            WorkspaceChangeKind.SolutionChanged);

        [System.Diagnostics.Contracts.Pure]
        static Solution UpdateReferencesAfterAdd(Solution solution)
        {
            // Build map from output assembly path to ProjectId
            // Use explicit loop instead of ToDictionary so we don't throw if multiple projects have same output assembly path.
            var outputAssemblyToProjectIdMap = new Dictionary<string, ProjectId>();
            foreach (var p in solution.Projects)
            {
                if (!string.IsNullOrEmpty(p.OutputFilePath))
                {
                    outputAssemblyToProjectIdMap[p.OutputFilePath!] = p.Id;
                }

                if (!string.IsNullOrEmpty(p.OutputRefFilePath))
                {
                    outputAssemblyToProjectIdMap[p.OutputRefFilePath!] = p.Id;
                }
            }

            // now fix each project if necessary
            foreach (var pid in solution.ProjectIds)
            {
                var project = solution.GetProject(pid)!;

                // convert metadata references to project references if the metadata reference matches some project's output assembly.
                foreach (var meta in project.MetadataReferences)
                {
                    if (meta is PortableExecutableReference pemeta)
                    {
                        // check both Display and FilePath. FilePath points to the actually bits, but Display should match output path if
                        // the metadata reference is shadow copied.
                        if ((!RoslynString.IsNullOrEmpty(pemeta.Display) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.Display, out var matchingProjectId)) ||
                            (!RoslynString.IsNullOrEmpty(pemeta.FilePath) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.FilePath, out matchingProjectId)))
                        {
                            var newProjRef = new ProjectReference(matchingProjectId, pemeta.Properties.Aliases, pemeta.Properties.EmbedInteropTypes);

                            if (!project.ProjectReferences.Contains(newProjRef))
                            {
                                project = project.AddProjectReference(newProjRef);
                            }

                            project = project.RemoveMetadataReference(meta);
                        }
                    }
                }

                solution = project.Solution;
            }

            return solution;
        }
    }

    #endregion

    #region Apply Changes

    /// <summary>
    /// Determines if the specific kind of change is supported by the <see cref="TryApplyChanges(Solution)"/> method.
    /// </summary>
    public virtual bool CanApplyChange(ApplyChangesKind feature)
        => false;

    /// <summary>
    /// Returns <see langword="true"/> if a reference to referencedProject can be added to
    /// referencingProject.  <see langword="false"/> otherwise.
    /// </summary>
    internal virtual bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        => false;

    /// <summary>
    /// Apply changes made to a solution back to the workspace.
    ///
    /// The specified solution must be one that originated from this workspace. If it is not, or the workspace
    /// has been updated since the solution was obtained from the workspace, then this method returns false. This method
    /// will still throw if the solution contains changes that are not supported according to the <see cref="CanApplyChange(ApplyChangesKind)"/>
    /// method.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the solution contains changes not supported according to the
    /// <see cref="CanApplyChange(ApplyChangesKind)"/> method.</exception>
    public virtual bool TryApplyChanges(Solution newSolution)
        => TryApplyChanges(newSolution, CodeAnalysisProgress.None);

    internal virtual bool TryApplyChanges(Solution newSolution, IProgress<CodeAnalysisProgress> progressTracker)
    {
        using (Logger.LogBlock(FunctionId.Workspace_ApplyChanges, CancellationToken.None))
        {
            // If solution did not originate from this workspace then fail
            if (newSolution.Workspace != this)
            {
                Logger.Log(FunctionId.Workspace_ApplyChanges, "Apply Failed: workspaces do not match");
                return false;
            }

            var oldSolution = this.CurrentSolution;

            // If the workspace has already accepted an update, then fail
            if (newSolution.WorkspaceVersion != oldSolution.WorkspaceVersion)
            {
                Logger.Log(
                    FunctionId.Workspace_ApplyChanges,
                    static (oldSolution, newSolution) =>
                    {
                        // 'oldSolution' is the current workspace solution; if we reach this point we know
                        // 'oldSolution' is newer than the expected workspace solution 'newSolution'.
                        var oldWorkspaceVersion = oldSolution.WorkspaceVersion;
                        var newWorkspaceVersion = newSolution.WorkspaceVersion;
                        return $"Apply Failed: Workspace has already been updated (from version '{newWorkspaceVersion}' to '{oldWorkspaceVersion}')";
                    },
                    oldSolution,
                    newSolution);
                return false;
            }

            var solutionChanges = newSolution.GetChanges(oldSolution);
            this.CheckAllowedSolutionChanges(solutionChanges);

            var solutionWithLinkedFileChangesMerged = newSolution.WithMergedLinkedFileChangesAsync(oldSolution, solutionChanges, cancellationToken: CancellationToken.None).Result;
            solutionChanges = solutionWithLinkedFileChangesMerged.GetChanges(oldSolution);

            // added projects
            foreach (var proj in solutionChanges.GetAddedProjects())
            {
                this.ApplyProjectAdded(CreateProjectInfo(proj));
            }

            // changed projects
            var projectChangesList = solutionChanges.GetProjectChanges().ToImmutableArray();
            progressTracker.AddItems(projectChangesList.Length);

            foreach (var projectChanges in projectChangesList)
            {
                progressTracker.Report(CodeAnalysisProgress.Description(string.Format(WorkspacesResources.Applying_changes_to_0, projectChanges.NewProject.Name)));
                this.ApplyProjectChanges(projectChanges);
                progressTracker.ItemCompleted();
            }

            this.ApplyDocumentsInfoChange(projectChangesList);

            // changes in mapped files outside the workspace (may span multiple projects)
            this.ApplyMappedFileChanges(solutionChanges);

            // removed projects
            foreach (var proj in solutionChanges.GetRemovedProjects())
            {
                this.ApplyProjectRemoved(proj.Id);
            }

            if (this.CurrentSolution.Options != newSolution.Options)
            {
                var changedOptions = newSolution.SolutionState.Options.GetChangedOptions();
                _legacyOptions.SetOptions(changedOptions.internallyDefined, changedOptions.externallyDefined);
            }

            if (!CurrentSolution.AnalyzerReferences.SequenceEqual(newSolution.AnalyzerReferences))
            {
                foreach (var analyzerReference in solutionChanges.GetRemovedAnalyzerReferences())
                {
                    ApplySolutionAnalyzerReferenceRemoved(analyzerReference);
                }

                foreach (var analyzerReference in solutionChanges.GetAddedAnalyzerReferences())
                {
                    ApplySolutionAnalyzerReferenceAdded(analyzerReference);
                }
            }

            return true;
        }
    }

    private void ApplyDocumentsInfoChange(ImmutableArray<ProjectChanges> projectChanges)
    {
        using var _1 = PooledHashSet<DocumentId>.GetInstance(out var infoChangedDocumentIds);
        using var _2 = PooledHashSet<Document>.GetInstance(out var infoChangedNewDocuments);
        foreach (var projectChange in projectChanges)
        {
            foreach (var docId in projectChange.GetChangedDocuments())
            {
                if (!infoChangedDocumentIds.Contains(docId))
                {
                    var oldDoc = projectChange.OldProject.GetRequiredDocument(docId);
                    var newDoc = projectChange.NewProject.GetRequiredDocument(docId);
                    // For linked documents, when info get changed (e.g. name/folder/filePath)
                    // only apply one document changed because it will update the 'real' file, causing the other linked documents get changed.
                    if (oldDoc.HasInfoChanged(newDoc))
                    {
                        var linkedDocuments = oldDoc.GetLinkedDocumentIds();
                        infoChangedDocumentIds.Add(docId);
                        infoChangedDocumentIds.AddRange(linkedDocuments);
                        infoChangedNewDocuments.Add(newDoc);
                    }
                }
            }
        }

        foreach (var newDoc in infoChangedNewDocuments)
        {
            // ApplyDocumentInfoChanged ignores the loader information, so we can pass null for it
            ApplyDocumentInfoChanged(newDoc.Id,
                new DocumentInfo(newDoc.DocumentState.Attributes, loader: null, documentServiceProvider: newDoc.State.Services));
        }
    }

    internal virtual void ApplyMappedFileChanges(SolutionChanges solutionChanges)
    {
        return;
    }

    private void CheckAllowedSolutionChanges(SolutionChanges solutionChanges)
    {
        // Note: For each kind of change first check if the change is disallowed and only if it is determine whether the change is actually made.
        // This is more efficient since most workspaces allow most changes and CanApplyChange is implementation is usually trivial.

        if (!CanApplyChange(ApplyChangesKind.RemoveProject) && solutionChanges.GetRemovedProjects().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_projects_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddProject) && solutionChanges.GetAddedProjects().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_projects_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddSolutionAnalyzerReference) && solutionChanges.GetAddedAnalyzerReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_analyzer_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveSolutionAnalyzerReference) && solutionChanges.GetRemovedAnalyzerReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_analyzer_references_is_not_supported);
        }

        foreach (var projectChanges in solutionChanges.GetProjectChanges())
        {
            CheckAllowedProjectChanges(projectChanges);
        }
    }

    private void CheckAllowedProjectChanges(ProjectChanges projectChanges)
    {
        // If CanApplyChange is true for ApplyChangesKind.ChangeCompilationOptions we allow any change to the compilaton options.
        // If only subset of changes is allowed CanApplyChange shall return false and CanApplyCompilationOptionChange
        // determines the outcome for the particular option change.
        if (!CanApplyChange(ApplyChangesKind.ChangeCompilationOptions) &&
            projectChanges.OldProject.CompilationOptions != projectChanges.NewProject.CompilationOptions)
        {
            // It's OK to assert this: if they were both null, the if check above would have been false right away
            // since they didn't change. Thus, at least one is non-null, and once you have a non-null CompilationOptions
            // and ParseOptions, we don't let you ever make it null again. Further, it can't ever start non-null:
            // we replace a null when a project is created with default compilation options.
            Contract.ThrowIfNull(projectChanges.OldProject.CompilationOptions);
            Contract.ThrowIfNull(projectChanges.NewProject.CompilationOptions);

            // The changes in CompilationOptions may include a change to the SyntaxTreeOptionsProvider, which would be happening
            // if an .editorconfig was added, removed, or modified. We'll compute the options without that change, and if there's
            // still changes then we need to verify we can apply those. The .editorconfig changes will also be represented as
            // document edits, which the host is expected to actually apply directly.
            var newOptionsWithoutSyntaxTreeOptionsChange =
                projectChanges.NewProject.CompilationOptions.WithSyntaxTreeOptionsProvider(
                    projectChanges.OldProject.CompilationOptions.SyntaxTreeOptionsProvider);

            if (projectChanges.OldProject.CompilationOptions != newOptionsWithoutSyntaxTreeOptionsChange)
            {
                // We're actually changing in a meaningful way, so now validate that the workspace can take it.
                // We will pass into the CanApplyCompilationOptionChange newOptionsWithoutSyntaxTreeOptionsChange,
                // which means it's only having to validate that the changes it's expected to apply are changing.
                // The common pattern is to reject all changes not recognized, so this keeps existing code running just fine.
                if (!CanApplyCompilationOptionChange(projectChanges.OldProject.CompilationOptions, newOptionsWithoutSyntaxTreeOptionsChange, projectChanges.NewProject))
                {
                    throw new NotSupportedException(WorkspacesResources.Changing_compilation_options_is_not_supported);
                }
            }
        }

        if (!CanApplyChange(ApplyChangesKind.ChangeParseOptions) &&
            projectChanges.OldProject.ParseOptions != projectChanges.NewProject.ParseOptions &&
            !CanApplyParseOptionChange(projectChanges.OldProject.ParseOptions!, projectChanges.NewProject.ParseOptions!, projectChanges.NewProject))
        {
            throw new NotSupportedException(WorkspacesResources.Changing_parse_options_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddDocument) && projectChanges.GetAddedDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveDocument) && projectChanges.GetRemovedDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.ChangeDocumentInfo)
            && projectChanges.GetChangedDocuments().Any(id => projectChanges.NewProject.GetDocument(id)!.HasInfoChanged(projectChanges.OldProject.GetDocument(id)!)))
        {
            throw new NotSupportedException(WorkspacesResources.Changing_document_property_is_not_supported);
        }

        var changedDocumentIds = projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true, IgnoreUnchangeableDocumentsWhenApplyingChanges).ToImmutableArray();

        if (!CanApplyChange(ApplyChangesKind.ChangeDocument) && changedDocumentIds.Length > 0)
        {
            throw new NotSupportedException(WorkspacesResources.Changing_documents_is_not_supported);
        }

        // Checking for unchangeable documents will only be done if we were asked not to ignore them.
        foreach (var documentId in changedDocumentIds)
        {
            var document = projectChanges.OldProject.State.DocumentStates.GetState(documentId) ??
                           projectChanges.NewProject.State.DocumentStates.GetState(documentId)!;

            if (!document.CanApplyChange())
            {
                throw new NotSupportedException(string.Format(WorkspacesResources.Changing_document_0_is_not_supported, document.FilePath ?? document.Name));
            }
        }

        if (!CanApplyChange(ApplyChangesKind.AddAdditionalDocument) && projectChanges.GetAddedAdditionalDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_additional_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveAdditionalDocument) && projectChanges.GetRemovedAdditionalDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_additional_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.ChangeAdditionalDocument) && projectChanges.GetChangedAdditionalDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Changing_additional_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddAnalyzerConfigDocument) && projectChanges.GetAddedAnalyzerConfigDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_analyzer_config_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveAnalyzerConfigDocument) && projectChanges.GetRemovedAnalyzerConfigDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_analyzer_config_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.ChangeAnalyzerConfigDocument) && projectChanges.GetChangedAnalyzerConfigDocuments().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Changing_analyzer_config_documents_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddProjectReference) && projectChanges.GetAddedProjectReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_project_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveProjectReference) && projectChanges.GetRemovedProjectReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_project_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddMetadataReference) && projectChanges.GetAddedMetadataReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_project_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveMetadataReference) && projectChanges.GetRemovedMetadataReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_project_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.AddAnalyzerReference) && projectChanges.GetAddedAnalyzerReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Adding_analyzer_references_is_not_supported);
        }

        if (!CanApplyChange(ApplyChangesKind.RemoveAnalyzerReference) && projectChanges.GetRemovedAnalyzerReferences().Any())
        {
            throw new NotSupportedException(WorkspacesResources.Removing_analyzer_references_is_not_supported);
        }
    }

    /// <summary>
    /// Called during a call to <see cref="TryApplyChanges(Solution)"/> to determine if a specific change to <see cref="Project.CompilationOptions"/> is allowed.
    /// </summary>
    /// <remarks>
    /// This method is only called if <see cref="CanApplyChange" /> returns false for <see cref="ApplyChangesKind.ChangeCompilationOptions"/>.
    /// If <see cref="CanApplyChange" /> returns true, then that means all changes are allowed and this method does not need to be called.
    /// </remarks>
    /// <param name="oldOptions">The old <see cref="CompilationOptions"/> of the project from prior to the change.</param>
    /// <param name="newOptions">The new <see cref="CompilationOptions"/> of the project that was passed to <see cref="TryApplyChanges(Solution)"/>.</param>
    /// <param name="project">The project contained in the <see cref="Solution"/> passed to <see cref="TryApplyChanges(Solution)"/>.</param>
    public virtual bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project)
        => false;

    /// <summary>
    /// Called during a call to <see cref="TryApplyChanges(Solution)"/> to determine if a specific change to <see cref="Project.ParseOptions"/> is allowed.
    /// </summary>
    /// <remarks>
    /// This method is only called if <see cref="CanApplyChange" /> returns false for <see cref="ApplyChangesKind.ChangeParseOptions"/>.
    /// If <see cref="CanApplyChange" /> returns true, then that means all changes are allowed and this method does not need to be called.
    /// </remarks>
    /// <param name="oldOptions">The old <see cref="ParseOptions"/> of the project from prior to the change.</param>
    /// <param name="newOptions">The new <see cref="ParseOptions"/> of the project that was passed to <see cref="TryApplyChanges(Solution)"/>.</param>
    /// <param name="project">The project contained in the <see cref="Solution"/> passed to <see cref="TryApplyChanges(Solution)"/>.</param>
    public virtual bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
        => false;

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> for each project
    /// that has been added, removed or changed.
    ///
    /// Override this method if you want to modify how project changes are applied.
    /// </summary>
    protected virtual void ApplyProjectChanges(ProjectChanges projectChanges)
    {
        // It's OK to use the null-suppression operator when calling ApplyCompilation/ParseOptionsChanged: the only change that is allowed
        // is going from one non-null value to another which is blocked by the Project.WithCompilationOptions() API directly.

        // The changes in CompilationOptions may include a change to the SyntaxTreeOptionsProvider, which would be happening
        // if an .editorconfig was added, removed, or modified. We'll compute the options without that change, and if there's
        // still changes then we need to verify we can apply those. The .editorconfig changes will also be represented as
        // document edits, which the host is expected to actually apply directly.
        var newOptionsWithoutSyntaxTreeOptionsChange =
            projectChanges.NewProject.CompilationOptions?.WithSyntaxTreeOptionsProvider(
                projectChanges.OldProject.CompilationOptions!.SyntaxTreeOptionsProvider);
        if (projectChanges.OldProject.CompilationOptions != newOptionsWithoutSyntaxTreeOptionsChange)
        {
            this.ApplyCompilationOptionsChanged(projectChanges.ProjectId, newOptionsWithoutSyntaxTreeOptionsChange!);
        }

        // changed parse options
        if (projectChanges.OldProject.ParseOptions != projectChanges.NewProject.ParseOptions)
        {
            this.ApplyParseOptionsChanged(projectChanges.ProjectId, projectChanges.NewProject.ParseOptions!);
        }

        // removed project references
        foreach (var removedProjectReference in projectChanges.GetRemovedProjectReferences())
        {
            this.ApplyProjectReferenceRemoved(projectChanges.ProjectId, removedProjectReference);
        }

        // added project references
        foreach (var addedProjectReference in projectChanges.GetAddedProjectReferences())
        {
            this.ApplyProjectReferenceAdded(projectChanges.ProjectId, addedProjectReference);
        }

        // removed metadata references
        foreach (var metadata in projectChanges.GetRemovedMetadataReferences())
        {
            this.ApplyMetadataReferenceRemoved(projectChanges.ProjectId, metadata);
        }

        // added metadata references
        foreach (var metadata in projectChanges.GetAddedMetadataReferences())
        {
            this.ApplyMetadataReferenceAdded(projectChanges.ProjectId, metadata);
        }

        // removed analyzer references
        foreach (var analyzerReference in projectChanges.GetRemovedAnalyzerReferences())
        {
            this.ApplyAnalyzerReferenceRemoved(projectChanges.ProjectId, analyzerReference);
        }

        // added analyzer references
        foreach (var analyzerReference in projectChanges.GetAddedAnalyzerReferences())
        {
            this.ApplyAnalyzerReferenceAdded(projectChanges.ProjectId, analyzerReference);
        }

        // removed documents
        foreach (var documentId in projectChanges.GetRemovedDocuments())
        {
            this.ApplyDocumentRemoved(documentId);
        }

        // removed additional documents
        foreach (var documentId in projectChanges.GetRemovedAdditionalDocuments())
        {
            this.ApplyAdditionalDocumentRemoved(documentId);
        }

        // removed analyzer config documents
        foreach (var documentId in projectChanges.GetRemovedAnalyzerConfigDocuments())
        {
            this.ApplyAnalyzerConfigDocumentRemoved(documentId);
        }

        // added documents
        foreach (var documentId in projectChanges.GetAddedDocuments())
        {
            var document = projectChanges.NewProject.GetDocument(documentId)!;
            var text = document.GetTextSynchronously(CancellationToken.None);
            var info = CreateDocumentInfoWithoutText(document);
            this.ApplyDocumentAdded(info, text);
        }

        // added additional documents
        foreach (var documentId in projectChanges.GetAddedAdditionalDocuments())
        {
            var document = projectChanges.NewProject.GetAdditionalDocument(documentId)!;
            var text = document.GetTextSynchronously(CancellationToken.None);
            var info = CreateDocumentInfoWithoutText(document);
            this.ApplyAdditionalDocumentAdded(info, text);
        }

        // added analyzer config documents
        foreach (var documentId in projectChanges.GetAddedAnalyzerConfigDocuments())
        {
            var document = projectChanges.NewProject.GetAnalyzerConfigDocument(documentId)!;
            var text = document.GetTextSynchronously(CancellationToken.None);
            var info = CreateDocumentInfoWithoutText(document);
            this.ApplyAnalyzerConfigDocumentAdded(info, text);
        }

        // changed documents
        foreach (var documentId in projectChanges.GetChangedDocuments())
        {
            ApplyChangedDocument(projectChanges, documentId);
        }

        // changed additional documents
        foreach (var documentId in projectChanges.GetChangedAdditionalDocuments())
        {
            var newDoc = projectChanges.NewProject.GetAdditionalDocument(documentId)!;

            // We don't understand the text of additional documents and so we just replace the entire text.
            var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
            this.ApplyAdditionalDocumentTextChanged(documentId, currentText);
        }

        // changed analyzer config documents
        foreach (var documentId in projectChanges.GetChangedAnalyzerConfigDocuments())
        {
            var newDoc = projectChanges.NewProject.GetAnalyzerConfigDocument(documentId)!;

            // We don't understand the text of analyzer config documents and so we just replace the entire text.
            var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
            this.ApplyAnalyzerConfigDocumentTextChanged(documentId, currentText);
        }
    }

    private void ApplyChangedDocument(
        ProjectChanges projectChanges, DocumentId documentId)
    {
        var oldDoc = projectChanges.OldProject.GetDocument(documentId)!;
        var newDoc = projectChanges.NewProject.GetDocument(documentId)!;

        // update text if it's changed (unless it's unchangeable and we were asked to exclude them)
        if (newDoc.HasTextChanged(oldDoc, IgnoreUnchangeableDocumentsWhenApplyingChanges))
        {
            // What we'd like to do here is figure out what actual text changes occurred and pass them on to the host.
            // However, since it is likely that the change was done by replacing the syntax tree, getting the actual text changes is non trivial.

            if (!oldDoc.TryGetText(out var oldText))
            {
                // If we don't have easy access to the old text, then either it was never observed or it was kicked out of memory.
                // Either way, the new text cannot possibly hold knowledge of the changes, and any new syntax tree will not likely be able to derive them.
                // So just use whatever new text we have without preserving text changes.
                var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
                this.ApplyDocumentTextChanged(documentId, currentText);
            }
            else if (!newDoc.TryGetText(out var newText))
            {
                // We have the old text, but no new text is easily available. This typically happens when the content is modified via changes to the syntax tree.
                // Ask document to compute equivalent text changes by comparing the syntax trees, and use them to
                var textChanges = newDoc.GetTextChangesAsync(oldDoc, CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None); // needs wait
                this.ApplyDocumentTextChanged(documentId, oldText.WithChanges(textChanges));
            }
            else
            {
                // We have both old and new text, so assume the text was changed manually.
                // So either the new text already knows the individual changes or we do not have a way to compute them.
                this.ApplyDocumentTextChanged(documentId, newText);
            }
        }
    }

    [Conditional("DEBUG")]
    private static void CheckNoChanges(Solution oldSolution, Solution newSolution)
    {
        var changes = newSolution.GetChanges(oldSolution);
        Contract.ThrowIfTrue(changes.GetAddedProjects().Any());
        Contract.ThrowIfTrue(changes.GetRemovedProjects().Any());
        Contract.ThrowIfTrue(changes.GetProjectChanges().Any());
    }

    private static ProjectInfo CreateProjectInfo(Project project)
    {
        return ProjectInfo.Create(
            project.State.Attributes.With(version: VersionStamp.Create()),
            project.CompilationOptions,
            project.ParseOptions,
            project.Documents.Select(CreateDocumentInfoWithText),
            project.ProjectReferences,
            project.MetadataReferences,
            project.AnalyzerReferences,
            additionalDocuments: project.AdditionalDocuments.Select(CreateDocumentInfoWithText),
            analyzerConfigDocuments: project.AnalyzerConfigDocuments.Select(CreateDocumentInfoWithText),
            hostObjectType: project.State.HostObjectType);
    }

    private static DocumentInfo CreateDocumentInfoWithText(TextDocument doc)
        => CreateDocumentInfoWithoutText(doc).WithTextLoader(TextLoader.From(TextAndVersion.Create(doc.GetTextSynchronously(CancellationToken.None), VersionStamp.Create(), doc.FilePath)));

    internal static DocumentInfo CreateDocumentInfoWithoutText(TextDocument doc)
        => DocumentInfo.Create(
            doc.Id,
            doc.Name,
            doc.Folders,
            doc is Document sourceDoc ? sourceDoc.SourceCodeKind : SourceCodeKind.Regular,
            loader: null,
            filePath: doc.FilePath,
            isGenerated: doc.State.Attributes.IsGenerated)
            .WithDesignTimeOnly(doc.State.Attributes.DesignTimeOnly)
            .WithDocumentServiceProvider(doc.Services);

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a project to the current solution.
    ///
    /// Override this method to implement the capability of adding projects.
    /// </summary>
    protected virtual void ApplyProjectAdded(ProjectInfo project)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddProject));
        this.OnProjectAdded(project);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a project from the current solution.
    ///
    /// Override this method to implement the capability of removing projects.
    /// </summary>
    protected virtual void ApplyProjectRemoved(ProjectId projectId)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveProject));
        this.OnProjectRemoved(projectId);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to change the compilation options.
    ///
    /// Override this method to implement the capability of changing compilation options.
    /// </summary>
    protected virtual void ApplyCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
    {
#if DEBUG
        var oldProject = CurrentSolution.GetRequiredProject(projectId);
        var newProjectForAssert = oldProject.WithCompilationOptions(options);

        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeCompilationOptions) ||
                     CanApplyCompilationOptionChange(oldProject.CompilationOptions!, options, newProjectForAssert));
#endif

        this.OnCompilationOptionsChanged(projectId, options);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to change the parse options.
    ///
    /// Override this method to implement the capability of changing parse options.
    /// </summary>
    protected virtual void ApplyParseOptionsChanged(ProjectId projectId, ParseOptions options)
    {
#if DEBUG
        var oldProject = CurrentSolution.GetRequiredProject(projectId);
        var newProjectForAssert = oldProject.WithParseOptions(options);

        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeParseOptions) ||
                     CanApplyParseOptionChange(oldProject.ParseOptions!, options, newProjectForAssert));
#endif
        this.OnParseOptionsChanged(projectId, options);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a project reference to a project.
    ///
    /// Override this method to implement the capability of adding project references.
    /// </summary>
    protected virtual void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddProjectReference));
        this.OnProjectReferenceAdded(projectId, projectReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a project reference from a project.
    ///
    /// Override this method to implement the capability of removing project references.
    /// </summary>
    protected virtual void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveProjectReference));
        this.OnProjectReferenceRemoved(projectId, projectReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a metadata reference to a project.
    ///
    /// Override this method to implement the capability of adding metadata references.
    /// </summary>
    protected virtual void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddMetadataReference));
        this.OnMetadataReferenceAdded(projectId, metadataReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a metadata reference from a project.
    ///
    /// Override this method to implement the capability of removing metadata references.
    /// </summary>
    protected virtual void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveMetadataReference));
        this.OnMetadataReferenceRemoved(projectId, metadataReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add an analyzer reference to a project.
    ///
    /// Override this method to implement the capability of adding analyzer references.
    /// </summary>
    protected virtual void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddAnalyzerReference));
        this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an analyzer reference from a project.
    ///
    /// Override this method to implement the capability of removing analyzer references.
    /// </summary>
    protected virtual void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAnalyzerReference));
        this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add an analyzer reference to the solution.
    ///
    /// Override this method to implement the capability of adding analyzer references.
    /// </summary>
    internal void ApplySolutionAnalyzerReferenceAdded(AnalyzerReference analyzerReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddSolutionAnalyzerReference));
        this.OnSolutionAnalyzerReferenceAdded(analyzerReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an analyzer reference from the solution.
    ///
    /// Override this method to implement the capability of removing analyzer references.
    /// </summary>
    internal void ApplySolutionAnalyzerReferenceRemoved(AnalyzerReference analyzerReference)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveSolutionAnalyzerReference));
        this.OnSolutionAnalyzerReferenceRemoved(analyzerReference);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new document to a project.
    ///
    /// Override this method to implement the capability of adding documents.
    /// </summary>
    protected virtual void ApplyDocumentAdded(DocumentInfo info, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddDocument));
        this.OnDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a document from a project.
    ///
    /// Override this method to implement the capability of removing documents.
    /// </summary>
    protected virtual void ApplyDocumentRemoved(DocumentId documentId)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveDocument));
        this.OnDocumentRemoved(documentId);
    }

    /// <summary>
    /// This method is called to change the text of a document.
    ///
    /// Override this method to implement the capability of changing document text.
    /// </summary>
    protected virtual void ApplyDocumentTextChanged(DocumentId id, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeDocument));
        this.OnDocumentTextChanged(id, text, PreservationMode.PreserveValue);
    }

    /// <summary>
    /// This method is called to change the info of a document.
    ///
    /// Override this method to implement the capability of changing a document's info.
    /// </summary>
    protected virtual void ApplyDocumentInfoChanged(DocumentId id, DocumentInfo info)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeDocumentInfo));
        this.OnDocumentInfoChanged(id, info);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new additional document to a project.
    ///
    /// Override this method to implement the capability of adding additional documents.
    /// </summary>
    protected virtual void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddAdditionalDocument));
        this.OnAdditionalDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an additional document from a project.
    ///
    /// Override this method to implement the capability of removing additional documents.
    /// </summary>
    protected virtual void ApplyAdditionalDocumentRemoved(DocumentId documentId)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAdditionalDocument));
        this.OnAdditionalDocumentRemoved(documentId);
    }

    /// <summary>
    /// This method is called to change the text of an additional document.
    ///
    /// Override this method to implement the capability of changing additional document text.
    /// </summary>
    protected virtual void ApplyAdditionalDocumentTextChanged(DocumentId id, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeAdditionalDocument));
        this.OnAdditionalDocumentTextChanged(id, text, PreservationMode.PreserveValue);
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new analyzer config document to a project.
    ///
    /// Override this method to implement the capability of adding analyzer config documents.
    /// </summary>
    protected virtual void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.AddAnalyzerConfigDocument));
        this.OnAnalyzerConfigDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
    }

    /// <summary>
    /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an analyzer config document from a project.
    ///
    /// Override this method to implement the capability of removing analyzer config documents.
    /// </summary>
    protected virtual void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAnalyzerConfigDocument));
        this.OnAnalyzerConfigDocumentRemoved(documentId);
    }

    /// <summary>
    /// This method is called to change the text of an analyzer config document.
    ///
    /// Override this method to implement the capability of changing analyzer config document text.
    /// </summary>
    protected virtual void ApplyAnalyzerConfigDocumentTextChanged(DocumentId id, SourceText text)
    {
        Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeAnalyzerConfigDocument));
        this.OnAnalyzerConfigDocumentTextLoaderChanged(id, TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));
    }

    #endregion

    #region Checks and Asserts
    /// <summary>
    /// Throws an exception is the solution is not empty.
    /// </summary>
    protected void CheckSolutionIsEmpty()
        => CheckSolutionIsEmpty(this.CurrentSolution);

    private static void CheckSolutionIsEmpty(Solution solution)
    {
        if (solution.ProjectIds.Any())
        {
            throw new ArgumentException(WorkspacesResources.Workspace_is_not_empty);
        }
    }

    /// <summary>
    /// Throws an exception if the project is not part of the current solution.
    /// </summary>
    protected void CheckProjectIsInCurrentSolution(ProjectId projectId)
        => CheckProjectIsInSolution(this.CurrentSolution, projectId);

    private static void CheckProjectIsInSolution(Solution solution, ProjectId projectId)
    {
        if (!solution.ContainsProject(projectId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_not_part_of_the_workspace,
                solution.Workspace.GetProjectName(projectId)));
        }
    }

    /// <summary>
    /// Throws an exception is the project is part of the current solution.
    /// </summary>
    protected void CheckProjectIsNotInCurrentSolution(ProjectId projectId)
        => CheckProjectIsNotInSolution(this.CurrentSolution, projectId);

    private static void CheckProjectIsNotInSolution(Solution solution, ProjectId projectId)
    {
        if (solution.ContainsProject(projectId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_already_part_of_the_workspace,
                solution.Workspace.GetProjectName(projectId)));
        }
    }

    /// <summary>
    /// Throws an exception if a project does not have a specific project reference.
    /// </summary>
    protected void CheckProjectHasProjectReference(ProjectId fromProjectId, ProjectReference projectReference)
    {
        if (!this.CurrentSolution.GetProject(fromProjectId)!.ProjectReferences.Contains(projectReference))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_not_referenced,
                this.GetProjectName(projectReference.ProjectId)));
        }
    }

    /// <summary>
    /// Throws an exception if a project already has a specific project reference.
    /// </summary>
    protected void CheckProjectDoesNotHaveProjectReference(ProjectId fromProjectId, ProjectReference projectReference)
    {
        if (this.CurrentSolution.GetProject(fromProjectId)!.ProjectReferences.Contains(projectReference))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_already_referenced,
                this.GetProjectName(projectReference.ProjectId)));
        }
    }

    /// <summary>
    /// Throws an exception if project has a transitive reference to another project.
    /// </summary>
    protected void CheckProjectDoesNotHaveTransitiveProjectReference(ProjectId fromProjectId, ProjectId toProjectId)
    {
        var transitiveReferences = this.CurrentSolution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(toProjectId);
        if (transitiveReferences.Contains(fromProjectId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources.Adding_project_reference_from_0_to_1_will_cause_a_circular_reference,
                this.GetProjectName(fromProjectId), this.GetProjectName(toProjectId)));
        }
    }

    /// <summary>
    /// Throws an exception if a project does not have a specific metadata reference.
    /// </summary>
    protected void CheckProjectHasMetadataReference(ProjectId projectId, MetadataReference metadataReference)
    {
        if (!this.CurrentSolution.GetProject(projectId)!.MetadataReferences.Contains(metadataReference))
        {
            throw new ArgumentException(WorkspacesResources.Metadata_is_not_referenced);
        }
    }

    /// <summary>
    /// Throws an exception if a project already has a specific metadata reference.
    /// </summary>
    protected void CheckProjectDoesNotHaveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
    {
        if (this.CurrentSolution.GetProject(projectId)!.MetadataReferences.Contains(metadataReference))
        {
            throw new ArgumentException(WorkspacesResources.Metadata_is_already_referenced);
        }
    }

    /// <summary>
    /// Throws an exception if a project does not have a specific analyzer reference.
    /// </summary>
    protected void CheckProjectHasAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        if (!this.CurrentSolution.GetProject(projectId)!.AnalyzerReferences.Contains(analyzerReference))
        {
            throw new ArgumentException(string.Format(WorkspacesResources._0_is_not_present, analyzerReference));
        }
    }

    /// <summary>
    /// Throws an exception if a project already has a specific analyzer reference.
    /// </summary>
    protected void CheckProjectDoesNotHaveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        if (this.CurrentSolution.GetProject(projectId)!.AnalyzerReferences.Contains(analyzerReference))
        {
            throw new ArgumentException(string.Format(WorkspacesResources._0_is_already_present, analyzerReference));
        }
    }

    /// <summary>
    /// Throws an exception if a project already has a specific analyzer reference.
    /// </summary>
    internal static void CheckSolutionHasAnalyzerReference(Solution solution, AnalyzerReference analyzerReference)
    {
        if (!solution.AnalyzerReferences.Contains(analyzerReference))
        {
            throw new ArgumentException(string.Format(WorkspacesResources._0_is_not_present, analyzerReference));
        }
    }

    /// <summary>
    /// Throws an exception if a project already has a specific analyzer reference.
    /// </summary>
    internal static void CheckSolutionDoesNotHaveAnalyzerReference(Solution solution, AnalyzerReference analyzerReference)
    {
        if (solution.AnalyzerReferences.Contains(analyzerReference))
        {
            throw new ArgumentException(string.Format(WorkspacesResources._0_is_already_present, analyzerReference));
        }
    }

    /// <summary>
    /// Throws an exception if a document is not part of the current solution.
    /// </summary>
    protected void CheckDocumentIsInCurrentSolution(DocumentId documentId)
        => CheckDocumentIsInSolution(this.CurrentSolution, documentId);

    private static void CheckDocumentIsInSolution(Solution solution, DocumentId documentId)
    {
        if (solution.GetDocument(documentId) == null)
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_not_part_of_the_workspace,
                solution.Workspace.GetDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Throws an exception if an additional document is not part of the current solution.
    /// </summary>
    protected void CheckAdditionalDocumentIsInCurrentSolution(DocumentId documentId)
        => CheckAdditionalDocumentIsInSolution(this.CurrentSolution, documentId);

    private static void CheckAdditionalDocumentIsInSolution(Solution solution, DocumentId documentId)
    {
        if (solution.GetAdditionalDocument(documentId) == null)
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_not_part_of_the_workspace,
                solution.Workspace.GetDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Throws an exception if an analyzer config is not part of the current solution.
    /// </summary>
    protected void CheckAnalyzerConfigDocumentIsInCurrentSolution(DocumentId documentId)
        => CheckAnalyzerConfigDocumentIsInSolution(this.CurrentSolution, documentId);

    private static void CheckAnalyzerConfigDocumentIsInSolution(Solution solution, DocumentId documentId)
    {
        if (!solution.ContainsAnalyzerConfigDocument(documentId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_not_part_of_the_workspace,
                solution.Workspace.GetDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Throws an exception if a document is already part of the current solution.
    /// </summary>
    protected void CheckDocumentIsNotInCurrentSolution(DocumentId documentId)
    {
        if (this.CurrentSolution.ContainsDocument(documentId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_already_part_of_the_workspace,
                this.GetDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Throws an exception if an additional document is already part of the current solution.
    /// </summary>
    protected void CheckAdditionalDocumentIsNotInCurrentSolution(DocumentId documentId)
        => CheckAdditionalDocumentIsNotInSolution(this.CurrentSolution, documentId);

    private static void CheckAdditionalDocumentIsNotInSolution(Solution solution, DocumentId documentId)
    {
        if (solution.ContainsAdditionalDocument(documentId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_already_part_of_the_workspace,
                solution.Workspace.GetAdditionalDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Throws an exception if the analyzer config document is already part of the current solution.
    /// </summary>
    protected void CheckAnalyzerConfigDocumentIsNotInCurrentSolution(DocumentId documentId)
        => CheckAnalyzerConfigDocumentIsNotInSolution(this.CurrentSolution, documentId);

    private static void CheckAnalyzerConfigDocumentIsNotInSolution(Solution solution, DocumentId documentId)
    {
        if (solution.ContainsAnalyzerConfigDocument(documentId))
        {
            throw new ArgumentException(string.Format(
                WorkspacesResources._0_is_already_part_of_the_workspace,
                solution.Workspace.GetAnalyzerConfigDocumentName(documentId)));
        }
    }

    /// <summary>
    /// Gets the name to use for a project in an error message.
    /// </summary>
    protected virtual string GetProjectName(ProjectId projectId)
    {
        var project = this.CurrentSolution.GetProject(projectId);
        var name = project != null ? project.Name : "<Project" + projectId.Id + ">";
        return name;
    }

    /// <summary>
    /// Gets the name to use for a document in an error message.
    /// </summary>
    protected virtual string GetDocumentName(DocumentId documentId)
    {
        var document = this.CurrentSolution.GetTextDocument(documentId);
        var name = document != null ? document.Name : "<Document" + documentId.Id + ">";
        return name;
    }

    /// <summary>
    /// Gets the name to use for an additional document in an error message.
    /// </summary>
    protected virtual string GetAdditionalDocumentName(DocumentId documentId)
        => GetDocumentName(documentId);

    /// <summary>
    /// Gets the name to use for an analyzer document in an error message.
    /// </summary>
    protected virtual string GetAnalyzerConfigDocumentName(DocumentId documentId)
        => GetDocumentName(documentId);

    #endregion
}
