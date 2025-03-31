// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageUnregisterHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageUnregisterHandler()
    : ILspServiceNotificationHandler<CustomMessageUnregisterParams>
{
    private const string MethodName = "roslyn/customMessageUnload";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task HandleNotificationAsync(CustomMessageUnregisterParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            await client.TryInvokeAsync<IRemoteCustomMessageHandlerService>(
                solution,
                (service, solutionInfo, cancellationToken) => service.UnregisterCustomMessageHandlersAsync(
                    request.AssemblyFilePath,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var service = solution.Services.GetRequiredService<ICustomMessageHandlerService>();
            await service.UnregisterCustomMessageHandlersAsync(
                    request.AssemblyFilePath,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}
