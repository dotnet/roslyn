// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageHandler()
    : ILspServiceDocumentRequestHandler<CustomMessageParams, CustomResponse>
{
    private const string MethodName = "roslyn/customMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CustomMessageParams request)
    {
        return request.Message.TextDocument;
    }

    public async Task<CustomResponse> HandleRequestAsync(CustomMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var project = context.Document?.Project
            ?? throw new InvalidOperationException();
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException();
        var requestLinePositions = request.Message.Positions.Select(tdp => ProtocolConversions.PositionToLinePosition(tdp)).ToArray();
        var jsonMessage = request.Message.Message.ToJsonString();
        var response = await client.TryInvokeAsync<IRemoteCustomMessageHandlerService, HandleCustomMessageResponse>(
            project,
            (service, solutionInfo, cancellationToken) => service.HandleCustomMessageAsync(
                solutionInfo,
                request.AssemblyPath,
                request.TypeFullName,
                jsonMessage,
                context.Document.Id,
                requestLinePositions,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (!response.HasValue)
        {
            throw new InvalidOperationException("The remote message handler didn't return any value.");
        }

        var responsePositions = response.Value.Positions
            .Select(p => new Position(p.Line, p.Character))
            .ToArray();
        return new CustomResponse(JsonNode.Parse(response.Value.Message)!, responsePositions);
    }
}
