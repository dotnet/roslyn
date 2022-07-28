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
        /// Cache of recently requested solution snapshots.  This always stores the last snapshot requested, and also
        /// stores any requested solution snapshot that is currently in-use.  This allows concurrent calls to come in
        /// and see/reuse in-flight solution snapshots being used by other requests.
        /// </summary>
        private sealed partial class ChecksumToSolutionCache
        {
            /// <summary>
            /// Pointer to <see cref="RemoteWorkspace._gate"/>.
            /// </summary>
            private readonly SemaphoreSlim _gate;

            /// <summary>
            /// The last solution requested by a service. This effectively adds an additional in-flight-count to one of
            /// the items in <see cref="_solutionChecksumToSolution"/> ensuring that the very last solution requested is
            /// kept alive by us, even if there are no active requests currently in progress for that solution.  That
            /// way if we have two non-concurrent requests for that same solution, with no intervening updates, we can
            /// cache and keep the solution around instead of having to recompute it.
            /// </summary>
            private SolutionAndInFlightCount? _lastRequestedSolution;

            /// <summary>
            /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a
            /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
            /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
            /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.
            /// </summary>
            private readonly Dictionary<Checksum, SolutionAndInFlightCount> _solutionChecksumToSolution = new();

            public ChecksumToSolutionCache(SemaphoreSlim gate)
            {
                _gate = gate;
            }

            public async ValueTask<SolutionAndInFlightCount?> TryFastGetSolutionAndAddInFlightCountAsync(
                Checksum solutionChecksum,
                CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = CancellationToken.None;

                    return TryFastGetSolutionAndAddInFlightCount_NoLock(solutionChecksum);
                }
            }

            public SolutionAndInFlightCount? TryFastGetSolutionAndAddInFlightCount_NoLock(Checksum solutionChecksum)
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
                SetLastRequestedSolution_NoLock(solution);

                // Our in-flight-count must not have somehow dropped here.  Note: we cannot assert that it incremented.
                // For example, if TryFastGetSolution found the item in the last-requested-solution slot then trying to
                // set it again as the last-requested-solution will not change anything.
                Contract.ThrowIfTrue(solution.InFlightCount < 1);

                return solution;

                SolutionAndInFlightCount? TryFastGetSolution()
                {
                    if (_lastRequestedSolution?.SolutionChecksum == solutionChecksum)
                    {
                        // The cached solution must have a valid in-flight-count
                        Contract.ThrowIfTrue(_lastRequestedSolution.InFlightCount < 0);
                        return _lastRequestedSolution;
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

            public async ValueTask<SolutionAndInFlightCount> SlowGetOrCreateSolutionAndAddInFlightCountAsync(
                Checksum solutionChecksum, Func<CancellationToken, Task<Solution>> getSolutionAsync, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = CancellationToken.None;

                    // see if someone already raced with us and set the solution in the cache while we were waiting on the lock.
                    var solution = TryFastGetSolutionAndAddInFlightCount_NoLock(solutionChecksum);
                    if (solution != null)
                    {
                        // We must have an in-flight-count of at least 2.  One for our caller who is requesting this,
                        // and one because TryFastGetSolutionAndAddInFlightCount_NoLock would have put this in the 
                        // last-requested-solution bucket and increased the count to that.
                        Contract.ThrowIfFalse(solution.InFlightCount < 2);
                        return solution;
                    }

                    // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                    // in-flight-count of 1 to represent our caller.
                    solution = new SolutionAndInFlightCount(this, solutionChecksum, getSolutionAsync);
                    Contract.ThrowIfFalse(solution.InFlightCount == 1);

                    _solutionChecksumToSolution.Add(solutionChecksum, solution);

                    // Also set this as the last-requested-solution.  Note this means our ref-count must go up since we
                    // just created this item and will always succeed at then increating the in-flight-count ourselves
                    // when we set the last-requsted-solution.
                    SetLastRequestedSolution_NoLock(solution);
                    Contract.ThrowIfFalse(solution.InFlightCount == 2);

                    return solution;
                }
            }

            private void SetLastRequestedSolution_NoLock(SolutionAndInFlightCount solution)
            {
                // The solution being passed in must have a valid in-flight-count since the caller currently has it in flight
                Contract.ThrowIfTrue(solution.InFlightCount < 1);
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                // Keep track of the existing solution so we can decrement the in-flight-count on it once done.
                var solutionToDecrement = _lastRequestedSolution;

                // Increase the in-flight-count as we are now holding onto this solution as well.
                solution.IncrementInFlightCount_WhileAlreadyHoldingLock();

                // At this point our caller has upped the in-flight-count and we have upped it as well, so we must at least have a count of 2.
                Contract.ThrowIfTrue(solution.InFlightCount < 2);

                _lastRequestedSolution = solution;

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
