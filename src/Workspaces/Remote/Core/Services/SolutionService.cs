// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly RemoteWorkspace s_primaryWorkspace = new RemoteWorkspace();

        private readonly AssetService _assetService;

        // this simple cache hold onto the last and primary solution created
        private volatile static Tuple<Checksum, Solution> s_primarySolution;
        private volatile static Tuple<Checksum, Solution> s_lastSolution;

        public SolutionService(AssetService assetService)
        {
            _assetService = assetService;
        }

        public Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // this method is called by users which means we don't know whether the solution is from primary branch or not.
            // so we will be conservative and assume it is not. meaning it won't update any internal caches but only consume cache if possible.
            return GetSolutionInternalAsync(solutionChecksum, fromPrimaryBranch: false, cancellationToken: cancellationToken);
        }

        private async Task<Solution> GetSolutionInternalAsync(Checksum solutionChecksum, bool fromPrimaryBranch, CancellationToken cancellationToken)
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

                var solution = await CreateSolution_NoLockAsync(solutionChecksum, fromPrimaryBranch, s_primaryWorkspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
                s_lastSolution = Tuple.Create(solutionChecksum, solution);

                return solution;
            }
        }

        private async Task<Solution> CreateSolution_NoLockAsync(Checksum solutionChecksum, bool fromPrimaryBranch, Solution baseSolution, CancellationToken cancellationToken)
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
                    s_primaryWorkspace.UpdateSolution(solution);
                    return s_primaryWorkspace.CurrentSolution;
                }

                // otherwise, just return the solution
                return solution;
            }

            // we need new solution. bulk sync all asset for the solution first.
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // get new solution info
            var solutionInfo = await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false);

            if (fromPrimaryBranch)
            {
                // if the solutionChecksum is for primary branch, update primary workspace cache with new solution
                s_primaryWorkspace.ClearSolution();
                s_primaryWorkspace.AddSolution(solutionInfo);

                return s_primaryWorkspace.CurrentSolution;
            }

            // otherwise, just return new solution
            var workspace = new TemporaryWorkspace(await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false));
            return workspace.CurrentSolution;
        }

        async Task<Solution> ISolutionController.GetSolutionAsync(Checksum solutionChecksum, bool primary, CancellationToken cancellationToken)
        {
            return await GetSolutionInternalAsync(solutionChecksum, primary, cancellationToken).ConfigureAwait(false);
        }

        async Task ISolutionController.UpdatePrimaryWorkspaceAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var currentSolution = s_primaryWorkspace.CurrentSolution;

            var primarySolutionChecksum = await currentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (primarySolutionChecksum == solutionChecksum)
            {
                return;
            }

            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var primary = true;
                var solution = await CreateSolution_NoLockAsync(solutionChecksum, primary, currentSolution, cancellationToken).ConfigureAwait(false);
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
