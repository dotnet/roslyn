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
        private async ValueTask<InFlightSolution> GetOrCreateSolutionAndAddInFlightCountAsync(
            Checksum solutionChecksum,
            Func<CancellationToken, Task<Solution>> computeDisconnectedSolutionAsync,
            Func<Solution, CancellationToken, Task<Solution>>? updatePrimaryBranchAsync,
            CancellationToken cancellationToken)
        {
            var updatePrimaryBranch = updatePrimaryBranchAsync != null;

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                cancellationToken = CancellationToken.None;

                // see if someone already raced with us and set the solution in the cache while we were waiting on the lock.
                var solution = TryFastGetSolutionAndAddInFlightCount_NoLock(solutionChecksum, updatePrimaryBranch);
                if (solution != null)
                {
                    // We may be getting back a solution that only was computing a non-primary branch.  If we were asked
                    // to compute the primary branch as well, let it know so it can start that now.
                    solution.TryKickOffPrimaryBranchWork(updatePrimaryBranchAsync);

                    // We must have an in-flight-count of at least 2.  One for our caller who is requesting this,
                    // and one because TryFastGetSolutionAndAddInFlightCount_NoLock would have put this in the 
                    // last-requested-solution bucket and increased the count to that.
                    Contract.ThrowIfFalse(solution.InFlightCount < 2);
                    return solution;
                }

                // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                // in-flight-count of 1 to represent our caller. 
                solution = new InFlightSolution(this, solutionChecksum, computeDisconnectedSolutionAsync, updatePrimaryBranchAsync);
                Contract.ThrowIfFalse(solution.InFlightCount == 1);

                _solutionChecksumToSolution.Add(solutionChecksum, solution);

                // Also set this as the last-requested-solution.  Note this means our ref-count must go up since we
                // just created this item and will always succeed at then increating the in-flight-count ourselves
                // when we set the last-requsted-solution.
                SetLastRequestedSolution_NoLock(solution, updatePrimaryBranch);
                Contract.ThrowIfFalse(solution.InFlightCount == 2);

                return solution;
            }
        }

        private InFlightSolution? TryFastGetSolutionAndAddInFlightCount_NoLock(Checksum solutionChecksum, bool updatePrimaryBranch)
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

            var solution = TryFastGetSolution();
            if (solution is null)
                return null;

            // If we found the solution, it must have a valid in-flight-count.
            Contract.ThrowIfTrue(solution.InFlightCount < 0);

            // Increase the count as our caller now is keeping this solution in-flight
            solution.IncrementInFlightCount_WhileAlreadyHoldingLock();

            Contract.ThrowIfTrue(solution.InFlightCount < 1);

            // Now mark this as the last-requested-solution for this cache.
            SetLastRequestedSolution_NoLock(solution, updatePrimaryBranch);

            // Our in-flight-count must not have somehow dropped here.  Note: we cannot assert that it incremented.
            // For example, if TryFastGetSolution found the item in the last-requested-solution slot then trying to
            // set it again as the last-requested-solution will not change anything.
            Contract.ThrowIfTrue(solution.InFlightCount < 1);

            return solution;

            InFlightSolution? TryFastGetSolution()
            {
                if (_lastPrimaryBranchSolution?.SolutionChecksum == solutionChecksum)
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(_lastPrimaryBranchSolution.InFlightCount < 0);
                    return _lastPrimaryBranchSolution;
                }

                if (_lastAnyBranchSolution?.SolutionChecksum == solutionChecksum)
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(_lastAnyBranchSolution.InFlightCount < 0);
                    return _lastAnyBranchSolution;
                }

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var solution))
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(solution.InFlightCount < 0);
                    return solution;
                }

                return null;
            }
        }

        private void SetLastRequestedSolution_NoLock(InFlightSolution solution, bool updatePrimaryBranch)
        {
            // The solution being passed in must have a valid in-flight-count since the caller currently has it in flight
            Contract.ThrowIfTrue(solution.InFlightCount < 1);
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

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
                solution.IncrementInFlightCount_WhileAlreadyHoldingLock();

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
                    solutionToDecrement.DecrementInFlightCount_WhileAlreadyHoldingLock();
                }
            }
        }
    }
}
