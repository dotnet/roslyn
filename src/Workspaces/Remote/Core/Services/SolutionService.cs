// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
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

        // TODO: make this simple cache better
        // this simple cache hold onto the last solution created
        private volatile static Tuple<Checksum, Solution> s_lastSolution;

        public SolutionService(AssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var currentSolution = s_primaryWorkspace.CurrentSolution;

            var primarySolutionChecksum = await currentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (primarySolutionChecksum == solutionChecksum)
            {
                // nothing changed
                return currentSolution;
            }

            if (s_lastSolution?.Item1 == solutionChecksum)
            {
                return s_lastSolution.Item2;
            }

            // make sure there is always only one that creates a new solution
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (s_lastSolution?.Item1 == solutionChecksum)
                {
                    return s_lastSolution.Item2;
                }

                var solution = await CreateSolution_NoLockAsync(solutionChecksum, currentSolution, cancellationToken).ConfigureAwait(false);
                s_lastSolution = Tuple.Create(solutionChecksum, solution);

                return solution;
            }
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // get solution
            var baseSolution = await GetSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // since options belong to workspace, we can't share solution
            // create temporary workspace
            var tempWorkspace = new TemporaryWorkspace(baseSolution);

            // set merged options
            tempWorkspace.Options = MergeOptions(tempWorkspace.Options, optionSet);

            // return new solution
            return tempWorkspace.CurrentSolution;
        }

        public async Task UpdatePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            var currentSolution = s_primaryWorkspace.CurrentSolution;

            var primarySolutionChecksum = await currentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (primarySolutionChecksum == checksum)
            {
                // nothing changed
                return;
            }

            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var updater = new SolutionCreator(_assetService, currentSolution, cancellationToken);

                if (await updater.IsIncrementalUpdateAsync(checksum).ConfigureAwait(false))
                {
                    // solution has updated
                    s_primaryWorkspace.UpdateSolution(await updater.CreateSolutionAsync(checksum).ConfigureAwait(false));
                    return;
                }

                // new solution. bulk sync all asset for the solution
                await _assetService.SynchronizeSolutionAssetsAsync(checksum, cancellationToken).ConfigureAwait(false);

                s_primaryWorkspace.ClearSolution();
                s_primaryWorkspace.AddSolution(await updater.CreateSolutionInfoAsync(checksum).ConfigureAwait(false));
            }
        }

        private OptionSet MergeOptions(OptionSet workspaceOptions, OptionSet userOptions)
        {
            var newOptions = workspaceOptions;
            foreach (var key in userOptions.GetChangedOptions(workspaceOptions))
            {
                newOptions = newOptions.WithChangedOption(key, userOptions.GetOption(key));
            }

            return newOptions;
        }

        private async Task<Solution> CreateSolution_NoLockAsync(Checksum solutionChecksum, Solution baseSolution, CancellationToken cancellationToken)
        {
            var updater = new SolutionCreator(_assetService, baseSolution, cancellationToken);

            if (await updater.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
            {
                // solution has updated
                return await updater.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);
            }

            // new solution. bulk sync all asset for the solution
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            var workspace = new TemporaryWorkspace(await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false));
            return workspace.CurrentSolution;
        }
    }
}
