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
internal class CustomMessageHandler()
    : ILspServiceRequestHandler<CustomMessageParams, CustomResponse>
{
    private const string MethodName = "roslyn/customMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<CustomResponse> HandleRequestAsync(CustomMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        var solution = context.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            var response = await client.TryInvokeAsync<IRemoteCustomMessageHandlerService, string>(
                solution,
                (service, solutionInfo, cancellationToken) => service.HandleCustomMessageAsync(
                    solutionInfo,
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    request.TypeFullName,
                    request.Message,
                    documentId: null,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!response.HasValue)
            {
                throw new InvalidOperationException("The remote message handler didn't return any value.");
            }

            return new CustomResponse(response.Value);
        }
        else
        {
            var service = context.Workspace!.Services.GetRequiredService<ICustomMessageHandlerService>();
            var response = await service.HandleCustomMessageAsync(
                    solution,
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    request.TypeFullName,
                    request.Message,
                    documentId: null,
                    cancellationToken).ConfigureAwait(false);

            return new CustomResponse(response);
        }
    }
}
