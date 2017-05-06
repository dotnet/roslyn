// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide solution from given checksum
    /// 
    /// TODO: change this to workspace service
    /// </summary>
    internal class SolutionService
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
            return GetSolutionAsync(solutionChecksum, primary: false, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This should be only consumed by engine, not by user.
        /// </summary>
        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, bool primary, CancellationToken cancellationToken)
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

                var beforeCreate = DateTime.Now;
                var solution = await CreateSolution_NoLockAsync(solutionChecksum, primary, s_primaryWorkspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
                var afterCreate = DateTime.Now;

                s_lastSolution = Tuple.Create(solutionChecksum, solution);
                AbstractNavigateToSearchService.Log("Solution create time: " + (afterCreate - beforeCreate));
                return solution;
            }
        }

        /// <summary>
        /// This should be only consumed by engine, not by user.
        /// </summary>
        public async Task UpdatePrimaryWorkspaceAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
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

        private async Task<Solution> CreateSolution_NoLockAsync(Checksum solutionChecksum, bool primary, Solution baseSolution, CancellationToken cancellationToken)
        {
            var updater = new SolutionCreator(_assetService, baseSolution, cancellationToken);

            if (await updater.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
            {
                // solution has updated
                if (primary)
                {
                    s_primaryWorkspace.UpdateSolution(await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false));
                    return s_primaryWorkspace.CurrentSolution;
                }

                return await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);
            }

            // new solution. bulk sync all asset for the solution
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            if (primary)
            {
                s_primaryWorkspace.ClearSolution();
                s_primaryWorkspace.AddSolution(await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false));

                return s_primaryWorkspace.CurrentSolution;
            }

            var workspace = new TemporaryWorkspace(await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false));
            return workspace.CurrentSolution;
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
