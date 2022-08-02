﻿// Licensed to the .NET Foundation under one or more agreements.
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
        /// The last solution requested by a service. This effectively adds an additional in-flight-count to one of
        /// the items in <see cref="_solutionChecksumToSolution"/> ensuring that the very last solution requested is
        /// kept alive by us, even if there are no active requests currently in progress for that solution.  That
        /// way if we have two non-concurrent requests for that same solution, with no intervening updates, we can
        /// cache and keep the solution around instead of having to recompute it.
        /// </summary>
        private InFlightSolution? _lastAnyBranchSolution;

        /// <summary>
        /// The same as <see cref="_lastAnyBranchSolution"/> except only for solution checksums we heard about in <see
        /// cref="UpdatePrimaryBranchSolutionAsync"/>.
        /// </summary>
        private InFlightSolution? _lastPrimaryBranchSolution;

        /// <summary>
        /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a
        /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
        /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.
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

            // Now mark this as the last-requested-solution.  Ensuring we keep alive at least one recent solution even
            // if there are no current host requests active.
            SetLastRequestedSolution_NoLock(solution);

            // Our in-flight-count must not have somehow dropped here.  Note: we cannot assert that it incremented.
            // For example, if TryFastGetSolution found the item in the last-requested-solution slot then trying to
            // set it again as the last-requested-solution will not change anything.
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

                var solution = TryFastGetSolution_NoLock();
                if (solution is not null)
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(solution.InFlightCount < 1);

                    // Increase the count as our caller now is keeping this solution in-flight
                    solution.IncrementInFlightCount_NoLock();
                    Contract.ThrowIfTrue(solution.InFlightCount < 2);

                    return solution;
                }

                // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                // in-flight-count of 1 to represent our caller. 
                solution = new InFlightSolution(
                    this, solutionChecksum,
                    cancellationToken => ComputeDisconnectedSolutionAsync(assetProvider, solutionChecksum, cancellationToken));
                Contract.ThrowIfFalse(solution.InFlightCount == 1);

                _solutionChecksumToSolution.Add(solutionChecksum, solution);

                return solution;
            }

            InFlightSolution? TryFastGetSolution_NoLock()
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                if (_lastPrimaryBranchSolution?.SolutionChecksum == solutionChecksum)
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(_lastPrimaryBranchSolution.InFlightCount < 1);
                    return _lastPrimaryBranchSolution;
                }

                if (_lastAnyBranchSolution?.SolutionChecksum == solutionChecksum)
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(_lastAnyBranchSolution.InFlightCount < 1);
                    return _lastAnyBranchSolution;
                }

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var solution))
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(solution.InFlightCount < 1);
                    return solution;
                }

                return null;
            }

            void SetLastRequestedSolution_NoLock(InFlightSolution solution)
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                // The solution being passed in must have a valid in-flight-count since the caller currently has it in flight
                Contract.ThrowIfTrue(solution.InFlightCount < 1);

                // Always set the last requested solution.
                SetLastRequestedSolution(solution, primaryBranch: false);

                // If we're updating the primary branch, then set the last requested primary branch solution as well.
                if (updatePrimaryBranch)
                    SetLastRequestedSolution(solution, primaryBranch: true);

                return;

                void SetLastRequestedSolution(InFlightSolution solution, bool primaryBranch)
                {
                    // Keep track of the existing solution so we can decrement the in-flight-count on it once done.
                    var solutionToDecrement = primaryBranch ? _lastPrimaryBranchSolution : _lastAnyBranchSolution;

                    // Increase the in-flight-count as we are now holding onto this solution as well.
                    solution.IncrementInFlightCount_NoLock();

                    // At this point our caller has upped the in-flight-count and we have upped it as well, so we must at least have a count of 2.
                    Contract.ThrowIfTrue(solution.InFlightCount < 2);

                    if (primaryBranch)
                    {
                        _lastPrimaryBranchSolution = solution;
                    }
                    else
                    {
                        _lastAnyBranchSolution = solution;
                    }

                    // Decrement the in-flight-count on the last solution we were pointing at.  If we were the last
                    // count on it then it will get removed from the cache.
                    if (solutionToDecrement != null)
                    {
                        // If were holding onto this solution, it must have a legal in-flight-count.
                        Contract.ThrowIfTrue(solutionToDecrement.InFlightCount < 1);
                        solutionToDecrement.DecrementInFlightCount_NoLock();

                        if (solutionToDecrement.InFlightCount == 0)
                        {
                            Contract.ThrowIfTrue(_lastAnyBranchSolution == solutionToDecrement);
                            Contract.ThrowIfTrue(_lastPrimaryBranchSolution == solutionToDecrement);
                            Contract.ThrowIfFalse(_solutionChecksumToSolution.ContainsKey(solutionChecksum));
                        }
                    }
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

            if (_lastAnyBranchSolution != null)
            {
                // if we're holding onto this solution, it must also be in _solutionChecksumToSolution.
                Contract.ThrowIfFalse(_solutionChecksumToSolution.TryGetValue(_lastAnyBranchSolution.SolutionChecksum, out var solution));
                Contract.ThrowIfFalse(_lastAnyBranchSolution == solution);
            }

            if (_lastPrimaryBranchSolution != null)
            {
                // if we're holding onto this solution, it must also be in _solutionChecksumToSolution.
                Contract.ThrowIfFalse(_solutionChecksumToSolution.TryGetValue(_lastPrimaryBranchSolution.SolutionChecksum, out var solution));
                Contract.ThrowIfFalse(_lastPrimaryBranchSolution == solution);
            }
        }
    }
}
