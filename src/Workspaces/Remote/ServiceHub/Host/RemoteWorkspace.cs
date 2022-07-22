// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.Threading.ThreadingTools;
using static Microsoft.VisualStudio.Threading.TplExtensions;

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

            // Do a no-op run.  This will still ensure that we compute and cache this checksum/solution pair for future callers.
            await RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                workspaceVersion,
                updatePrimaryBranch: true,
                static _ => ValueTaskFactory.FromResult(false),
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
            // !!!CRITICAL!!! Ensure we immediately place the refCountedLazySolution in a using-block.  That way if
            // anything causes us to abort/cancel, we'll reduce the incremented ref-count that GetOrCreateSolutionAsync
            // caused.  Do NOT add anything between this line and the next.
            var refCountedLazySolution = await GetOrCreateSolutionAsync(
                assetProvider, solutionChecksum, workspaceVersion, updatePrimaryBranch, cancellationToken).ConfigureAwait(false);
            await using var _ = refCountedLazySolution.ConfigureAwait(false);

            // Store this around so that if another call comes through for this same checksum, they will see the
            // solution we just computed, even if we have returned.  This also ensures that if we promoted a
            // non-primary-solution to a primary-solution that it will now take precedence in all our caches for this
            // particular checksum.
            await _anyBranchSolutionCache.SetLastRequestedSolutionAsync(solutionChecksum, refCountedLazySolution, cancellationToken).ConfigureAwait(false);
            if (updatePrimaryBranch)
                await _primaryBranchSolutionCache.SetLastRequestedSolutionAsync(solutionChecksum, refCountedLazySolution, cancellationToken).ConfigureAwait(false);

            // Actually get the solution, computing it ourselves, or getting the result that another caller was
            // computing. In the event of cancellation, we do not wait here for the refCountedLazySolution to clean up,
            // even if this was the last use of this solution.
            var newSolution = await refCountedLazySolution.Target.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

            // Now, pass it to the callback to do the work.  Any other callers into us will be able to benefit from
            // using this same solution as well
            var result = await implementation(newSolution).ConfigureAwait(false);

            return (newSolution, result);
        }

        private async ValueTask<ReferenceCountedDisposable<LazySolution>> GetOrCreateSolutionAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool updatePrimaryBranch,
            CancellationToken cancellationToken)
        {
            // Always try to retrieve cached solutions from the primary branch first.  That way we can use the solutions
            // that were the real solutions of this Workspace, and not ones forked off from that.  This gives the
            // highest likelihood of sharing data and being able to reuse caches and services shared among all
            // components.
            var primaryBranchRefCountedSolution = await _primaryBranchSolutionCache.TryFastGetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
            if (primaryBranchRefCountedSolution != null)
                return primaryBranchRefCountedSolution;

            // Otherwise, have the any-branch solution try to retrieve or create the solution.
            var anyBranchRefCountedSolution =
                await _anyBranchSolutionCache.TryFastGetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false) ??
                await _anyBranchSolutionCache.SlowGetOrCreateSolutionAsync(
                    solutionChecksum,
                    cancellationToken => ComputeSolutionAsync(assetProvider, solutionChecksum, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

            if (!updatePrimaryBranch)
            {
                // if we aren't updating the primary branch we're done and can just return the any-branch solution
                return anyBranchRefCountedSolution;
            }

            // We were asked to update the primary-branch solution.  So take the any-branch solution and promote it to
            // the primary-branch-level.
            //
            // Ensure we release the ref count on this solution once we're done.
            await using var _ = anyBranchRefCountedSolution.ConfigureAwait(false);

            var anyBranchSolution = await anyBranchRefCountedSolution.Target.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

            return await _primaryBranchSolutionCache.SlowGetOrCreateSolutionAsync(
                solutionChecksum,
                async cancellationToken =>
                {
                    var (primaryBranchSolution, _) = await this.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, anyBranchSolution, cancellationToken).ConfigureAwait(false);
                    return primaryBranchSolution;
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The workspace is designed to be stateless. If someone asks for a solution (through solution checksum), 
        /// it will create one and return the solution. The engine takes care of syncing required data and creating a solution
        /// corresponding to the given checksum.
        /// 
        /// but doing that from scratch all the time will be expansive in terms of syncing data, compilation being cached, file being parsed
        /// and etc. so even if the service itself is stateless, internally it has several caches to improve perf of various parts.
        /// 
        /// first, it holds onto last solution got built. this will take care of common cases where multiple services running off same solution.
        /// second, it uses assets cache to hold onto data just synched (within 3 min) so that if it requires to build new solution, 
        ///         it can save some time to re-sync data which might just used by other solution.
        /// third, it holds onto solution from primary branch from Host. and it will try to see whether it can build new solution off the
        ///        primary solution it is holding onto. this will make many solution level cache to be re-used.
        ///
        /// the primary solution can be updated in 2 ways.
        /// first, host will keep track of primary solution changes in host, and call OOP to synch to latest time to time.
        /// second, engine keeps track of whether a certain request is for primary solution or not, and if it is, 
        ///         it let that request to update primary solution cache to latest.
        /// 
        /// these 2 are complimentary to each other. #1 makes OOP's primary solution to be ready for next call (push), #2 makes OOP's primary
        /// solution be not stale as much as possible. (pull)
        /// </summary>
        private async Task<Solution> ComputeSolutionAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            CancellationToken cancellationToken)
        {
            try
            {
                var updater = new SolutionCreator(Services.HostServices, assetProvider, this.CurrentSolution);

                // check whether solution is update to the given base solution
                if (await updater.IsIncrementalUpdateAsync(solutionChecksum, cancellationToken).ConfigureAwait(false))
                {
                    // create updated solution off the baseSolution
                    return await updater.CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                }

                // we need new solution. bulk sync all asset for the solution first.
                await assetProvider.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // get new solution info and options
                var solutionInfo = await assetProvider.CreateSolutionInfoAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                return CreateSolutionFromInfo(solutionInfo);
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

        private sealed class ChecksumToSolutionCache
        {
            /// <summary>
            /// Pointer to <see cref="RemoteWorkspace._gate"/>.
            /// </summary>
            private readonly SemaphoreSlim _gate;

            /// <summary>
            /// The last checksum and solution requested by a service. This effectively adds an additional ref count to
            /// one of the items in <see cref="_solutionChecksumToLazySolution"/> ensuring that the very last solution
            /// requested is kept alive by us, even if there are no active requests currently in progress for that
            /// solution.  That way if we have two non-concurrent requests for that same solution, with no intervening
            /// updates, we can cache and keep the solution around instead of having to recompute it.
            /// </summary>
            private Checksum? _lastRequestedChecksum;
            private ReferenceCountedDisposable<LazySolution>? _lastRequestedSolution;

            /// <summary>
            /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a
            /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
            /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
            /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.
            /// </summary>
            private readonly Dictionary<Checksum, ReferenceCountedDisposable<LazySolution>> _solutionChecksumToLazySolution = new();

            public ChecksumToSolutionCache(SemaphoreSlim gate)
            {
                _gate = gate;
            }

            public async ValueTask<ReferenceCountedDisposable<LazySolution>?> TryFastGetSolutionAsync(
                Checksum solutionChecksum,
                CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(solutionChecksum);

                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Adding refcounts in the cases where we see we have a direct reference to the solution cannot
                    // fail.  We only have a matching item in _lastRequestedChecksum if it had a positive refcount
                    // already added to it when it was placed in this cache.  And the only thing that releases that
                    // refcount item is code that runs while holding the same _gate that we are holding, and then
                    // overwrites the item.  So it's not possible for us to race with anything on this, and so we must
                    // succeed.

                    if (_lastRequestedChecksum == solutionChecksum)
                        return _lastRequestedSolution!.TryAddReference() ?? throw ExceptionUtilities.Unreachable;

                    // Next, see if it's the map of items being looked at by another request.  Note: this may fail to add
                    // the ref count as we might see an item that is disposed of, but hasn't been cleaned from this cache
                    // yet.
                    if (_solutionChecksumToLazySolution.TryGetValue(solutionChecksum, out var refCountedLazySolution))
                        return refCountedLazySolution.TryAddReference();

                    return null;
                }
            }

            public async ValueTask<ReferenceCountedDisposable<LazySolution>> SlowGetOrCreateSolutionAsync(
                Checksum solutionChecksum, Func<CancellationToken, Task<Solution>> getSolutionAsync, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_solutionChecksumToLazySolution.TryGetValue(solutionChecksum, out var refCountedLazySolution))
                    {
                        var lazySolutionInstance = refCountedLazySolution.TryAddReference();
                        if (lazySolutionInstance is not null)
                            return lazySolutionInstance;

                        // Remove the value since it's clearly no longer usable. The cleanupAsync method would have
                        // removed this value, but has not completed its execution yet.
                        _solutionChecksumToLazySolution.Remove(solutionChecksum);
                    }

                    // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                    // refcount of 1 (for 'us').
                    refCountedLazySolution = null;
                    var lazySolution = new LazySolution(
                        getSolutionAsync,
                        cleanupAsync: async () =>
                        {
                            // We use CancellationToken.None here as we have to ensure the lazy solution is removed from the
                            // checksum map, or else we will have a memory leak.  This should hopefully not ever be an issue as we
                            // only ever hold this gate for very short periods of time in order to set do basic operations on our
                            // state.
                            using var _ = await _gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false);

                            // Only remove a value from the map if it still exists and holds the same expected instance
                            if (_solutionChecksumToLazySolution.TryGetValue(solutionChecksum, out var remainingRefCountedLazySolution)
                                && remainingRefCountedLazySolution == refCountedLazySolution)
                            {
                                _solutionChecksumToLazySolution.Remove(solutionChecksum);
                            }
                        });
                    refCountedLazySolution = new ReferenceCountedDisposable<LazySolution>(lazySolution);
                    _solutionChecksumToLazySolution.Add(solutionChecksum, refCountedLazySolution);
                    return refCountedLazySolution;
                }
            }

            public async Task SetLastRequestedSolutionAsync(Checksum solutionChecksum, ReferenceCountedDisposable<LazySolution> solution, CancellationToken cancellationToken)
            {
                // Collect the existing solution we're pointing at so we can decrease its ref count.  Do this out of the
                // lock to prevent deadlocks when DisposeAsync runs and potentally calls back to remove the item from
                // the dictionary.
                ReferenceCountedDisposable<LazySolution>? solutionToDispose;

                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // All the TryAddReference calls below must succeed as we're called from a caller that is pinning
                    // solution anyways with it's own refcount of at least 1.

                    solutionToDispose = _lastRequestedSolution;

                    _lastRequestedChecksum = solutionChecksum;
                    _lastRequestedSolution = solution.TryAddReference() ?? throw ExceptionUtilities.Unreachable;
                }

                if (solutionToDispose != null)
                    await solutionToDispose.DisposeAsync().ConfigureAwait(false);
            }
        }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <summary>
        /// This type behaves similar to <see cref="T:Roslyn.Utilities.AsyncLazy{T}"/> (with <c>T</c> being <see
        /// cref="Solution"/>), except for the following unique characteristics:
        ///
        /// <list type="bullet">
        /// <item><description>This type will start the asynchronous computation in the constructor instead of waiting
        /// for the first call to <see cref="M:Roslyn.Utilities.AsyncLazy{T}.GetValueAsync(CancellationToken)"/>.</description></item>
        /// <item><description>This type can be disposed asynchronously to cancel the inner operation and wait for the
        /// inner operation to complete cancellation processing (similar to <see
        /// cref="TaskContinuationOptions.LazyCancellation"/>). Since <see cref="T:Roslyn.Utilities.AsyncLazy{T}"/> does
        /// not directly expose the inner computation, it does not support lazy cancellation
        /// scenarios.</description></item>
        /// </list>
        /// </summary>
        private sealed class LazySolution : IAsyncDisposable, IDisposable
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new();
            private readonly Func<Task> _cleanupAsync;
            private readonly Task<Solution> _task;

            public LazySolution(Func<CancellationToken, Task<Solution>> getSolutionAsync, Func<Task> cleanupAsync)
            {
                _cleanupAsync = cleanupAsync;
                _task = getSolutionAsync(_cancellationTokenSource.Token);
            }

            public Task<Solution> Task => _task;

            void IDisposable.Dispose()
            {
                // This type is only used as IAsyncDisposable, but needs to implement IDisposable for wrapping within
                // ReferenceCountedDisposable<T>.
                throw ExceptionUtilities.Unreachable;
            }

            public async ValueTask DisposeAsync()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();

                await _cleanupAsync().ConfigureAwait(false);

                // Make sure to wait for the underlying asynchronous operation to complete before returning. Use a
                // no-throw awaitable to avoid throwing an exception on cancellation or error (the inner operation is
                // responsible for its own error reporting, if any).
                await _task.NoThrowAwaitable(false);
            }
        }
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    }
}
