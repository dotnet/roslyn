// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteExtensionMessageHandlerService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteExtensionMessageHandlerService
{
    internal sealed class Factory : FactoryBase<IRemoteExtensionMessageHandlerService>
    {
        protected override IRemoteExtensionMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteExtensionMessageHandlerService(arguments);
    }

    public ValueTask<RegisterExtensionResponse> RegisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspaceServices().GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            cancellationToken => service.RegisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken);
    }

    public ValueTask UnregisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspaceServices().GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            cancellationToken => service.UnregisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken);
    }

    public ValueTask<string> HandleExtensionDocumentMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspaceServices().GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            solutionChecksum,
            async solution =>
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                return await service.HandleExtensionDocumentMessageAsync(
                    document, messageName, jsonMessage, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<string> HandleExtensionWorkspaceMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspaceServices().GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            solutionChecksum,
            solution => service.HandleExtensionWorkspaceMessageAsync(
                solution, messageName, jsonMessage, cancellationToken),
            cancellationToken);
    }

    public ValueTask ResetAsync(
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            cancellationToken =>
            {
                service.Reset();
                return default;
            },
            cancellationToken);
    }
}
