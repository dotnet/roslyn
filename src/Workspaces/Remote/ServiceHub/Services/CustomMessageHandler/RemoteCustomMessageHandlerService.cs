// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteCustomMessageHandlerService : BrokeredServiceBase, IRemoteCustomMessageHandlerService
{
    private readonly ICustomMessageHandlerService _customMessageHandlerService;

    internal sealed class Factory : FactoryBase<IRemoteCustomMessageHandlerService>
    {
        protected override IRemoteCustomMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCustomMessageHandlerService(arguments);
    }

    public RemoteCustomMessageHandlerService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
        // TODO get the MEF-exported ICustomMessageHandlerService
        _customMessageHandlerService = null!;
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
        return RunServiceAsync(
            solutionChecksum,
            solution => _customMessageHandlerService.HandleCustomMessageAsync(
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
        return RunServiceAsync(
            (_) => _customMessageHandlerService.UnloadCustomMessageHandlersAsync(
                assemblyFolderPath,
                cancellationToken),
            cancellationToken);
    }
}
