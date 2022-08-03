// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteWorkspace
    {
        /// <summary>
        /// The last solution for the primary branch fetched from the client.  Cached as it's very common to have a
        /// flurry of requests for the same checksum that don't run concurrently.  Only read/write while holding <see
        /// cref="_gate"/>.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedPrimaryBranchSolution;

        /// <summary>
        /// The last solution requested by a service.  Cached as it's very common to have a flurry of requests for the
        /// same checksum that don't run concurrently.  Only read/write while holding <see cref="_gate"/>.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedAnyBranchSolution;

        /// <summary>
        /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a solution
        /// around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
        /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.  Only
        /// read/write while holding <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<Checksum, InFlightSolution> _solutionChecksumToSolution = new();

        /// <summary>
        /// Deliberately not cancellable.  This code must always run fully to completion.
        /// </summary>
        private InFlightSolution GetOrCreateSolutionAndAddInFlightCount_NoLock(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool updatePrimaryBranch)
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

            CheckCacheInvariants_NoLock();

            var solution = GetOrCreateSolutionAndAddInFlightCount_NoLock();

            // The solution must now have a valid in-flight-count.
            Contract.ThrowIfTrue(solution.InFlightCount < 1);

            // We may be getting back a solution that only was computing a non-primary branch.  If we were asked
            // to compute the primary branch as well, let it know so it can start that now.
            if (updatePrimaryBranch)
            {
                solution.TryKickOffPrimaryBranchWork_NoLock((disconnectedSolution, cancellationToken) =>
                    this.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, disconnectedSolution, cancellationToken));
            }

            CheckCacheInvariants_NoLock();

            return solution;

            InFlightSolution GetOrCreateSolutionAndAddInFlightCount_NoLock()
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var solution))
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(solution.InFlightCount < 1);

                    // Increase the count as our caller now is keeping this solution in-flight
                    solution.IncrementInFlightCount_NoLock();
                    Contract.ThrowIfTrue(solution.InFlightCount < 2);

                    return solution;
                }

                // See if we're being asked for a checksum we already have cached a solution for.  Safe to read directly
                // as we're holding _gate.
                var cachedSolution =
                    _lastRequestedPrimaryBranchSolution.checksum == solutionChecksum ? _lastRequestedPrimaryBranchSolution.solution :
                    _lastRequestedAnyBranchSolution.checksum == solutionChecksum ? _lastRequestedAnyBranchSolution.solution : null;

                // We're the first call that is asking about this checksum.  Kick off async computation to compute it
                // (or use an existing cached value we already have).  Start with an in-flight-count of 1 to represent
                // our caller. 
                solution = new InFlightSolution(
                    this, solutionChecksum,
                    async cancellationToken => cachedSolution ?? await ComputeDisconnectedSolutionAsync(assetProvider, solutionChecksum, cancellationToken).ConfigureAwait(false));
                Contract.ThrowIfFalse(solution.InFlightCount == 1);

                _solutionChecksumToSolution.Add(solutionChecksum, solution);

                return solution;
            }
        }

        private void CheckCacheInvariants_NoLock()
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

            foreach (var (solutionChecksum, solution) in _solutionChecksumToSolution)
            {
                // Anything in this dictionary is currently in flight with an existing request.  So it must have an
                // in-flight-count of at least 1.  Note: this in-flight-request may be an actual request that has come
                // in from the client.  Or it can be a virtual one we've created through _lastAnyBranchSolution or
                // _lastPrimaryBranchSolution
                Contract.ThrowIfTrue(solution.InFlightCount < 1);
                Contract.ThrowIfTrue(solutionChecksum != solution.SolutionChecksum);
            }
        }
    }
}
