// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteWorkspace
    {
        /// <summary>
        /// Wrapper around asynchronously produced solution for a particular <see cref="SolutionChecksum"/>.  The
        /// computation for producing the solution will be canceled when the number of in-flight operations using it
        /// goes down to 0.
        /// </summary>
        public sealed class InFlightSolution
        {
            private readonly RemoteWorkspace _workspace;

            public readonly Checksum SolutionChecksum;

            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// Background work to just compute the disconnected solution associated with this <see cref="SolutionChecksum"/>
            /// </summary>
            private readonly Task<Solution> _disconnectedSolutionTask;

            /// <summary>
            /// Optional work to try to elevate the solution computed by <see cref="_disconnectedSolutionTask"/> to be
            /// the primary solution of this <see cref="RemoteWorkspace"/>.  Must only be read/written while holding
            /// <see cref="RemoteWorkspace._gate"/>.
            /// </summary>
            private Task<Solution>? _primaryBranchTask;

            /// <summary>
            /// Initially set to 1 to represent the operation that requested and is using this solution.  This also
            /// allows us to use 0 to represent a point that this solution computation is canceled and can not be
            /// used again.
            /// </summary>
            public int InFlightCount { get; private set; } = 1;

            public InFlightSolution(
                RemoteWorkspace workspace,
                Checksum solutionChecksum,
                Func<CancellationToken, Task<Solution>> computeDisconnectedSolutionAsync)
            {
                _workspace = workspace;
                SolutionChecksum = solutionChecksum;

                _disconnectedSolutionTask = computeDisconnectedSolutionAsync(_cancellationTokenSource.Token);
            }

            /// <summary>
            /// Allow the RemoteWorkspace to try to elevate this solution to be the primary solution for itself.  This
            /// commonly happens because when a change happens to the host, features may kick off immediately, creating
            /// the disconnected solution, followed shortly afterwards by a request from the host to make that same
            /// checksum be the primary solution of this workspace.
            /// </summary>
            /// <param name="updatePrimaryBranchAsync"></param>
            public void TryKickOffPrimaryBranchWork_NoLock(Func<Solution, CancellationToken, Task<Solution>>? updatePrimaryBranchAsync)
            {
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);

                if (updatePrimaryBranchAsync is null)
                    return;

                // Already set up the work to update the primary branch
                if (_primaryBranchTask != null)
                    return;

                _primaryBranchTask = ComputePrimaryBranchAsync();
                return;

                async Task<Solution> ComputePrimaryBranchAsync()
                {
                    var anyBranchSolution = await _disconnectedSolutionTask.ConfigureAwait(false);
                    return await updatePrimaryBranchAsync(anyBranchSolution, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }

            public async ValueTask<Solution> GetSolutionAsync(CancellationToken cancellationToken)
            {
                // Defer to the primary branch task if we have it, otherwise, fallback to the any-branch-task. This
                // keeps everything on the primary branch if possible, allowing more sharing of services/caches.
                Task<Solution> task;
                using (await _workspace._gate.DisposableWaitAsync(cancellationToken))
                {
                    task = _primaryBranchTask ?? _disconnectedSolutionTask;
                }

                return await task.WithCancellation(cancellationToken).ConfigureAwait(false);
            }

            public void IncrementInFlightCount_WhileAlreadyHoldingLock()
            {
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
                Contract.ThrowIfTrue(InFlightCount < 1);
                InFlightCount++;
            }

            public void DecrementInFlightCount()
            {
                // Intentionally not cancellable.  We must do the decrement to ensure our state is consistent.  This
                // does block the calling thread.  However, this should only be for a short amount of time as nothing in
                // RemoteWorkspace should ever hold this lock for long periods of time.
                using (_workspace._gate.DisposableWait(CancellationToken.None))
                {
                    DecrementInFlightCount_WhileAlreadyHoldingLock();
                }
            }

            public void DecrementInFlightCount_WhileAlreadyHoldingLock()
            {
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
                Contract.ThrowIfTrue(InFlightCount < 1);
                InFlightCount--;
                if (InFlightCount == 0)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();

                    // If we're going away, then we absolutely must not be pointed at in the _lastRequestedSolution field.
                    Contract.ThrowIfTrue(_workspace._lastAnyBranchSolution == this);
                    Contract.ThrowIfTrue(_workspace._lastPrimaryBranchSolution == this);

                    // If we're going away, we better find ourself in the mapping for this checksum.
                    Contract.ThrowIfFalse(_workspace._solutionChecksumToSolution.TryGetValue(SolutionChecksum, out var existingSolution));
                    Contract.ThrowIfFalse(existingSolution == this);

                    // And we better succeed at actually removing.
                    Contract.ThrowIfFalse(_workspace._solutionChecksumToSolution.Remove(SolutionChecksum));
                }
            }
        }
    }
}
