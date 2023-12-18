// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[LanguageServerEndpoint(Methods.TextDocumentDidCloseName)]
[ExportRazorStatelessLspService(typeof(RazorCohostDidCloseEndpoint)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorCohostDidCloseEndpoint([Import(AllowDefault = true)] IRazorCohostDidCloseHandler? didCloseHandler) : ILspServiceNotificationHandler<DidCloseTextDocumentParams>, ITextDocumentIdentifierHandler<DidCloseTextDocumentParams, TextDocumentIdentifier>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DidCloseTextDocumentParams request)
        => request.TextDocument;

    public async Task HandleNotificationAsync(DidCloseTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        context.TraceInformation($"didClose for {request.TextDocument.Uri}");

        await context.StopTrackingAsync(request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);

        // Razor can't handle this request because they don't have access to the RequestContext, but they might want to do something with it
        if (didCloseHandler is not null)
        {
            await didCloseHandler.HandleAsync(request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal interface IRazorCohostDidCloseHandler
{
    Task HandleAsync(Uri uri, CancellationToken cancellationToken);
}
