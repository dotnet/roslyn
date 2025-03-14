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
    }

    public ValueTask<string> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string assemblyFolderPath,
        string assemblyFileName,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<ICustomMessageHandlerService>();
        return RunServiceAsync(
            solutionChecksum,
            solution => service.HandleCustomMessageAsync(
                solution,
                assemblyFolderPath,
                assemblyFileName,
                typeFullName,
                jsonMessage,
                documentId,
                cancellationToken),
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
}
