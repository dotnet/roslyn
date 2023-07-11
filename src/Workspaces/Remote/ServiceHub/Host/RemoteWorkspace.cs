﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.Threading.ThreadingTools;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Workspace created by the remote host that mirrors the corresponding client workspace.
    /// </summary>
    internal sealed partial class RemoteWorkspace : Workspace
    {
        /// <summary>
        /// Guards updates to all mutable state in this workspace.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Used to make sure we never move remote workspace backward. this version is the WorkspaceVersion of primary
        /// solution in client (VS) we are currently caching.
        /// </summary>
        private int _currentRemoteWorkspaceVersion = -1;

        // internal for testing purposes.
        internal RemoteWorkspace(HostServices hostServices)
            : base(hostServices, WorkspaceKind.RemoteWorkspace)
        {
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);
            Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Unregister(this);
        }

        public AssetProvider CreateAssetProvider(Checksum solutionChecksum, SolutionAssetCache assetCache, IAssetSource assetSource)
        {
            var serializerService = Services.GetRequiredService<ISerializerService>();
            return new AssetProvider(solutionChecksum, assetCache, assetSource, serializerService);
        }

        protected internal override bool PartialSemanticsEnabled => true;

        /// <summary>
        /// Syncs over the solution corresponding to <paramref name="solutionChecksum"/> and sets it as the current
        /// solution for <see langword="this"/> workspace.  This will also end up updating <see
        /// cref="_lastRequestedAnyBranchSolution"/> and <see cref="_lastRequestedPrimaryBranchSolution"/>, allowing
        /// them to be pre-populated for feature requests that come in soon after this call completes.
        /// </summary>
        public async Task UpdatePrimaryBranchSolutionAsync(
            AssetProvider assetProvider, Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            // See if the current snapshot we're pointing at is the same one the host wants us to sync to.  If so, we
            // don't need to do anything.
            var currentSolutionChecksum = await this.CurrentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (currentSolutionChecksum == solutionChecksum)
                return;

            // Do a normal Run with a no-op for `implementation`.  This will still ensure that we compute and cache this
            // checksum/solution pair for future callers.
            await RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                workspaceVersion,
                updatePrimaryBranch: true,
                implementation: static _ => ValueTaskFactory.FromResult(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Given an appropriate <paramref name="solutionChecksum"/>, gets or computes the corresponding <see
        /// cref="Solution"/> snapshot for it, and then invokes <paramref name="implementation"/> with that snapshot.  That
        /// snapshot and the result of <paramref name="implementation"/> are then returned from this method.  Note: the
        /// solution returned is only for legacy cases where we expose OOP to 2nd party clients who expect to be able to
        /// call through <see cref="RemoteWorkspaceManager.GetSolutionAsync"/> and who expose that statically to
        /// themselves.
        /// <para>
        /// During the life of the call to <paramref name="implementation"/> the solution corresponding to <paramref
        /// name="solutionChecksum"/> will be kept alive and returned to any other concurrent calls to this method with
        /// the same <paramref name="solutionChecksum"/>.
        /// </para>
        /// </summary>
        public ValueTask<(Solution solution, T result)> RunWithSolutionAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            Func<Solution, ValueTask<T>> implementation,
            CancellationToken cancellationToken)
        {
            return RunWithSolutionAsync(assetProvider, solutionChecksum, workspaceVersion: -1, updatePrimaryBranch: false, implementation, cancellationToken);
        }

        private async ValueTask<(Solution solution, T result)> RunWithSolutionAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool updatePrimaryBranch,
            Func<Solution, ValueTask<T>> implementation,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solutionChecksum);
            Contract.ThrowIfTrue(solutionChecksum == Checksum.Null);

            // Gets or creates a solution corresponding to the requested checksum.  This will always succeed, and will
            // increment the in-flight of that solution until we decrement it at the end of our try/finally block.
            var (inFlightSolution, solutionTask) = await AcquireSolutionAndIncrementInFlightCountAsync().ConfigureAwait(false);

            try
            {
                return await ProcessSolutionAsync(inFlightSolution, solutionTask).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken, ErrorSeverity.Critical))
            {
                // Any non-cancellation exception is bad and needs to be reported.  We will still ensure that we cleanup
                // below though no matter what happens so that other calls to OOP can properly work.
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                await DecrementInFlightCountAsync(inFlightSolution).ConfigureAwait(false);
            }

            // Gets or creates a solution corresponding to the requested checksum.  This will always succeed, and will
            // increment the in-flight of that solution until we decrement it at the end of our try/finally block.
            async ValueTask<(InFlightSolution inFlightSolution, Task<Solution> solutionTask)> AcquireSolutionAndIncrementInFlightCountAsync()
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        inFlightSolution = GetOrCreateSolutionAndAddInFlightCount_NoLock(
                            assetProvider, solutionChecksum, workspaceVersion, updatePrimaryBranch);
                        solutionTask = inFlightSolution.PreferredSolutionTask_NoLock;

                        // We must have at least 1 for the in-flight-count (representing this current in-flight call).
                        Contract.ThrowIfTrue(inFlightSolution.InFlightCount < 1);

                        return (inFlightSolution, solutionTask);
                    }
                    catch (Exception ex) when (FatalError.ReportAndPropagate(ex, ErrorSeverity.Critical))
                    {
                        // Any exception thrown in the above (including cancellation) is critical and unrecoverable.  We
                        // will have potentially started work, while also leaving ourselves in some inconsistent state.
                        throw ExceptionUtilities.Unreachable();
                    }
                }
            }

            async ValueTask<(Solution solution, T result)> ProcessSolutionAsync(InFlightSolution inFlightSolution, Task<Solution> solutionTask)
            {
                // We must have at least 1 for the in-flight-count (representing this current in-flight call).
                Contract.ThrowIfTrue(inFlightSolution.InFlightCount < 1);

                // Actually get the solution, computing it ourselves, or getting the result that another caller was
                // computing. Note: we use our own cancellation token here as the task is currently operating using a
                // private CTS token that inFlightSolution controls.
                var solution = await solutionTask.WithCancellation(cancellationToken).ConfigureAwait(false);

                // now that we've computed the solution, cache it to help out future requests.
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (updatePrimaryBranch)
                        _lastRequestedPrimaryBranchSolution = (solutionChecksum, solution);
                    else
                        _lastRequestedAnyBranchSolution = (solutionChecksum, solution);
                }

                // Now, pass it to the callback to do the work.  Any other callers into us will be able to benefit from
                // using this same solution as well
                var result = await implementation(solution).ConfigureAwait(false);

                return (solution, result);
            }

            async ValueTask DecrementInFlightCountAsync(InFlightSolution inFlightSolution)
            {
                // All this work is intentionally not cancellable.  We must do the decrement to ensure our cache state
                // is consistent. This will block the calling thread.  However, this should only be for a short amount
                // of time as nothing in RemoteWorkspace should ever hold this lock for long periods of time.

                try
                {
                    ImmutableArray<Task> solutionComputationTasks;
                    using (await _gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
                    {

                        // finally, decrement our in-flight-count on the solution.  If we were the last one keeping it alive, it
                        // will get removed from our caches.
                        solutionComputationTasks = inFlightSolution.DecrementInFlightCount_NoLock();
                    }

                    // If we were the request that decremented the in-flight-count to 0, then ensure we wait for all the
                    // solution-computation tasks to finish.  If we do not do this then it's possible for this call to
                    // return all the way back to the host side unpinning the solution we have pinned there.  This may
                    // happen concurrently with the solution-computation calls calling back into the host which will
                    // then crash due to that solution no longer being pinned there.  While this does force this caller
                    // to wait for those tasks to stop, this should ideally be fast as they will have been cancelled
                    // when the in-flight-count went to 0.
                    //
                    // Use a NoThrowAwaitable as we want to await all tasks here regardless of how individual ones may cancel.
                    foreach (var task in solutionComputationTasks)
                        await task.NoThrowAwaitable(false);
                }
                catch (Exception ex) when (FatalError.ReportAndPropagate(ex, ErrorSeverity.Critical))
                {
                    // Similar to AcquireSolutionAndIncrementInFlightCountAsync Any exception thrown in the above
                    // (including cancellation) is critical and unrecoverable.  We must clean up our state, and anything
                    // that prevents that could leave us in an inconsistent position.
                }
            }
        }

        /// <summary>
        /// Create an appropriate <see cref="Solution"/> instance corresponding to the <paramref
        /// name="solutionChecksum"/> passed in.  Note: this method changes no Workspace state and exists purely to
        /// compute the corresponding solution.  Updating of our caches, or storing this solution as the <see
        /// cref="Workspace.CurrentSolution"/> of this <see cref="RemoteWorkspace"/> is the responsibility of any
        /// callers.
        /// <para>
        /// The term 'disconnected' is used to mean that this solution is not assigned to be the current solution of
        /// this <see cref="RemoteWorkspace"/>.  It is effectively a fork of that instead.
        /// </para>
        /// <para>
        /// This method will either create the new solution from scratch if it has to.  Or it will attempt to create a
        /// fork off of <see cref="Workspace.CurrentSolution"/> if possible.  The latter is almost always what will
        /// happen (once the first sync completes) as most calls to the remote workspace are using a solution snapshot
        /// very close to the primary one, and so can share almost all state with that.
        /// </para>
        /// </summary>
        private async Task<Solution> ComputeDisconnectedSolutionAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            CancellationToken cancellationToken)
        {
            try
            {
                // Try to create the solution snapshot incrementally off of the workspaces CurrentSolution first.
                var updater = new SolutionCreator(Services.HostServices, assetProvider, this.CurrentSolution);
                if (await updater.IsIncrementalUpdateAsync(solutionChecksum, cancellationToken).ConfigureAwait(false))
                {
                    return await updater.CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Otherwise, this is a different solution, or the first time we're creating this solution.  Bulk
                    // sync over all assets for it.
                    await assetProvider.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                    // get new solution info and options
                    var solutionInfo = await assetProvider.CreateSolutionInfoAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                    return CreateSolutionFromInfo(solutionInfo);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private Solution CreateSolutionFromInfo(SolutionInfo solutionInfo)
        {
            var solution = this.CreateSolution(solutionInfo);
            foreach (var projectInfo in solutionInfo.Projects)
                solution = solution.AddProject(projectInfo);
            return solution;
        }

        /// <summary>
        /// Attempts to update this workspace with the given <paramref name="newSolution"/>.  If this succeeds, <see
        /// langword="true"/> will be returned in the tuple result as well as the actual solution that the workspace is
        /// updated to point at.  If we cannot update this workspace, then <see langword="false"/> will be returned,
        /// along with the solution passed in.  The only time the solution can not be updated is if it would move <see
        /// cref="_currentRemoteWorkspaceVersion"/> backwards.
        /// </summary>
        private async Task<Solution> TryUpdateWorkspaceCurrentSolutionAsync(
            int workspaceVersion,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            var (solution, _) = await TryUpdateWorkspaceCurrentSolutionWorkerAsync(workspaceVersion, newSolution, cancellationToken).ConfigureAwait(false);
            return solution;
        }

        private async ValueTask<(Solution solution, bool updated)> TryUpdateWorkspaceCurrentSolutionWorkerAsync(
            int workspaceVersion,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Never move workspace backward
                if (workspaceVersion <= _currentRemoteWorkspaceVersion)
                    return (newSolution, updated: false);

                _currentRemoteWorkspaceVersion = workspaceVersion;

                // if either solution id or file path changed, then we consider it as new solution. Otherwise,
                // update the current solution in place.

                // Ensure we update newSolution with the result of SetCurrentSolution.  It will be the one appropriately
                // 'attached' to this workspace.
                (_, newSolution) = this.SetCurrentSolution(
                    _ => newSolution,
                    changeKind: static (oldSolution, newSolution) =>
                        (IsAddingSolution(oldSolution, newSolution) ? WorkspaceChangeKind.SolutionAdded : WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
                    onBeforeUpdate: (oldSolution, newSolution) =>
                    {
                        if (IsAddingSolution(oldSolution, newSolution))
                        {
                            // We're not doing an update, we're moving to a new solution entirely.  Clear out the old one. This
                            // is necessary so that we clear out any open document information this workspace is tracking. Note:
                            // this seems suspect as the remote workspace should not be tracking any open document state.
                            this.ClearSolutionData();
                        }
                    });

                return (newSolution, updated: true);
            }

            static bool IsAddingSolution(Solution oldSolution, Solution newSolution)
                => oldSolution.Id != newSolution.Id || oldSolution.FilePath != newSolution.FilePath;
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor
        {
            private readonly RemoteWorkspace _remoteWorkspace;

            public TestAccessor(RemoteWorkspace remoteWorkspace)
            {
                _remoteWorkspace = remoteWorkspace;
            }

            public Solution CreateSolutionFromInfo(SolutionInfo solutionInfo)
                => _remoteWorkspace.CreateSolutionFromInfo(solutionInfo);

            public ValueTask<(Solution solution, bool updated)> TryUpdateWorkspaceCurrentSolutionAsync(Solution newSolution, int workspaceVersion)
                => _remoteWorkspace.TryUpdateWorkspaceCurrentSolutionWorkerAsync(workspaceVersion, newSolution, CancellationToken.None);

            public async ValueTask<Solution> GetSolutionAsync(
                AssetProvider assetProvider,
                Checksum solutionChecksum,
                bool updatePrimaryBranch,
                int workspaceVersion,
                CancellationToken cancellationToken)
            {
                var (solution, _) = await _remoteWorkspace.RunWithSolutionAsync(
                    assetProvider, solutionChecksum, workspaceVersion, updatePrimaryBranch, _ => ValueTaskFactory.FromResult(false), cancellationToken).ConfigureAwait(false);
                return solution;
            }
        }
    }
}
