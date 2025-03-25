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

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageLoadHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageLoadHandler()
    : ILspServiceRequestHandler<CustomMessageLoadParams, CustomMessageLoadResponse>
{
    private const string MethodName = "roslyn/customMessageLoad";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<CustomMessageLoadResponse> HandleRequestAsync(CustomMessageLoadParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            var response = await client.TryInvokeAsync<IRemoteCustomMessageHandlerService, RegisterHandlersResponse>(
                solution,
                (service, solutionInfo, cancellationToken) => service.LoadCustomMessageHandlersAsync(
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!response.HasValue)
            {
                throw new InvalidOperationException("The remote message handler didn't return any value.");
            }

            return new(response.Value.Handlers, response.Value.DocumentHandlers);
        }
        else
        {
            var service = solution.Services.GetRequiredService<ICustomMessageHandlerService>();
            var response = await service.LoadCustomMessageHandlersAsync(
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    cancellationToken).ConfigureAwait(false);

            return new(response.Handlers, response.DocumentHandlers);
        }
    }
}
