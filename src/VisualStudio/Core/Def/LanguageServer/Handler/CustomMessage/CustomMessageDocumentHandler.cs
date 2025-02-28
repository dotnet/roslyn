// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageDocumentHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageDocumentHandler()
    : ILspServiceDocumentRequestHandler<CustomMessageDocumentParams, CustomDocumentResponse>
{
    private const string MethodName = "roslyn/customDocumentMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CustomMessageDocumentParams request)
    {
        return request.TextDocument;
    }

    public async Task<CustomDocumentResponse> HandleRequestAsync(CustomMessageDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var project = context.Document?.Project
            ?? throw new InvalidOperationException();
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException();
        var requestLinePositions = request.Positions.Select(tdp => ProtocolConversions.PositionToLinePosition(tdp)).ToImmutableArray();
        var jsonMessage = request.Message.ToJsonString();
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
        return new CustomDocumentResponse(JsonNode.Parse(response.Value.Response)!, responsePositions);
    }
}
