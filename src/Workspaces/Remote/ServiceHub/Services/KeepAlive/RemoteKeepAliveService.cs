// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteKeepAliveService : BrokeredServiceBase, IRemoteKeepAliveService
{
    internal sealed class Factory : FactoryBase<IRemoteKeepAliveService>
    {
        protected override IRemoteKeepAliveService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteKeepAliveService(arguments);
    }

    public RemoteKeepAliveService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask KeepAliveAsync(
        Checksum solutionChecksum,
        CancellationToken cancellationToken)
    {
        // First get the solution, ensuring that it is currently pinned.
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            // Wait for our caller to tell us to cancel.  That way we can release this solution and allow it
            // to be collected if not needed anymore.
            //
            // This was provided by stoub as an idiomatic way to wait indefinitely until a cancellation token triggers.
            await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
