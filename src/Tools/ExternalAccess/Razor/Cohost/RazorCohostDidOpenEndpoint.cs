// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[LanguageServerEndpoint(Methods.TextDocumentDidOpenName)]
[ExportRazorStatelessLspService(typeof(RazorCohostDidOpenEndpoint)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorCohostDidOpenEndpoint(
    [Import(AllowDefault = true)] IRazorCohostDidOpenHandler? didOpenHandler)
    : ILspServiceNotificationHandler<DidOpenTextDocumentParams>, ITextDocumentIdentifierHandler<DidOpenTextDocumentParams, Uri>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public Uri GetTextDocumentIdentifier(DidOpenTextDocumentParams request)
        => request.TextDocument.Uri;

    public async Task HandleNotificationAsync(DidOpenTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        context.TraceInformation($"didOpen for {request.TextDocument.Uri}");

        var sourceText = SourceText.From(request.TextDocument.Text, System.Text.Encoding.UTF8, SourceHashAlgorithms.OpenDocumentChecksumAlgorithm);

        await context.StartTrackingAsync(request.TextDocument.Uri, sourceText, request.TextDocument.LanguageId, cancellationToken).ConfigureAwait(false);

        // Razor can't handle this request because they don't have access to the RequestContext, but they might want to do something with it
        if (didOpenHandler is not null)
        {
            await didOpenHandler.HandleAsync(request.TextDocument.Uri, request.TextDocument.Version, sourceText, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal interface IRazorCohostDidOpenHandler
{
    Task HandleAsync(Uri uri, int version, SourceText sourceText, CancellationToken cancellationToken);
}
