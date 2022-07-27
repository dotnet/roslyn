// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteWorkspace
    {
        private sealed partial class ChecksumToSolutionCache
        {
            /// <summary>
            /// Wrapper around asynchronously produced solution.  The computation for producing the solution will be
            /// canceled when the number of in-flight operations using it goes down to 0.
            /// </summary>
            public sealed class SolutionAndInFlightCount
            {
                private readonly ChecksumToSolutionCache _cache;
                public readonly Checksum SolutionChecksum;

                private readonly CancellationTokenSource _cancellationTokenSource = new();

                /// <summary>
                /// Initially set to 1 to represent the operation that requested and is using this solution.  This also
                /// allows us to use 0 to represent a point that this solution computation is canceled and can not be
                /// used again.
                /// </summary>
                private int _inFlightCount = 1;

                // For assertion purposes.
                public int InFlightCount => _inFlightCount;

                public SolutionAndInFlightCount(
                    ChecksumToSolutionCache cache,
                    Checksum solutionChecksum,
                    Func<CancellationToken, Task<Solution>> getSolutionAsync)
                {
                    _cache = cache;
                    SolutionChecksum = solutionChecksum;
                    Task = getSolutionAsync(_cancellationTokenSource.Token);
                }

                public Task<Solution> Task { get; }

                public void IncrementInFlightCount()
                {
                    using (_cache._gate.DisposableWait(CancellationToken.None))
                    {
                        IncrementInFlightCount_WhileAlreadyHoldingLock();
                    }
                }

                public void IncrementInFlightCount_WhileAlreadyHoldingLock()
                {
                    Contract.ThrowIfFalse(_cache._gate.CurrentCount == 0);
                    Contract.ThrowIfTrue(_inFlightCount < 1);
                    _inFlightCount++;
                }

                public void DecrementInFlightCount()
                {
                    using (_cache._gate.DisposableWait(CancellationToken.None))
                    {
                        DecrementInFlightCount_WhileAlreadyHoldingLock();
                    }
                }

                public void DecrementInFlightCount_WhileAlreadyHoldingLock()
                {
                    Contract.ThrowIfFalse(_cache._gate.CurrentCount == 0);
                    Contract.ThrowIfTrue(_inFlightCount < 1);
                    _inFlightCount--;
                    if (_inFlightCount == 0)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();

                        // If we're going away, then we absolutely must not be pointed at in the _lastRequestedSolution field.
                        Contract.ThrowIfTrue(_cache._lastRequestedSolution == this);

                        // If we're going away, we better find ourself in the mapping for this checksum.
                        Contract.ThrowIfFalse(_cache._solutionChecksumToSolution.TryGetValue(SolutionChecksum, out var existingSolution));
                        Contract.ThrowIfFalse(existingSolution == this);

                        // And we better succeed at actually removing.
                        Contract.ThrowIfFalse(_cache._solutionChecksumToSolution.Remove(SolutionChecksum));
                    }
                }
            }
        }
    }
}
