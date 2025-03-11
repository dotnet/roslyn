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
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageDocumentHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageDocumentHandler()
    : ILspServiceDocumentRequestHandler<CustomMessageDocumentParams, CustomResponse>
{
    private const string MethodName = "roslyn/customDocumentMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CustomMessageDocumentParams request)
    {
        return request.TextDocument;
    }

    public async Task<CustomResponse> HandleRequestAsync(CustomMessageDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var project = context.Document?.Project
            ?? throw new InvalidOperationException();
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);

        if (client is not null)
        {
            var response = await client.TryInvokeAsync<IRemoteCustomMessageHandlerService, string>(
                project,
                (service, solutionInfo, cancellationToken) => service.HandleCustomMessageAsync(
                    solutionInfo,
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    request.TypeFullName,
                    request.Message,
                    context.Document.Id,
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
#if NETSTANDARD2_0
            throw new InvalidOperationException("Custom handlers are not supported");
#else
            var service = CustomMessageHandlerService.Instance.Value;
            var response = await service.HandleCustomMessageAsync(
                    project.Solution,
                    request.AssemblyFolderPath,
                    request.AssemblyFileName,
                    request.TypeFullName,
                    request.Message,
                    context.Document.Id,
                    cancellationToken).ConfigureAwait(false);

            return new CustomResponse(response);
#endif
        }
    }
}
