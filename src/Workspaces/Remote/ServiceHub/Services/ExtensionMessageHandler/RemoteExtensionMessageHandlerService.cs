// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;

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

    private IExtensionMessageHandlerService GetExtensionService()
        => this.GetWorkspaceServices().GetRequiredService<IExtensionMessageHandlerService>();

    public ValueTask<VoidResult> RegisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
        => RunServiceAsync(
            async cancellationToken =>
            {
                await GetExtensionService().RegisterExtensionAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);
                return default(VoidResult);
            }, cancellationToken);

    public ValueTask<VoidResult> UnregisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
        => RunServiceAsync(
            async cancellationToken =>
            {
                await GetExtensionService().UnregisterExtensionAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);
                return default(VoidResult);
            }, cancellationToken);

    public ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
        => RunServiceAsync(
            cancellationToken => GetExtensionService().GetExtensionMessageNamesAsync(assemblyFilePath, cancellationToken),
            cancellationToken);

    public ValueTask<string> HandleExtensionDocumentMessageAsync(
        Checksum solutionChecksum, string messageName, string jsonMessage, DocumentId documentId, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionChecksum,
            async solution =>
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                return await GetExtensionService().HandleExtensionDocumentMessageAsync(document, messageName, jsonMessage, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);

    public ValueTask<string> HandleExtensionWorkspaceMessageAsync(
        Checksum solutionChecksum, string messageName, string jsonMessage, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionChecksum,
            solution => GetExtensionService().HandleExtensionWorkspaceMessageAsync(solution, messageName, jsonMessage, cancellationToken),
            cancellationToken);

    public ValueTask<VoidResult> ResetAsync(CancellationToken cancellationToken)
        => RunServiceAsync(
            async cancellationToken =>
            {
                await GetExtensionService().ResetAsync(cancellationToken).ConfigureAwait(false);
                return default(VoidResult);
            },
            cancellationToken);
}
