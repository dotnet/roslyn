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

[ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionWorkspaceMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionWorkspaceMessageHandler()
    : ILspServiceRequestHandler<ExtensionWorkspaceMessageParams, ExtensionMessageResponse>
{
    private const string MethodName = "roslyn/extensionWorkspaceMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<ExtensionMessageResponse> HandleRequestAsync(ExtensionWorkspaceMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            var response = await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, string>(
                solution,
                (service, solutionInfo, cancellationToken) => service.HandleExensionWorkspaceMessageAsync(
                    solutionInfo,
                    request.MessageName,
                    request.Message,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!response.HasValue)
            {
                throw new InvalidOperationException("The remote message handler didn't return any value.");
            }

            return new ExtensionMessageResponse(response.Value);
        }
        else
        {
            var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
            var response = await service.HandleExtensionWorkspaceMessageAsync(
                    solution,
                    request.MessageName,
                    request.Message,
                    cancellationToken).ConfigureAwait(false);

            return new ExtensionMessageResponse(response);
        }
    }
}
