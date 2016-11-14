// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
        public const string WorkspaceKind_RemoteWorkspace = "RemoteWorkspace";

        private static readonly RemoteWorkspace s_primaryWorkspace = new RemoteWorkspace();

        // TODO: make this simple cache better
        // this simple cache hold onto the last solution created
        private static readonly SemaphoreSlim s_gate = new SemaphoreSlim(initialCount: 1);
        private static ValueTuple<Checksum, Solution> s_lastSolution;

        private readonly AssetService _assetService;

        public SolutionService(AssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            if (s_lastSolution.Item1 == solutionChecksum)
            {
                return s_lastSolution.Item2;
            }

            // make sure there is always only one that creates a new solution
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (s_lastSolution.Item1 == solutionChecksum)
                {
                    return s_lastSolution.Item2;
                }

                var solution = await CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
                s_lastSolution = ValueTuple.Create(solutionChecksum, solution);

                return solution;
            }
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // since option belong to workspace, we can't share solution

            // create new solution
            var solution = await CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // set merged options
            solution.Workspace.Options = MergeOptions(solution.Workspace.Options, optionSet);

            // return new solution
            return solution;
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

        private async Task<Solution> CreateSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // synchronize whole solution first
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            var workspace = CreateRemoteWorkspace();

            var updater = new SolutionCreator(_assetService, s_primaryWorkspace.CurrentSolution, cancellationToken);
            var solutionInfo = await updater.CreateSolutionInfoAsync(solutionChecksum).ConfigureAwait(false);
            return workspace.AddSolution(solutionInfo);
        }

        private static AdhocWorkspace CreateRemoteWorkspace()
        {
            var workspace = new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: WorkspaceKind_RemoteWorkspace);
            workspace.Options = workspace.Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);

            return workspace;
        }
    }
}
