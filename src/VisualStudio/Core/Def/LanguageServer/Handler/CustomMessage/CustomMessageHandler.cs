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
        var solution = context.Solution
            ?? throw new InvalidOperationException();
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        Optional<string> response;
        if (client is not null)
        {
            response = await client.TryInvokeAsync<IRemoteCustomMessageHandlerService, string>(
                solution,
                (service, solutionInfo, cancellationToken) => service.HandleCustomMessageAsync(
                    solutionInfo,
                    request.AssemblyPath,
                    request.TypeFullName,
                    request.Message,
                    documentId: null,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // TODO fix this
            throw new NotSupportedException("Custom message handlers are not supported.");
        }

        if (!response.HasValue)
        {
            throw new InvalidOperationException("The remote message handler didn't return any value.");
        }

        return new CustomResponse(response.Value);
    }
}
