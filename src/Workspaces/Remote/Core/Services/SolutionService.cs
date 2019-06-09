// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide solution from given checksum
    /// 
    /// TODO: change this to workspace service
    /// </summary>
    internal class SolutionService : ISolutionController
    {
        private static readonly SemaphoreSlim s_gate = new SemaphoreSlim(initialCount: 1);

        private readonly AssetService _assetService;

        // this simple cache hold onto the last and primary solution created
        private volatile static Tuple<Checksum, Solution> s_primarySolution;
        private volatile static Tuple<Checksum, Solution> s_lastSolution;

        public SolutionService(AssetService assetService)
        {
            _assetService = assetService;
        }

        public static RemoteWorkspace PrimaryWorkspace
        {
            get
            {
                var exportProvider = (IMefHostExportProvider)RoslynServices.HostServices;
                var primaryWorkspace = exportProvider.GetExports<PrimaryWorkspace>().Single().Value;
                if (primaryWorkspace.Workspace == null)
                {
                    // The Roslyn OOP service assumes a singleton workspace exists, but doesn't initialize it anywhere.
                    // If we get here, code is asking for a workspace before it exists, so we create one on the fly.
                    // The RemoteWorkspace constructor assigns itself as the new singleton instance.
                    new RemoteWorkspace();
                }

                return (RemoteWorkspace)primaryWorkspace.Workspace;
            }
        }

        public Task<SolutionInfo> GetSolutionInfoAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            return SolutionInfoCreator.CreateSolutionInfoAsync(_assetService, solutionChecksum, cancellationToken);
        }

        public Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // this method is called by users which means we don't know whether the solution is from primary branch or not.
            // so we will be conservative and assume it is not. meaning it won't update any internal caches but only consume cache if possible.
            return GetSolutionInternalAsync(solutionChecksum, fromPrimaryBranch: false, workspaceVersion: -1, cancellationToken: cancellationToken);
        }

        private async Task<Solution> GetSolutionInternalAsync(
            Checksum solutionChecksum,
            bool fromPrimaryBranch,
            int workspaceVersion,
            CancellationToken cancellationToken)
        {
            var currentSolution = GetAvailableSolution(solutionChecksum);
            if (currentSolution != null)
            {
                return currentSolution;
            }

            // make sure there is always only one that creates a new solution
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                currentSolution = GetAvailableSolution(solutionChecksum);
                if (currentSolution != null)
                {
                    return currentSolution;
                }

                var solution = await CreateSolution_NoLockAsync(
                    solutionChecksum,
                    fromPrimaryBranch,
                    workspaceVersion,
                    PrimaryWorkspace.CurrentSolution,
                    cancellationToken).ConfigureAwait(false);
                s_lastSolution = Tuple.Create(solutionChecksum, solution);

                return solution;
            }
        }

        /// <summary>
        /// SolutionService is designed to be stateless. if someone asks a solution (through solution checksum), 
        /// it will create one and return the solution. the engine takes care of synching required data and creating a solution
        /// correspoing to the given checksum.
        /// 
        /// but doing that from scratch all the time wil be expansive in terms of synching data, compilation being cached, file being parsed
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
        private async Task<Solution> CreateSolution_NoLockAsync(
            Checksum solutionChecksum,
            bool fromPrimaryBranch,
            int workspaceVersion,
            Solution baseSolution,
            CancellationToken cancellationToken)
        {
            var updater = new SolutionCreator(_assetService, baseSolution, cancellationToken);

            // check whether solution is update to the given base solution
            if (await updater.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
            {
                // create updated solution off the baseSolution
                var solution = await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);

                if (fromPrimaryBranch)
                {
                    // if the solutionChecksum is for primary branch, update primary workspace cache with the solution
                    return PrimaryWorkspace.UpdateSolutionIfPossible(solution, workspaceVersion);
                }

                // otherwise, just return the solution
                return solution;
            }

            // we need new solution. bulk sync all asset for the solution first.
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // get new solution info
            var solutionInfo = await GetSolutionInfoAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            if (fromPrimaryBranch)
            {
                // if the solutionChecksum is for primary branch, update primary workspace cache with new solution
                if (PrimaryWorkspace.TryAddSolutionIfPossible(solutionInfo, workspaceVersion, out var solution))
                {
                    return solution;
                }
            }

            // otherwise, just return new solution
            var workspace = new TemporaryWorkspace(solutionInfo);
            return workspace.CurrentSolution;
        }

        Task<Solution> ISolutionController.GetSolutionAsync(Checksum solutionChecksum, bool primary, int workspaceVersion, CancellationToken cancellationToken)
        {
            return GetSolutionInternalAsync(solutionChecksum, primary, workspaceVersion, cancellationToken);
        }

        async Task ISolutionController.UpdatePrimaryWorkspaceAsync(Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            var currentSolution = PrimaryWorkspace.CurrentSolution;

            var primarySolutionChecksum = await currentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (primarySolutionChecksum == solutionChecksum)
            {
                return;
            }

            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var primary = true;
                var solution = await CreateSolution_NoLockAsync(solutionChecksum, primary, workspaceVersion, currentSolution, cancellationToken).ConfigureAwait(false);
                s_primarySolution = Tuple.Create(solutionChecksum, solution);
            }
        }

        private static Solution GetAvailableSolution(Checksum solutionChecksum)
        {
            var currentSolution = s_primarySolution;
            if (currentSolution?.Item1 == solutionChecksum)
            {
                // asked about primary solution
                return currentSolution.Item2;
            }

            var lastSolution = s_lastSolution;
            if (lastSolution?.Item1 == solutionChecksum)
            {
                // asked about last solution
                return lastSolution.Item2;
            }

            return null;
        }
    }
}
