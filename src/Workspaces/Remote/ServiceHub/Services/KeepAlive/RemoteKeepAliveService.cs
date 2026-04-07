// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteKeepAliveService : BrokeredServiceBase, IRemoteKeepAliveService
{
    internal sealed class Factory : FactoryBase<IRemoteKeepAliveService>
    {
        protected override IRemoteKeepAliveService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteKeepAliveService(arguments);
    }

    /// <summary>
    /// Mapping from sessionId to the completion source that will be used to signal when that session's solution
    /// snapshot is fully hydrated on the OOP (this) side.  The `bool` result of the TaskCompletionSource is unused, and
    /// is only there because TaskCompletionSource requires some type on netstandard2.0.
    /// </summary>
    private readonly ConcurrentDictionary<long, TaskCompletionSource<bool>> _sessionIdToCompletionSource = new();

    public RemoteKeepAliveService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    private TaskCompletionSource<bool> GetSessionCompletionSource(long sessionId)
        => _sessionIdToCompletionSource.GetOrAdd(sessionId, static _ => new());

    public async ValueTask KeepAliveAsync(
        Checksum solutionChecksum,
        long sessionId,
        CancellationToken cancellationToken)
    {
        // Ensure we have a completion source for this sessionId.  Note: we are potentially racing with
        // WaitForSessionIdAsync on the host side, so it's possible that it will beat us to creating the entry in this
        // dictionary.  That's fine though, as both calls will be guaranteed to get the same completionSource thanks to
        // GetOrAdd.
        var completionSource = GetSessionCompletionSource(sessionId);
        try
        {
            // First get the solution, ensuring that it is currently pinned.
            await RunServiceAsync(solutionChecksum, async solution =>
            {
                // Now that we have the solution, we can trigger the completion source to let the host know it can 
                // proceed with its work.
                completionSource.TrySetResult(true);

                // Wait for our caller to tell us to cancel.  That way we can release this solution and allow it
                // to be collected if not needed anymore.
                //
                // This was provided by stoub as an idiomatic way to wait indefinitely until a cancellation token triggers.
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Now that we've been cancelled, remove the completion source from our map.  Note: this cancellationToken
            // is the one owned by RemoteKeepAliveSession, and only triggered in its Dispose method.  We only get to
            // that point once the RemoteKeepAliveSession is fully created, returned back out to the caller, and then
            // eventually Disposed of.  That means that by the time we get here, WaitForSessionIdAsync already must have
            // returned, so there is no concern about racing at this point, and we are certain to clear out the value
            // and not leave around stale entries.
            Contract.ThrowIfFalse(_sessionIdToCompletionSource.TryRemove(sessionId, out var foundSource));
            Contract.ThrowIfFalse(foundSource == completionSource);
        }
    }

    public async ValueTask WaitForSessionIdAsync(long sessionId, CancellationToken cancellationToken)
    {
        // Ensure we have a completion source for this sessionId.  Note: we are potentially racing with KeepAliveAsync
        // on the host side, so it's possible that it will beat us to creating the entry in this dictionary.  That's
        // fine though, as both calls will be guaranteed to get the same completionSource thanks to GetOrAdd.
        var completionSource = GetSessionCompletionSource(sessionId);

        // Now, wait for KeepAliveAsync to finally signal us to proceed.  We also listen for cancellation so that the
        // host can bail out if needed, without waiting on the full solution (or project cone) to sync over.
        await completionSource.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
    }
}
