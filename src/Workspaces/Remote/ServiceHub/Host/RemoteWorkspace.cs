// Licensed to the .NET Foundation under one or more agreements.
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
        private (Checksum checksum, Solution solution) _primaryBranchSolutionWithChecksum;

        /// <summary>
        /// The last solution requested by a service.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedSolutionWithChecksum;

        /// <summary>
        /// Used to make sure we never move remote workspace backward.
        /// this version is the WorkspaceVersion of primary solution in client (VS) we are
        /// currently caching.
        /// </summary>
        private int _currentRemoteWorkspaceVersion = -1;

        /// <summary>
        /// Mapping from solution checksum to to the solution computed for it.  This is used so that we can hold a
        /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same checksum can
        /// share the computation of that particular solution and avoid duplicated concurrent work.
        /// </summary>
        private readonly Dictionary<Checksum, (int refCount, AsyncLazy<Solution> lazySolution)> _checksumToRefCountAndLazySolution = new();

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
        /// cref="_lastRequestedSolutionWithChecksum"/> and <see cref="_primaryBranchSolutionWithChecksum"/>, allowing
        /// them to be pre-populated for feature requests that come in soon after this call completes.
        /// </summary>
        public async Task UpdatePrimaryBranchSolutionAsync(
            AssetProvider assetProvider, Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            var baseSolution = CurrentSolution;

            // See if the current snapshot we're pointing at is the same one the host wants us to sync to.  If so, we
            // don't need to do anything.
            var currentSolutionChecksum = await baseSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (currentSolutionChecksum == solutionChecksum)
                return;

            // Do a no-op run.  This will still ensure that we compute and cache this checksum/solution pair for future callers.
            await RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                workspaceVersion,
                fromPrimaryBranch: true,
                baseSolution,
                static _ => ValueTaskFactory.FromResult(false),
                cancellationToken).ConfigureAwait(false);
        }

        public ValueTask<(Solution solution, T result)> RunWithSolutionAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool fromPrimaryBranch,
            Func<Solution, ValueTask<T>> doWorkAsync,
            CancellationToken cancellationToken)
        {
            return RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                workspaceVersion,
                fromPrimaryBranch,
                baseSolution: this.CurrentSolution,
                doWorkAsync,
                cancellationToken);
        }

        private async ValueTask<(Solution solution, T result)> RunWithSolutionAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool fromPrimaryBranch,
            Solution baseSolution,
            Func<Solution, ValueTask<T>> doWorkAsync,
            CancellationToken cancellationToken)
        {
            // Fast path if this solution checksum is for a solution we're already caching. This also avoids us then
            // trying to actually mutate the workspace for the simple case of asking for the same thing the last calls
            // asked about.
            var tuple = await TryFastGetSolutionAndRunAsync().ConfigureAwait(false);
            if (tuple.solution != null)
                return (tuple.solution, tuple.result);

            // Wasn't the same as the last thing we cached, actually get the corresponding solution and run the
            // requested callback against it.
            return await TrySlowGetSolutionAndRunAsync(
                assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, baseSolution, doWorkAsync, cancellationToken).ConfigureAwait(false);

            async ValueTask<(Solution? solution, T result)> TryFastGetSolutionAndRunAsync()
            {
                Solution solution;
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_primaryBranchSolutionWithChecksum.checksum == solutionChecksum)
                    {
                        solution = _primaryBranchSolutionWithChecksum.solution;
                    }
                    else if (_lastRequestedSolutionWithChecksum.checksum == solutionChecksum)
                    {
                        solution = _lastRequestedSolutionWithChecksum.solution;
                    }
                    else
                    {
                        return default;
                    }
                }

                var result = await doWorkAsync(solution).ConfigureAwait(false);
                return (solution, result);
            }
        }

        private async ValueTask<(Solution solution, T result)> TrySlowGetSolutionAndRunAsync<T>(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool fromPrimaryBranch,
            Solution baseSolution,
            Func<Solution, ValueTask<T>> doWorkAsync,
            CancellationToken cancellationToken)
        {
            // See if anyone else is computing this solution for this checksum.  If so, just piggy-back on that.  No
            // need for us to force the same computation to happen ourselves.
            var lazySolution = await GetLazySolutionAsync().ConfigureAwait(false);

            // Actually get the solution, computing it ourselves, or getting the result that another caller was computing.
            var solution = await lazySolution.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // Store this around so that if another call comes through, they will see the solution we just computed.
            await SetLastRequestedSolutionAsync(solution).ConfigureAwait(false);

            // Now, pass it to the callback to do the work.  Any other callers into us will be able to benefit from
            // using this same solution as well
            var result = await doWorkAsync(solution).ConfigureAwait(false);

            // Now that we're done, update the refcounts for this lazy solution, removing it if the refcount goes back
            // to zero.
            await UpdateLazySolutionRefcountAsync().ConfigureAwait(false);

            return (solution, result);

            async ValueTask<AsyncLazy<Solution>> GetLazySolutionAsync()
            {
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
                        tuple = (refCount: 1, new AsyncLazy<Solution>(
                            c => ComputeSolutionAsync(assetProvider, solutionChecksum, workspaceVersion, fromPrimaryBranch, baseSolution, c), cacheResult: true));
                        _checksumToRefCountAndLazySolution.Add(solutionChecksum, tuple);
                    }

                    return tuple.lazySolution;
                }
            }

            async ValueTask SetLastRequestedSolutionAsync(Solution solution)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Quick caches of the last solutions we computed.  That way if return all the way out and something
                    // else calls back in, we have a likely chance of a cache hit.
                    _lastRequestedSolutionWithChecksum = (solutionChecksum, solution);
                    if (fromPrimaryBranch)
                        _primaryBranchSolutionWithChecksum = (solutionChecksum, solution);
                }
            }

            async ValueTask UpdateLazySolutionRefcountAsync()
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    var tuple = _checksumToRefCountAndLazySolution[solutionChecksum];
                    tuple.refCount--;
                    Contract.ThrowIfTrue(tuple.refCount < 0);
                    if (tuple.refCount == 0)
                    {
                        // last computation of this solution went away.  Remove from in flight cache.
                        _checksumToRefCountAndLazySolution.Remove(solutionChecksum);
                    }
                    else
                    {
                        // otherwise, update with our decremented refcount.
                        _checksumToRefCountAndLazySolution[solutionChecksum] = tuple;
                    }
                }
            }
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
            Solution baseSolution,
            CancellationToken cancellationToken)
        {
            try
            {
                var updater = new SolutionCreator(Services.HostServices, assetProvider, baseSolution, cancellationToken);

                // check whether solution is update to the given base solution
                if (await updater.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
                {
                    // create updated solution off the baseSolution
                    var solution = await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);

                    // if the solutionChecksum is for primary branch, update primary workspace cache with the solution
                    return await TryUpdateAndReturnPrimarySolutionAsync(
                        workspaceVersion, fromPrimaryBranch, solution, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // we need new solution. bulk sync all asset for the solution first.
                    await assetProvider.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                    // get new solution info and options
                    var (solutionInfo, options) = await assetProvider.CreateSolutionInfoAndOptionsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                    // if the solutionChecksum is for primary branch, update primary workspace cache with new solution
                    var solution = await TrySetCurrentSolutionAsync(
                        solutionInfo, workspaceVersion, fromPrimaryBranch, options, cancellationToken).ConfigureAwait(false);
                    if (solution != null)
                        return solution;

                    // otherwise, just return new solution
                    var workspace = new TemporaryWorkspace(Services.HostServices, WorkspaceKind.RemoteTemporaryWorkspace, solutionInfo, options);
                    return workspace.CurrentSolution;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Update if for primary solution and for version after what we've already stored in <see cref="_currentRemoteWorkspaceVersion"/>.
        /// </summary>
        internal async ValueTask<Solution> TryUpdateAndReturnPrimarySolutionAsync(
            int workspaceVersion,
            bool fromPrimaryBranch,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // if this wasn't from the primary branch, then we have nothing to do.  Just return the solution back for
            // the caller.
            if (fromPrimaryBranch)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // we never move workspace backward
                    if (workspaceVersion > _currentRemoteWorkspaceVersion)
                    {
                        _currentRemoteWorkspaceVersion = workspaceVersion;

                        var oldSolution = CurrentSolution;

                        var newSolution = SetCurrentSolution(solution);
                        _ = this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

                        SetOptions(newSolution.Options);

                        // Since we did successfully change the solution, return the actual final solution the workspace
                        // recorded (which will contain things like the real workspace version associated with this
                        // solution instance).
                        return this.CurrentSolution;
                    }
                }
            }

            return solution;
        }

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.  only do this for primary
        /// solution and for version after what we've already stored in <see cref="_currentRemoteWorkspaceVersion"/>. 
        /// </summary>
        internal async ValueTask<Solution?> TrySetCurrentSolutionAsync(
            SolutionInfo solutionInfo, int workspaceVersion, bool fromPrimaryBranch, SerializableOptionSet options, CancellationToken cancellationToken)
        {
            if (fromPrimaryBranch)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // we never move workspace backward
                    if (workspaceVersion > _currentRemoteWorkspaceVersion)
                    {
                        _currentRemoteWorkspaceVersion = workspaceVersion;

                        // clear previous solution data if there is one it is required by OnSolutionAdded
                        ClearSolutionData();

                        OnSolutionAdded(solutionInfo);

                        // The call to SetOptions will ensure that the options get pushed into the remote IOptionService
                        // store.  However, we still update our current solution with the options passed in.  This is
                        // due to the fact that the option store will ignore any options it considered unchanged to what
                        // it currently knows about.  This will prevent it from actually going and writing those
                        // unchanged values into Solution.Options.  This is not a correctness issue, but it impacts how
                        // checksums and syncing work in oop.  Currently, the checksum is based off Solution.Options and
                        // the values loaded into it.  If one side has loaded a default value and the other has not,
                        // then they will disagree on their checksum.  This ensures the remote side agrees with the
                        // host.
                        //
                        // A better fix in the future is to make all options pure data and remove the general concept of
                        // any part of the system eliding information about any options that have their 'default' value.
                        // https://github.com/dotnet/roslyn/issues/55728
                        this.SetCurrentSolution(this.CurrentSolution.WithOptions(options));
                        SetOptions(options);

                        return CurrentSolution;
                    }
                }
            }

            return null;
        }
    }
}
