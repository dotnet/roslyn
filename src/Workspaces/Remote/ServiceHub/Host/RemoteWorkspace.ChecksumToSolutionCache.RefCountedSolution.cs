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
            /// Ref counted wrapper around asynchronously produced solution.  The computation for producing the solution
            /// will be canceled when the ref-count goes to 0.
            /// </summary>
            public sealed class RefCountedSolution : IDisposable
            {
                private readonly ChecksumToSolutionCache _cache;
                private readonly Checksum _solutionChecksum;

                private readonly CancellationTokenSource _cancellationTokenSource = new();

                private int _refCount = 0;

                // For assertion purposes.
                public int RefCount => _refCount;

                public RefCountedSolution(
                    ChecksumToSolutionCache cache,
                    Checksum solutionChecksum,
                    Func<CancellationToken, Task<Solution>> getSolutionAsync)
                {
                    _cache = cache;
                    _solutionChecksum = solutionChecksum;
                    Task = getSolutionAsync(_cancellationTokenSource.Token);
                }

                public Task<Solution> Task { get; }

                public void AddReference_TakeLock()
                {
                    using (_cache._gate.DisposableWait(CancellationToken.None))
                    {
                        AddReference_WhileAlreadyHoldingLock();
                    }
                }

                public void AddReference_WhileAlreadyHoldingLock()
                {
                    Contract.ThrowIfFalse(_cache._gate.CurrentCount == 0);
                    Contract.ThrowIfTrue(_refCount < 1);
                    _refCount++;
                }

                public void Dispose()
                {
                    using (_cache._gate.DisposableWait(CancellationToken.None))
                    {
                        Release_WhileAlreadyHoldingLock();
                    }
                }

                public void Release_WhileAlreadyHoldingLock()
                {
                    Contract.ThrowIfFalse(_cache._gate.CurrentCount == 0);
                    Contract.ThrowIfTrue(_refCount < 1);
                    _refCount--;
                    if (_refCount == 0)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();

                        Contract.ThrowIfFalse(_cache._solutionChecksumToSolution.TryGetValue(_solutionChecksum, out var existingSolution));
                        Contract.ThrowIfFalse(existingSolution == this);
                        _cache._solutionChecksumToSolution.Remove(_solutionChecksum);
                    }
                }
            }
        }
    }
}
