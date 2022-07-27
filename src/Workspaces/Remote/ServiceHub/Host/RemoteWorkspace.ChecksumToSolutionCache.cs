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
            /// The last checksum and solution requested by a service. This effectively adds an additional ref count to
            /// one of the items in <see cref="_solutionChecksumToSolution"/> ensuring that the very last solution
            /// requested is kept alive by us, even if there are no active requests currently in progress for that
            /// solution.  That way if we have two non-concurrent requests for that same solution, with no intervening
            /// updates, we can cache and keep the solution around instead of having to recompute it.
            /// </summary>
            private Checksum? _lastRequestedChecksum;
            private RefCountedSolution? _lastRequestedSolution;

            /// <summary>
            /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a
            /// solution around as long as the checksum for it is being used in service of some feature operation (e.g.
            /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
            /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.
            /// </summary>
            private readonly Dictionary<Checksum, RefCountedSolution> _solutionChecksumToSolution = new();

            public ChecksumToSolutionCache(SemaphoreSlim gate)
            {
                _gate = gate;
            }

            public async ValueTask<RefCountedSolution?> TryFastGetSolutionAndAddRefAsync(
                Checksum solutionChecksum,
                CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = default;

                    var refCountedSolution = TryFastGetSolution_NoLock_NoRefCountChange(solutionChecksum);

                    if (refCountedSolution != null)
                    {
                        // We are holding the solution ourself.  So it must have a legal refcount.
                        Contract.ThrowIfTrue(refCountedSolution.RefCount < 1);

                        // Increase the ref count as our caller now owns a ref to this solution as well.
                        refCountedSolution.AddReference_WhileAlreadyHoldingLock();

                        Contract.ThrowIfTrue(refCountedSolution.RefCount < 2);
                    }

                    return refCountedSolution;
                }
            }

            public RefCountedSolution? TryFastGetSolution_NoLock_NoRefCountChange(
                Checksum solutionChecksum)
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                if (_lastRequestedChecksum == solutionChecksum)
                {
                    // If we had a checksum match, then we must have a cached solution.
                    Contract.ThrowIfNull(_lastRequestedSolution);

                    // The cached solution must have a valid ref count.
                    Contract.ThrowIfTrue(_lastRequestedSolution.RefCount < 1);
                    return _lastRequestedSolution;
                }

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var refCountedSolution))
                {
                    // The cached solution must have a valid ref count.
                    Contract.ThrowIfTrue(refCountedSolution.RefCount < 1);
                    return refCountedSolution;
                }

                return null;
            }

            public async ValueTask<RefCountedSolution> SlowGetOrCreateSolutionAndAddRefAsync(
                Checksum solutionChecksum, Func<CancellationToken, Task<Solution>> getSolutionAsync, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = default;

                    // see if someone already raced with us and set the solution in the cache while we were waiting on the lock.
                    var refCountedSolution = TryFastGetSolution_NoLock_NoRefCountChange(solutionChecksum);
                    if (refCountedSolution == null)
                    {
                        // We're the first call that is asking about this checksum.  Create a lazy to compute it with a
                        // refcount of 0.
                        refCountedSolution = new RefCountedSolution(this, solutionChecksum, getSolutionAsync);
                        Contract.ThrowIfFalse(refCountedSolution.RefCount == 0);

                        // Add a ref count to represent being held by us.
                        refCountedSolution.AddReference_WhileAlreadyHoldingLock();
                        Contract.ThrowIfFalse(refCountedSolution.RefCount == 1);

                        _solutionChecksumToSolution.Add(solutionChecksum, refCountedSolution);
                    }

                    // We are holding the solution ourself.  So it must have a legal refcount.
                    Contract.ThrowIfTrue(refCountedSolution.RefCount < 1);

                    // Increase the ref count as our caller now owns a ref to this solution as well.
                    refCountedSolution.AddReference_WhileAlreadyHoldingLock();
                    Contract.ThrowIfTrue(refCountedSolution.RefCount < 2);

                    return refCountedSolution;
                }
            }

            public async Task SetLastRequestedSolutionAsync(Checksum solutionChecksum, RefCountedSolution refCountedSolution, CancellationToken cancellationToken)
            {
                // The solution being passed in must have a valid ref count since the caller is holding onto it.
                Contract.ThrowIfTrue(refCountedSolution.RefCount < 1);

                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // From this point on we are mutating state.  Ensure we absolutely do not cancel accidentally.
                    cancellationToken = default;

                    // Keep track of the existing solution so we can release the ref on it once done.
                    var solutionToRelease = _lastRequestedSolution;

                    // Increase the ref count as we are now holding onto this solution as well.
                    refCountedSolution.AddReference_WhileAlreadyHoldingLock();

                    // At this point our caller has a ref and we have a ref, so we must at least have a ref count of 2.
                    Contract.ThrowIfTrue(refCountedSolution.RefCount < 2);

                    (_lastRequestedSolution, _lastRequestedChecksum) = (refCountedSolution, solutionChecksum);

                    // Release the ref count on the last solution we were pointing at.  If we were the last reference to
                    // it, it will get removed from the cache.
                    if (solutionToRelease != null)
                    {
                        // If were holding onto this solution, it must have a legal refcount.
                        Contract.ThrowIfTrue(solutionToRelease.RefCount < 1);
                        solutionToRelease?.Release_WhileAlreadyHoldingLock();
                    }
                }
            }
        }
    }
}
