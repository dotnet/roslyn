// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

[ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionUnregisterHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionUnregisterHandler()
    : ILspServiceNotificationHandler<ExtensionUnregisterParams>
{
    private const string MethodName = "roslyn/extensionUnregister";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task HandleNotificationAsync(ExtensionUnregisterParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService>(
                solution,
                (service, solutionInfo, cancellationToken) => service.UnregisterExtensionAsync(
                    request.AssemblyFilePath,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
            await service.UnregisterExtensionAsync(
                    request.AssemblyFilePath,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}
