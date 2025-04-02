// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteExtensionMessageHandlerService : BrokeredServiceBase, IRemoteExtensionMessageHandlerService
{
    internal sealed class Factory : FactoryBase<IRemoteExtensionMessageHandlerService>
    {
        protected override IRemoteExtensionMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteExtensionMessageHandlerService(arguments);
    }

    public RemoteExtensionMessageHandlerService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<RegisterExtensionResponse> RegisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var workspace = this.GetWorkspace();
        var service = workspace.Services.GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            (_) =>
            {
                return service.RegisterExtensionAsync(
                    workspace,
                    assemblyFilePath,
                    cancellationToken);
            },
            cancellationToken);
    }

    public ValueTask<string> HandleExtensionDocumentMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            async solution =>
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
                return await service.HandleExtensionDocumentMessageAsync(
                    document,
                    messageName,
                    jsonMessage,
                    cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<string> HandleExensionWorkspaceMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            solution =>
            {
                var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
                return service.HandleExtensionWorkspaceMessageAsync(
                    solution,
                    messageName,
                    jsonMessage,
                    cancellationToken);
            },
            cancellationToken);
    }

    public ValueTask UnregisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            (_) => service.UnregisterExtensionAsync(
                assemblyFilePath,
                cancellationToken),
            cancellationToken);
    }

    public ValueTask ResetAsync(
        CancellationToken cancellationToken)
    {
        var service = this.GetWorkspace().Services.GetRequiredService<IExtensionMessageHandlerService>();
        return RunServiceAsync(
            (_) =>
            {
                service.Reset();
                return default;
            },
            cancellationToken);
    }
}
