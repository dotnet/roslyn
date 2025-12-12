// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

[ExportCSharpVisualBasicStatelessLspService(typeof(DispatchDocumentExtensionMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DispatchDocumentExtensionMessageHandler()
    : AbstractExtensionHandler, ILspServiceDocumentRequestHandler<DispatchDocumentExtensionMessageParams, DispatchExtensionMessageResponse>
{
    private const string MethodName = "textDocument/_vs_dipatchExtensionMessage";

    public TextDocumentIdentifier GetTextDocumentIdentifier(DispatchDocumentExtensionMessageParams request)
        => request.TextDocument;

    public async Task<DispatchExtensionMessageResponse> HandleRequestAsync(DispatchDocumentExtensionMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Document);

        var solution = context.Document.Project.Solution;

        var service = solution.Services.GetRequiredService<IExtensionMessageHandlerService>();
        var (response, extensionWasUnloaded, exception) = await service.HandleExtensionDocumentMessageAsync(
            context.Document, request.MessageName, request.Message, cancellationToken).ConfigureAwait(false);

        // Report any exceptions the extension itself caused while handling the request.
        if (exception is not null)
            context.Logger.LogException(exception);

        return new DispatchExtensionMessageResponse(response, extensionWasUnloaded, exception);
    }
}
