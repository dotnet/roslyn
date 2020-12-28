﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        private readonly ISolutionCrawlerRegistrationService? _registrationService;

        /// <summary>
        /// Guards updates to <see cref="_primaryBranchSolutionWithChecksum"/> and <see cref="_lastRequestedSolutionWithChecksum"/>.
        /// </summary>
        private readonly SemaphoreSlim _availableSolutionsGate = new SemaphoreSlim(initialCount: 1);

        /// <summary>
        /// The last solution for the the primary branch fetched from the client.
        /// </summary>
        private volatile Tuple<Checksum, Solution>? _primaryBranchSolutionWithChecksum;

        /// <summary>
        /// The last solution requested by a service.
        /// </summary>
        private volatile Tuple<Checksum, Solution>? _lastRequestedSolutionWithChecksum;

        /// <summary>
        /// Guards setting current workspace solution.
        /// </summary>
        private readonly object _currentSolutionGate = new object();

        /// <summary>
        /// Used to make sure we never move remote workspace backward.
        /// this version is the WorkspaceVersion of primary solution in client (VS) we are
        /// currently caching.
        /// </summary>
        private int _currentRemoteWorkspaceVersion = -1;

        // internal for testing purposes.
        internal RemoteWorkspace(HostServices hostServices, string? workspaceKind)
            : base(hostServices, workspaceKind)
        {
            var exportProvider = (IMefHostExportProvider)Services.HostServices;
            RegisterDocumentOptionProviders(exportProvider.GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>());

            SetOptions(Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0));

            _registrationService = Services.GetService<ISolutionCrawlerRegistrationService>();
            _registrationService?.Register(this);
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);

            _registrationService?.Unregister(this);
        }

        public AssetProvider CreateAssetProvider(PinnedSolutionInfo solutionInfo, SolutionAssetCache assetCache, IAssetSource assetSource)
        {
            var serializerService = Services.GetRequiredService<ISerializerService>();
            return new AssetProvider(solutionInfo.ScopeId, assetCache, assetSource, serializerService);
        }

        public async Task UpdatePrimaryBranchSolutionAsync(AssetProvider assetProvider, Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            var currentSolution = CurrentSolution;

            var currentSolutionChecksum = await currentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (currentSolutionChecksum == solutionChecksum)
            {
                return;
            }

            using (await _availableSolutionsGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var solution = await CreateSolution_NoLockAsync(assetProvider, solutionChecksum, fromPrimaryBranch: true, workspaceVersion, currentSolution, cancellationToken).ConfigureAwait(false);
                _primaryBranchSolutionWithChecksum = Tuple.Create(solutionChecksum, solution);
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
        private async Task<Solution> CreateSolution_NoLockAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            bool fromPrimaryBranch,
            int workspaceVersion,
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

                    if (fromPrimaryBranch)
                    {
                        // if the solutionChecksum is for primary branch, update primary workspace cache with the solution
                        return UpdateSolutionIfPossible(solution, workspaceVersion);
                    }

                    // otherwise, just return the solution
                    return solution;
                }

                // we need new solution. bulk sync all asset for the solution first.
                await assetProvider.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // get new solution info and options
                var (solutionInfo, options) = await assetProvider.CreateSolutionInfoAndOptionsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

                if (fromPrimaryBranch)
                {
                    // if the solutionChecksum is for primary branch, update primary workspace cache with new solution
                    if (TrySetCurrentSolution(solutionInfo, workspaceVersion, options, out var solution))
                    {
                        return solution;
                    }
                }

                // otherwise, just return new solution
                var workspace = new TemporaryWorkspace(Services.HostServices, WorkspaceKind.RemoteTemporaryWorkspace, solutionInfo, options);
                return workspace.CurrentSolution;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private Solution? TryGetAvailableSolution(Checksum solutionChecksum)
        {
            var currentSolution = _primaryBranchSolutionWithChecksum;
            if (currentSolution?.Item1 == solutionChecksum)
            {
                // asked about primary solution
                return currentSolution.Item2;
            }

            var lastSolution = _lastRequestedSolutionWithChecksum;
            if (lastSolution?.Item1 == solutionChecksum)
            {
                // asked about last solution
                return lastSolution.Item2;
            }

            return null;
        }

        public async ValueTask<Solution> GetSolutionAsync(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            bool fromPrimaryBranch,
            int workspaceVersion,
            CancellationToken cancellationToken)
        {
            var availableSolution = TryGetAvailableSolution(solutionChecksum);
            if (availableSolution != null)
            {
                return availableSolution;
            }

            // make sure there is always only one that creates a new solution
            using (await _availableSolutionsGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                availableSolution = TryGetAvailableSolution(solutionChecksum);
                if (availableSolution != null)
                {
                    return availableSolution;
                }

                var solution = await CreateSolution_NoLockAsync(
                    assetProvider,
                    solutionChecksum,
                    fromPrimaryBranch,
                    workspaceVersion,
                    CurrentSolution,
                    cancellationToken).ConfigureAwait(false);

                _lastRequestedSolutionWithChecksum = Tuple.Create(solutionChecksum, solution);

                return solution;
            }
        }

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.
        /// </summary>
        internal bool TrySetCurrentSolution(SolutionInfo solutionInfo, int workspaceVersion, SerializableOptionSet options, [NotNullWhen(true)] out Solution? solution)
        {
            lock (_currentSolutionGate)
            {
                if (workspaceVersion <= _currentRemoteWorkspaceVersion)
                {
                    // we never move workspace backward
                    solution = null;
                    return false;
                }

                // set initial solution version
                _currentRemoteWorkspaceVersion = workspaceVersion;

                // clear previous solution data if there is one
                // it is required by OnSolutionAdded
                ClearSolutionData();

                OnSolutionAdded(solutionInfo);

                SetOptions(options);

                solution = CurrentSolution;
                return true;
            }
        }

        /// <summary>
        /// update primary solution
        /// </summary>
        internal Solution UpdateSolutionIfPossible(Solution solution, int workspaceVersion)
        {
            lock (_currentSolutionGate)
            {
                if (workspaceVersion <= _currentRemoteWorkspaceVersion)
                {
                    // we never move workspace backward
                    return solution;
                }

                // move version forward
                _currentRemoteWorkspaceVersion = workspaceVersion;

                var oldSolution = CurrentSolution;
                Contract.ThrowIfFalse(oldSolution.Id == solution.Id && oldSolution.FilePath == solution.FilePath);

                var newSolution = SetCurrentSolution(solution);
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

                SetOptions(newSolution.Options);

                return this.CurrentSolution;
            }
        }
    }
}
