// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

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

        /// <summary>
        /// CancellationTokenSource controlling the execution of <see cref="_disconnectedSolutionTask"/> and <see
        /// cref="_primaryBranchTask"/>.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource_doNotAccessDirectly = new();

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
            Contract.ThrowIfFalse(workspace._gate.CurrentCount == 0);

            _workspace = workspace;
            SolutionChecksum = solutionChecksum;

            // Grab the cancellation token up front while we know we are holding the lock and mutating state.  We
            // don't want to potentially grab it at some undefined point at the future when this type has already
            // had its in-flight-count reduced to 0 and thus had our CTS be disposed.
            //
            // Also kick this off in a dedicated task.  The state mutation updating of this InFlightSolution and the
            // cache state in RemoteWorkspace must always run fully to completion without issue.  We don't want
            // anything we call in the constructor to possibly prevent the constructor from running to completion.
            var cancellationToken = this.CancellationToken;
            _disconnectedSolutionTask = Task.Run(() => computeDisconnectedSolutionAsync(cancellationToken), cancellationToken);
        }

        private CancellationToken CancellationToken
        {
            get
            {
                // Only safe to get this when the lock is being held.  That way nothing is racing against the work
                // in DecrementInFlightCount_NoLock which cancels this CTS. 
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
                return _cancellationTokenSource_doNotAccessDirectly.Token;
            }
        }

        public Task<Solution> PreferredSolutionTask_NoLock
        {
            get
            {
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);

                // Defer to the primary branch task if we have it, otherwise, fallback to the any-branch-task. This
                // keeps everything on the primary branch if possible, allowing more sharing of services/caches.
                return _primaryBranchTask ?? _disconnectedSolutionTask;
            }
        }

        /// <summary>
        /// Allow the RemoteWorkspace to try to elevate this solution to be the primary solution for itself.  This
        /// commonly happens because when a change happens to the host, features may kick off immediately, creating
        /// the disconnected solution, followed shortly afterwards by a request from the host to make that same
        /// checksum be the primary solution of this workspace.
        /// </summary>
        /// <param name="updatePrimaryBranchAsync"></param>
        public void TryKickOffPrimaryBranchWork_NoLock(Func<Solution, CancellationToken, Task<Solution>> updatePrimaryBranchAsync)
        {
            try
            {
                Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
                Contract.ThrowIfNull(updatePrimaryBranchAsync);
                Contract.ThrowIfTrue(this._cancellationTokenSource_doNotAccessDirectly.IsCancellationRequested);

                // Already set up the work to update the primary branch
                if (_primaryBranchTask != null)
                    return;

                // Grab the cancellation token up front while we know we are holding the lock and mutating state.  We
                // don't want to potentially grab it at some undefined point at the future when this type has already
                // had its in-flight-count reduced to 0 and thus had our CTS be disposed.
                //
                // Also kick this off in a dedicated task.  The state mutation updating of this InFlightSolution and the
                // cache state in RemoteWorkspace must always run fully to completion without issue. We don't want
                // anything we call in this method to possibly prevent the method from running to completion.
                var cancellationToken = this.CancellationToken;
                _primaryBranchTask = Task.Run(() => ComputePrimaryBranchAsync(cancellationToken), cancellationToken);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagate(ex, ErrorSeverity.General))
            {
                // The above must never throw.  If it does our caller will not properly update the cache state
                // properly and can leave things invalid.
            }

            return;

            async Task<Solution> ComputePrimaryBranchAsync(CancellationToken cancellationToken)
            {
                var solution = await _disconnectedSolutionTask.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                return await updatePrimaryBranchAsync(solution, cancellationToken).ConfigureAwait(false);
            }
        }

        public void IncrementInFlightCount_NoLock()
        {
            Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
            Contract.ThrowIfTrue(InFlightCount < 1);
            InFlightCount++;
        }

        /// <summary>
        /// Returns the in-flight solution computations <em>when</em> the in-flight-count is decremented to 0. This
        /// allows the caller to wait for those computations to complete (which will hopefully be quickly as they
        /// will have just been canceled).  This ensures the caller doesn't return back to the host (potentially
        /// unpinning the solution on the host) while the solution-computation tasks are still running and may still
        /// attempt to call into the host.
        /// </summary>
        public ImmutableArray<Task> DecrementInFlightCount_NoLock()
        {
            Contract.ThrowIfFalse(_workspace._gate.CurrentCount == 0);
            Contract.ThrowIfTrue(InFlightCount < 1);
            InFlightCount--;
            if (InFlightCount != 0)
                return [];

            _cancellationTokenSource_doNotAccessDirectly.Cancel();
            _cancellationTokenSource_doNotAccessDirectly.Dispose();

            // If we're going away, we better find ourself in the mapping for this checksum.
            Contract.ThrowIfFalse(_workspace._solutionChecksumToSolution.TryGetValue(SolutionChecksum, out var existingSolution));
            Contract.ThrowIfFalse(existingSolution == this);

            // And we better succeed at actually removing.
            Contract.ThrowIfFalse(_workspace._solutionChecksumToSolution.Remove(SolutionChecksum));

            _workspace.CheckCacheInvariants_NoLock();

            // Return the solutions we were in the process of computing.  Note, returning the _primaryBranchTask is
            // likely not necessary as all that task does is take the _disconnectedSolutionTask and make it the
            // primary branch of hte workspace (which doesn't involve a call back from OOP to the host).  However,
            // this is just safer to return both, esp. if that might ever change in the future.
            using var solutions = TemporaryArray<Task>.Empty;

            solutions.Add(_disconnectedSolutionTask);
            solutions.AsRef().AddIfNotNull(_primaryBranchTask);

            return solutions.ToImmutableAndClear();
        }
    }
}
