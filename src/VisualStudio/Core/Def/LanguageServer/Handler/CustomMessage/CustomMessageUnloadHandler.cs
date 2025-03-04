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

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageUnloadHandler()
    : ILspServiceNotificationHandler<CustomMessageUnloadParams>
{
    private const string MethodName = "roslyn/customMessageUnload";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task HandleNotificationAsync(CustomMessageUnloadParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var solution = context.Solution
            ?? throw new InvalidOperationException();
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException();
        await client.TryInvokeAsync<IRemoteCustomMessageHandlerService>(
            solution,
            (service, solutionInfo, cancellationToken) => service.UnloadCustomMessageHandlerAsync(
                request.AssemblyPath,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
