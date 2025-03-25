// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteCustomMessageHandlerService : BrokeredServiceBase, IRemoteCustomMessageHandlerService
{
    internal sealed class Factory : FactoryBase<IRemoteCustomMessageHandlerService>
    {
        protected override IRemoteCustomMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCustomMessageHandlerService(arguments);
    }

    public RemoteCustomMessageHandlerService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif
    }

    public ValueTask<RegisterHandlersResponse> LoadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        string assemblyFileName,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<ICustomMessageHandlerService>();
        return RunServiceAsync(
            (_) => service.LoadCustomMessageHandlersAsync(
                assemblyFolderPath,
                assemblyFileName,
                cancellationToken),
            cancellationToken);
    }

    public ValueTask<string> HandleCustomDocumentMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            solution =>
            {
                var service = solution.Services.GetRequiredService<ICustomMessageHandlerService>();
                return service.HandleCustomDocumentMessageAsync(
                solution,
                messageName,
                jsonMessage,
                documentId,
                cancellationToken);
            },
            cancellationToken);
    }

    public ValueTask<string> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            solution =>
            {
                var service = solution.Services.GetRequiredService<ICustomMessageHandlerService>();
                return service.HandleCustomMessageAsync(
                solution,
                messageName,
                jsonMessage,
                cancellationToken);
            },
            cancellationToken);
    }

    public ValueTask UnloadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<ICustomMessageHandlerService>();
        return RunServiceAsync(
            (_) => service.UnloadCustomMessageHandlersAsync(
                assemblyFolderPath,
                cancellationToken),
            cancellationToken);
    }

    public ValueTask ResetAsync(
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<ICustomMessageHandlerService>();
        return RunServiceAsync(
            (_) => service.ResetAsync(
                cancellationToken),
            cancellationToken);
    }
}
