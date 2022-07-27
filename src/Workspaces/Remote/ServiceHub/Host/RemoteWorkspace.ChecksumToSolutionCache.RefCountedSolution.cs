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
                private readonly Checksum _solutionChecksum;

                private readonly CancellationTokenSource _cancellationTokenSource = new();

                private int _inFlightCount = 0;

                /// <summary>
                /// True if our _inFlightCount went to 0 and we canceled <see cref="_cancellationTokenSource"/>.  At
                /// this point this instance is no longer usable.
                /// </summary>
                private bool _discarded;

                // For assertion purposes.
                public int InFlightCount => _inFlightCount;

                public SolutionAndInFlightCount(
                    ChecksumToSolutionCache cache,
                    Checksum solutionChecksum,
                    Func<CancellationToken, Task<Solution>> getSolutionAsync)
                {
                    _cache = cache;
                    _solutionChecksum = solutionChecksum;
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
                    Contract.ThrowIfTrue(_discarded);
                    Contract.ThrowIfFalse(_cache._gate.CurrentCount == 0);
                    Contract.ThrowIfTrue(_inFlightCount < 1);
                    _inFlightCount++;
                }

                public void DecrementInFlightCount()
                {
                    using (_cache._gate.DisposableWait(CancellationToken.None))
                    {
                        Contract.ThrowIfTrue(_discarded);
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
                        _discarded = true;
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();

                        Contract.ThrowIfTrue(_cache._lastRequestedSolution == this);
                        Contract.ThrowIfFalse(_cache._solutionChecksumToSolution.TryGetValue(_solutionChecksum, out var existingSolution));
                        Contract.ThrowIfFalse(existingSolution == this);
                        _cache._solutionChecksumToSolution.Remove(_solutionChecksum);
                    }
                }
            }
        }
    }
}
