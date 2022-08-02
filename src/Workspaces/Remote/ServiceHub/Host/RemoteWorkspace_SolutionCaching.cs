// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteWorkspace
    {
        /// <summary>
        /// The last solution for the primary branch fetched from the client.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedPrimaryBranchSolution;

        /// <summary>
        /// The last solution requested by a service.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedAnyBranchSolution;

        /// <summary>
        /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a
        /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
        /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.
        /// </summary>
        private readonly Dictionary<Checksum, InFlightSolution> _solutionChecksumToSolution = new();

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
                solution.TryKickOffPrimaryBranchWork_NoLock(async (disconnectedSolution, cancellationToken) =>
                {
                    // compute the primary workspace, and then cache it as well to help with future requests.
                    var result = await this.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, disconnectedSolution, cancellationToken).ConfigureAwait(false);
                    await SetLastRequestedSolutionAsync(result, primary: true, cancellationToken).ConfigureAwait(false);
                    return result;
                });
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

                // See if we're being asked for a checksum we already have cached a solution for.
                var cachedSolution =
                    _lastRequestedPrimaryBranchSolution.checksum == solutionChecksum ? _lastRequestedPrimaryBranchSolution.solution :
                    _lastRequestedAnyBranchSolution.checksum == solutionChecksum ? _lastRequestedAnyBranchSolution.solution : null;

                // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                // in-flight-count of 1 to represent our caller. 
                solution = new InFlightSolution(
                    this, solutionChecksum,
                    async cancellationToken =>
                    {
                        // Compute the solution (or use one we already cached) then cache it to help with future requests.
                        var result = cachedSolution ?? await ComputeDisconnectedSolutionAsync(assetProvider, solutionChecksum, cancellationToken).ConfigureAwait(false);
                        await SetLastRequestedSolutionAsync(result, primary: false, cancellationToken).ConfigureAwait(false);
                        return result;
                    });
                Contract.ThrowIfFalse(solution.InFlightCount == 1);

                _solutionChecksumToSolution.Add(solutionChecksum, solution);

                return solution;
            }

            async Task SetLastRequestedSolutionAsync(Solution solution, bool primary, CancellationToken cancellationToken)
            {
                // it's fine if we end up not setting this due to cancellation.  this is just a helper cache to speed up
                // future requests.
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (primary)
                        _lastRequestedPrimaryBranchSolution = (solutionChecksum, solution);
                    else
                        _lastRequestedAnyBranchSolution = (solutionChecksum, solution);
                }
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
