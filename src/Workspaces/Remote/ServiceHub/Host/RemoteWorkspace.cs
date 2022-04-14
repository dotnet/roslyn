﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

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
        /// The last solution for the primary branch fetched from the client.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedPrimaryBranchSolution;

        /// <summary>
        /// The last solution requested by a service.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedAnyBranchSolution;

        /// <summary>
        /// Used to make sure we never move remote workspace backward.
        /// this version is the WorkspaceVersion of primary solution in client (VS) we are
        /// currently caching.
        /// </summary>
        private int _currentRemoteWorkspaceVersion = -1;

#if SHARE_SOLUTIONS_ACROSS_CONCURRENT_CALLS
        /// <summary>
        /// Mapping from solution checksum to to the solution computed for it.  This is used so that we can hold a
        /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same checksum can
        /// share the computation of that particular solution and avoid duplicated concurrent work.
        /// </summary>
        private readonly Dictionary<Checksum, (int refCount, AsyncLazy<Solution> lazySolution)> _checksumToRefCountAndLazySolution = new();
#endif

        // internal for testing purposes.
        internal RemoteWorkspace(HostServices hostServices, string? workspaceKind)
            : base(hostServices, workspaceKind)
        {
            var exportProvider = (IMefHostExportProvider)Services.HostServices;
            RegisterDocumentOptionProviders(exportProvider.GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>());
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);
            Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Unregister(this);
        }

        public AssetProvider CreateAssetProvider(PinnedSolutionInfo solutionInfo, SolutionAssetCache assetCache, IAssetSource assetSource)
        {
            var serializerService = Services.GetRequiredService<ISerializerService>();
            return new AssetProvider(solutionInfo.ScopeId, assetCache, assetSource, serializerService);
        }

        /// <summary>
        /// Syncs over the solution corresponding to <paramref name="solutionChecksum"/> and sets it as the current
        /// solution for <see langword="this"/> workspace.  This will also end up setting <see
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

            // Do a no-op run.  This will still ensure that we compute and cache this checksum/solution pair for future
            // callers. note we call directly into SlowGetSolutionAndRunAsync (skipping TryFastGetSolutionAndRunAsync)
            // as we always want to cache the primary workspace we are being told about here.
            await SlowGetSolutionAndRunAsync(
                assetProvider,
                solutionChecksum,
                workspaceVersion,
                fromPrimaryBranch: true,
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
        public async ValueTask<(Solution solution, T result)> RunWithSolutionAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool fromPrimaryBranch,
            Func<Solution, ValueTask<T>> implementation,
            CancellationToken cancellationToken)
        {
            // Fast path if this solution checksum is for a solution we're already caching. This also avoids us then
            // trying to actually mutate the workspace for the simple case of asking for the same thing the last call
            // asked about.
            var (solution, result) = await TryFastGetSolutionAndRunAsync().ConfigureAwait(false);
            if (solution != null)
                return (solution, result);

            // Wasn't the same as the last thing we cached, actually get the corresponding solution and run the
            // requested callback against it.
            return await SlowGetSolutionAndRunAsync(
                assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, implementation, cancellationToken).ConfigureAwait(false);

            async ValueTask<(Solution? solution, T result)> TryFastGetSolutionAndRunAsync()
            {
                Solution solution;
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_lastRequestedPrimaryBranchSolution.checksum == solutionChecksum)
                    {
                        solution = _lastRequestedPrimaryBranchSolution.solution;
                    }
                    else if (_lastRequestedAnyBranchSolution.checksum == solutionChecksum)
                    {
                        solution = _lastRequestedAnyBranchSolution.solution;
                    }
                    else
                    {
                        return default;
                    }
                }

                var result = await implementation(solution).ConfigureAwait(false);
                return (solution, result);
            }
        }

        private async ValueTask<(Solution solution, T result)> SlowGetSolutionAndRunAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool fromPrimaryBranch,
            Func<Solution, ValueTask<T>> doWorkAsync,
            CancellationToken cancellationToken)
        {
            // See if anyone else is computing this solution for this checksum.  If so, just piggy-back on that.  No
            // need for us to force the same computation to happen ourselves.
            var lazySolution = await GetLazySolutionAndIncrementRefCountAsync().ConfigureAwait(false);

            try
            {
                // Actually get the solution, computing it ourselves, or getting the result that another caller was computing.
                var solution = await lazySolution.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // Store this around so that if another call comes through, they will see the solution we just computed.
                await SetLastRequestedSolutionAsync(solution).ConfigureAwait(false);

                // Now, pass it to the callback to do the work.  Any other callers into us will be able to benefit from
                // using this same solution as well
                var result = await doWorkAsync(solution).ConfigureAwait(false);

                return (solution, result);

            }
            finally
            {
                // Now that we're done, update the refcounts for this lazy solution, removing it if the refcount goes back
                // to zero.
                await DecrementLazySolutionRefcountAsync().ConfigureAwait(false);
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async ValueTask<AsyncLazy<Solution>> GetLazySolutionAndIncrementRefCountAsync()
            {
#if !SHARE_SOLUTIONS_ACROSS_CONCURRENT_CALLS
                return
                    AsyncLazy.Create(c => ComputeSolutionAsync(assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, c), cacheResult: true);
#else
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_checksumToRefCountAndLazySolution.TryGetValue(solutionChecksum, out var tuple))
                    {
                        // Some other call was getting this same solution.  Increase our ref count on that to mark that we
                        // care about that computation as well.
                        Contract.ThrowIfTrue(tuple.refCount <= 0);
                        tuple.refCount++;
                        _checksumToRefCountAndLazySolution[solutionChecksum] = tuple;
                    }
                    else
                    {
                        // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                        // refcount of 1 (for 'us').
                        tuple = (refCount: 1, AsyncLazy.Create(
                            c => ComputeSolutionAsync(assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, c), cacheResult: true));
                        _checksumToRefCountAndLazySolution.Add(solutionChecksum, tuple);
                    }

                    return tuple.lazySolution;
                }
#endif
            }

            async ValueTask SetLastRequestedSolutionAsync(Solution solution)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Quick caches of the last solutions we computed.  That way if return all the way out and something
                    // else calls back in, we have a likely chance of a cache hit.
                    _lastRequestedAnyBranchSolution = (solutionChecksum, solution);
                    if (fromPrimaryBranch)
                        _lastRequestedPrimaryBranchSolution = (solutionChecksum, solution);
                }
            }

            async ValueTask DecrementLazySolutionRefcountAsync()
            {
#if !SHARE_SOLUTIONS_ACROSS_CONCURRENT_CALLS
                return;
#else
                // We use CancellationToken.None here as we have to ensure the refcount is decremented, or else we will
                // have a memory leak.  This should hopefully not ever be an issue as we only ever hold this gate for
                // very short periods of time in order to set do basic operations on our state.
                using (await _gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    var (refCount, lazySolution) = _checksumToRefCountAndLazySolution[solutionChecksum];
                    refCount--;
                    Contract.ThrowIfTrue(refCount < 0);
                    if (refCount == 0)
                    {
                        // last computation of this solution went away.  Remove from in flight cache.
                        _checksumToRefCountAndLazySolution.Remove(solutionChecksum);
                    }
                    else
                    {
                        // otherwise, update with our decremented refcount.
                        _checksumToRefCountAndLazySolution[solutionChecksum] = (refCount, lazySolution);
                    }
                }
#endif
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
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
            int workspaceVersion,
            bool fromPrimaryBranch,
            CancellationToken cancellationToken)
        {
            try
            {
                var updater = new SolutionCreator(Services.HostServices, assetProvider, this.CurrentSolution, cancellationToken);

                // check whether solution is update to the given base solution
                Solution solution;
                if (await updater.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
                {
                    // create updated solution off the baseSolution
                    solution = await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);
                }
                else
                {
                    // we need new solution. bulk sync all asset for the solution first.
                    await assetProvider.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                    // get new solution info and options
                    var (solutionInfo, options) = await assetProvider.CreateSolutionInfoAndOptionsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                    solution = CreateSolutionFromInfoAndOptions(solutionInfo, options);
                }

                var (newSolution, _) = await TryUpdateWorkspaceAsync(
                    workspaceVersion, fromPrimaryBranch, solution, cancellationToken).ConfigureAwait(false);
                return newSolution;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private Solution CreateSolutionFromInfoAndOptions(SolutionInfo solutionInfo, SerializableOptionSet options)
        {
            // The call to SetOptions in TryUpdateWorkspaceAsync will ensure that the options get pushed into
            // the remote IOptionService store.  However, we still update our current solution with the options
            // passed in.  This is due to the fact that the option store will ignore any options it considered
            // unchanged to what it currently knows about.  This will prevent it from actually going and writing
            // those unchanged values into Solution.Options.  This is not a correctness issue, but it impacts
            // how checksums and syncing work in oop.  Currently, the checksum is based off Solution.Options and
            // the values loaded into it.  If one side has loaded a default value and the other has not, then
            // they will disagree on their checksum.  This ensures the remote side agrees with the host.
            //
            // A better fix in the future is to make all options pure data and remove the general concept of
            // any part of the system eliding information about any options that have their 'default' value.
            // https://github.com/dotnet/roslyn/issues/55728
            var solution = this.CreateSolution(solutionInfo).WithOptions(options);
            foreach (var projectInfo in solutionInfo.Projects)
                solution = solution.AddProject(projectInfo);
            return solution;
        }

        /// <summary>
        /// Attempts to update this workspace with the given <paramref name="newSolution"/>.  If this succeeds, <see
        /// langword="true"/> will be returned in the tuple result as well as the actual solution that the workspace is
        /// updated to point at.  If we cannot update this workspace, then <see langword="false"/> will be returned,
        /// along with the solution passed in.
        /// </summary>
        private async ValueTask<(Solution solution, bool updated)> TryUpdateWorkspaceAsync(
            int workspaceVersion,
            bool fromPrimaryBranch,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var oldSolution = this.CurrentSolution;

                // if this wasn't from the primary branch, then we have nothing to do.  Just return the solution back for
                // the caller.
                //
                // we never move workspace backward
                if (!fromPrimaryBranch || workspaceVersion <= _currentRemoteWorkspaceVersion)
                    return (newSolution, updated: false);

                _currentRemoteWorkspaceVersion = workspaceVersion;

                // if either solution id or file path changed, then we consider it as new solution. Otherwise,
                // update the current solution in place.

                var addingSolution = oldSolution.Id != newSolution.Id || oldSolution.FilePath != newSolution.FilePath;
                if (addingSolution)
                {
                    // We're not doing an update, we're moving to a new solution entirely.  Clear out the old one. This
                    // is necessary so that we clear out any open document information this workspace is tracking. Note:
                    // this seems suspect as the remote workspace should not be tracking any open document state.
                    this.ClearSolutionData();
                }

                newSolution = SetCurrentSolution(newSolution);
                SetOptions(newSolution.Options);
                _ = this.RaiseWorkspaceChangedEventAsync(
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

            public Solution CreateSolutionFromInfoAndOptions(SolutionInfo solutionInfo, SerializableOptionSet options)
                => _remoteWorkspace.CreateSolutionFromInfoAndOptions(solutionInfo, options);

            public ValueTask<(Solution solution, bool updated)> TryUpdateWorkspaceAsync(Solution newSolution, int workspaceVersion)
                => _remoteWorkspace.TryUpdateWorkspaceAsync(workspaceVersion, fromPrimaryBranch: true, newSolution, CancellationToken.None);

            public async ValueTask<Solution> GetSolutionAsync(
                AssetProvider assetProvider,
                Checksum solutionChecksum,
                bool fromPrimaryBranch,
                int workspaceVersion,
                CancellationToken cancellationToken)
            {
                var tuple = await _remoteWorkspace.RunWithSolutionAsync(
                    assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, _ => ValueTaskFactory.FromResult(false), cancellationToken).ConfigureAwait(false);
                return tuple.solution;
            }
        }
    }
}
