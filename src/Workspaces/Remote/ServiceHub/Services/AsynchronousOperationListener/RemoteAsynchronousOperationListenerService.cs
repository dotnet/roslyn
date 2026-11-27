// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteAsynchronousOperationListenerService : BrokeredServiceBase, IRemoteAsynchronousOperationListenerService
{
    internal sealed class Factory : FactoryBase<IRemoteAsynchronousOperationListenerService>
    {
        protected override IRemoteAsynchronousOperationListenerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteAsynchronousOperationListenerService(in arguments);
    }

    public RemoteAsynchronousOperationListenerService(in ServiceConstructionArguments arguments)
        : base(in arguments)
    {
    }

    public ValueTask EnableAsync(bool enable, bool diagnostics, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            AsynchronousOperationListenerProvider.Enable(enable, diagnostics);
        }, cancellationToken);
    }

    public ValueTask<bool> IsCompletedAsync(ImmutableArray<string> featureNames, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            var workspace = GetWorkspace();
            var exportProvider = workspace.Services.SolutionServices.ExportProvider;
            var listenerProvider = exportProvider.GetExports<AsynchronousOperationListenerProvider>().Single().Value;

            return !listenerProvider.HasPendingWaiter([.. featureNames]);
        }, cancellationToken);
    }

    public ValueTask ExpeditedWaitAsync(ImmutableArray<string> featureNames, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            var workspace = GetWorkspace();
            var exportProvider = workspace.Services.SolutionServices.ExportProvider;
            var listenerProvider = exportProvider.GetExports<AsynchronousOperationListenerProvider>().Single().Value;

            await listenerProvider.WaitAllAsync(workspace, [.. featureNames]).ConfigureAwait(false);
        }, cancellationToken);
    }
}
