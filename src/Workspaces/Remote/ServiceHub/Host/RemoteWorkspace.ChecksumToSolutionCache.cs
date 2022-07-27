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
            /// The last checksum and solution requested by a service. This effectively adds an additional in-flight
            /// count to one of the items in <see cref="_solutionChecksumToSolution"/> ensuring that the very last
            /// solution requested is kept alive by us, even if there are no active requests currently in progress for
            /// that solution.  That way if we have two non-concurrent requests for that same solution, with no
            /// intervening updates, we can cache and keep the solution around instead of having to recompute it.
            /// </summary>
            private Checksum? _lastRequestedChecksum;
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
                    cancellationToken = default;

                    var solution = TryFastGetSolution_NoLock_NoInFlightCountChange(solutionChecksum);

                    if (solution != null)
                    {
                        Contract.ThrowIfTrue(solution.InFlightCount < 0);

                        // Increase the count as our caller now is keeping this solution in-flight
                        solution.IncrementInFlightCount_WhileAlreadyHoldingLock();

                        Contract.ThrowIfTrue(solution.InFlightCount < 1);
                    }

                    return solution;
                }
            }

            public SolutionAndInFlightCount? TryFastGetSolution_NoLock_NoInFlightCountChange(
                Checksum solutionChecksum)
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                if (_lastRequestedChecksum == solutionChecksum)
                {
                    // If we had a checksum match, then we must have a cached solution.
                    Contract.ThrowIfNull(_lastRequestedSolution);

                    // The cached solution must have a valid in-flight count
                    Contract.ThrowIfTrue(_lastRequestedSolution.InFlightCount < 0);
                    return _lastRequestedSolution;
                }

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var solution))
                {
                    // The cached solution must have a valid in-flight count
                    Contract.ThrowIfTrue(solution.InFlightCount < 0);
                    return solution;
                }

                return null;
            }

            public async ValueTask<SolutionAndInFlightCount> SlowGetOrCreateSolutionAndAddInFlightCountAsync(
                Checksum solutionChecksum, Func<CancellationToken, Task<Solution>> getSolutionAsync, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = default;

                    // see if someone already raced with us and set the solution in the cache while we were waiting on the lock.
                    var solution = TryFastGetSolution_NoLock_NoInFlightCountChange(solutionChecksum);
                    if (solution != null)
                    {
                        // The cached solution must have a valid in-flight count
                        Contract.ThrowIfFalse(solution.InFlightCount < 0);

                        // Increase the count as our caller now is keeping this solution in-flight
                        solution.IncrementInFlightCount_WhileAlreadyHoldingLock();
                        Contract.ThrowIfFalse(solution.InFlightCount < 1);

                        return solution;
                    }
                    else
                    {
                        // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                        // in-flight count of 1 to represent our caller.
                        solution = new SolutionAndInFlightCount(this, solutionChecksum, getSolutionAsync);
                        Contract.ThrowIfFalse(solution.InFlightCount == 1);

                        _solutionChecksumToSolution.Add(solutionChecksum, solution);
                        return solution;
                    }
                }
            }

            public async Task SetLastRequestedSolutionAsync(Checksum solutionChecksum, SolutionAndInFlightCount solution, CancellationToken cancellationToken)
            {
                // The solution being passed in must have a valid in-flight count since the caller currently has it in flight
                Contract.ThrowIfTrue(solution.InFlightCount < 1);

                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = default;

                    // Keep track of the existing solution so we can decrement the in-flight count on it once done.
                    var solutionToDecrement = _lastRequestedSolution;

                    // Increase the in-flight count as we are now holding onto this solution as well.
                    solution.IncrementInFlightCount_WhileAlreadyHoldingLock();

                    // At this point our caller has upped the in-flight count and we have upped it as well, so we must at least have a count of 2.
                    Contract.ThrowIfTrue(solution.InFlightCount < 2);

                    (_lastRequestedSolution, _lastRequestedChecksum) = (solution, solutionChecksum);

                    // Release the in-flight count on the last solution we were pointing at.  If we were the last count
                    // on it then it will get removed from the cache.
                    if (solutionToDecrement != null)
                    {
                        // If were holding onto this solution, it must have a legal in-flight count.
                        Contract.ThrowIfTrue(solutionToDecrement.InFlightCount < 1);
                        solutionToDecrement.DecrementInFlightCount_WhileAlreadyHoldingLock();

                        // after releasing, if we went down to 0 in flight operations, then we better not still be in the cache.
                        if (solutionToDecrement.InFlightCount == 0)
                        {
                            Contract.ThrowIfTrue(_solutionChecksumToSolution.ContainsKey(solutionChecksum));
                        }
                    }
                }
            }
        }
    }
}
