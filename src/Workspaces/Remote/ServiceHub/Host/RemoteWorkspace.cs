// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.SolutionCrawler;
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
        /// Guards updates to all mutable state in this workspace.  The caches below will also use this same gate to
        /// mutate themselves.  This keeps the caches in sync with each other.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// Cache of checksums/solutions for any request that came through.
        /// </summary>
        private readonly ChecksumToSolutionCache _anyBranchSolutionCache;

        /// <summary>
        /// Cache of checksums/solutions for requests to update the primary-solution (e.g. the solution this <see
        /// cref="RemoteWorkspace"/> should actually use as its <see cref="Workspace.CurrentSolution"/>).  When requests
        /// come through, we should always try to service them from this cache if possible, or fallback to the
        /// <see cref="_anyBranchSolutionCache"/> otherwise.
        /// </summary>
        private readonly ChecksumToSolutionCache _primaryBranchSolutionCache;

        /// <summary>
        /// Used to make sure we never move remote workspace backward. this version is the WorkspaceVersion of primary
        /// solution in client (VS) we are currently caching.
        /// </summary>
        private int _currentRemoteWorkspaceVersion = -1;

        // internal for testing purposes.
        internal RemoteWorkspace(HostServices hostServices, string? workspaceKind)
            : base(hostServices, workspaceKind)
        {
            // Pass along our gate to our two caches.  This way all mutation is kept in sync.
            _anyBranchSolutionCache = new ChecksumToSolutionCache(_gate);
            _primaryBranchSolutionCache = new ChecksumToSolutionCache(_gate);
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

        /// <summary>
        /// Syncs over the solution corresponding to <paramref name="solutionChecksum"/> and sets it as the current
        /// solution for <see langword="this"/> workspace.  This will also end up updating <see
        /// cref="_anyBranchSolutionCache"/> and <see cref="_primaryBranchSolutionCache"/>, allowing them to be pre-populated for
        /// feature requests that come in soon after this call completes.
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
            // increment the ref count of that solution until we release it at the end of our using block.
            using var solutionAndInFlightCount = await GetOrCreateSolutionAsync(
                assetProvider, solutionChecksum, workspaceVersion, updatePrimaryBranch, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(solutionAndInFlightCount.InFlightCount < 1);

            // Store this around so that if another call comes through for this same checksum, they will see the
            // solution we just computed, even if we have returned.  This also ensures that if we promoted a
            // non-primary-solution to a primary-solution that it will now take precedence in all our caches for this
            // particular checksum.
            await _anyBranchSolutionCache.SetLastRequestedSolutionAsync(solutionChecksum, refCountedSolution, cancellationToken).ConfigureAwait(false);
            if (updatePrimaryBranch)
                await _primaryBranchSolutionCache.SetLastRequestedSolutionAsync(solutionChecksum, refCountedSolution, cancellationToken).ConfigureAwait(false);

            // Actually get the solution, computing it ourselves, or getting the result that another caller was
            // computing. In the event of cancellation, we do not wait here for the refCountedSolution to clean up,
            // even if this was the last use of this solution.
            var newSolution = await refCountedSolution.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

            // Now, pass it to the callback to do the work.  Any other callers into us will be able to benefit from
            // using this same solution as well
            var result = await implementation(newSolution).ConfigureAwait(false);

            // finally, implicitly release our ref-count on the solution.  If we were the last one keeping it alive, it
            // will get released from our caches.

            return (newSolution, result);
        }

        private async ValueTask<ChecksumToSolutionCache.SolutionAndInFlightCount> GetOrCreateSolutionAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool updatePrimaryBranch,
            CancellationToken cancellationToken)
        {
            // Always try to retrieve cached solutions from the primary branch first.  That way we can use the solutions
            // that were the real solutions of this Workspace, and not ones forked off from that.  This gives the
            // highest likelihood of sharing data and being able to reuse workspace caches and services shared among all
            // components.
            var primaryBranchRefCountedSolution = await _primaryBranchSolutionCache.TryFastGetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
            if (primaryBranchRefCountedSolution != null)
            {
                Contract.ThrowIfTrue(primaryBranchRefCountedSolution.RefCount < 1);
                return primaryBranchRefCountedSolution;
            }

            // Otherwise, have the any-branch solution try to retrieve or create the solution.  This will always
            // succeed, and must give us back a solution with a ref-count that is keeping it alive.
            var anyBranchRefCountedSolution =
                await _anyBranchSolutionCache.TryFastGetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false) ??
                await _anyBranchSolutionCache.SlowGetOrCreateSolutionAsync(
                    solutionChecksum,
                    cancellationToken => ComputeSolutionAsync(assetProvider, solutionChecksum, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(anyBranchRefCountedSolution.RefCount < 1);

            if (!updatePrimaryBranch)
            {
                // if we aren't updating the primary branch we're done and can just return the any-branch solution.
                // note: the ref-count of this item is still correct.  the calls above to TryFastGetSolutionAsync or 
                // SlowGetOrCreateSolutionAsync will have appropriately raised it for us.
                return anyBranchRefCountedSolution;
            }

            // We were asked to update the primary-branch solution.  So take the any-branch solution and promote it to
            // the primary-branch-level.  This may return a different solution.  So ensure that we release our reference
            // on the anyBranch solution when we're done. SlowGetOrCreateSolutionAsync will ensure the refcount of the
            // solution it returns is proper, so this is safe to do.
            using (anyBranchRefCountedSolution)
            {
                var anyBranchSolution = await anyBranchRefCountedSolution.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

                var finalRefCountedSolution = await _primaryBranchSolutionCache.SlowGetOrCreateSolutionAsync(
                    solutionChecksum,
                    async cancellationToken =>
                    {
                        var (primaryBranchSolution, _) = await this.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, anyBranchSolution, cancellationToken).ConfigureAwait(false);
                        return primaryBranchSolution;
                    },
                    cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfTrue(anyBranchRefCountedSolution.RefCount < 1);
                Contract.ThrowIfTrue(finalRefCountedSolution.RefCount < 1);
            }
        }

        /// <summary>
        /// Create an appropriate <see cref="Solution"/> instance corresponding to the <paramref
        /// name="solutionChecksum"/> passed in.  Note: this method changes no Workspace state and exists purely to
        /// compute the corresponding solution.  Updating of our caches, or storing this solution as the <see
        /// cref="Workspace.CurrentSolution"/> of this <see cref="RemoteWorkspace"/> is the responsibility of any
        /// callers.
        /// <para>
        /// This method will either create the new solution from scratch if it has to.  Or it will attempt to create a
        /// fork off of <see cref="Workspace.CurrentSolution"/> if possible.  The latter is almost always what will
        /// happen (once the first sync completes) as most calls to the remote workspace are using a solution snapshot
        /// very close to the primary one, and so can share almost all state with that.
        /// </para>
        /// </summary>
        private async Task<Solution> ComputeSolutionAsync(
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
                throw ExceptionUtilities.Unreachable;
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
        private async ValueTask<(Solution solution, bool updated)> TryUpdateWorkspaceCurrentSolutionAsync(
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

                var oldSolution = CurrentSolution;
                var addingSolution = oldSolution.Id != newSolution.Id || oldSolution.FilePath != newSolution.FilePath;
                if (addingSolution)
                {
                    // We're not doing an update, we're moving to a new solution entirely.  Clear out the old one. This
                    // is necessary so that we clear out any open document information this workspace is tracking. Note:
                    // this seems suspect as the remote workspace should not be tracking any open document state.
                    ClearSolutionData();
                }

                newSolution = SetCurrentSolution(newSolution);

                _ = RaiseWorkspaceChangedEventAsync(
                    addingSolution ? WorkspaceChangeKind.SolutionAdded : WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

                return (newSolution, updated: true);
            }
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
                => _remoteWorkspace.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, newSolution, CancellationToken.None);

            public async ValueTask<Solution> GetSolutionAsync(
                AssetProvider assetProvider,
                Checksum solutionChecksum,
                bool updatePrimaryBranch,
                int workspaceVersion,
                CancellationToken cancellationToken)
            {
                var tuple = await _remoteWorkspace.RunWithSolutionAsync(
                    assetProvider, solutionChecksum, workspaceVersion, updatePrimaryBranch, _ => ValueTaskFactory.FromResult(false), cancellationToken).ConfigureAwait(false);
                return tuple.solution;
            }
        }
    }
}
