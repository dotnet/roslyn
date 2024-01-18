// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[RazorMethod(Methods.TextDocumentDidChangeName)]
[ExportRazorStatelessLspService(typeof(RazorCohostDidChangeEndpoint)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorCohostDidChangeEndpoint(
    [Import(AllowDefault = true)] IRazorCohostTextDocumentSyncHandler? razorDocSyncHandler)
    : ILspServiceDocumentRequestHandler<DidChangeTextDocumentParams, object?>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DidChangeTextDocumentParams request)
        => request.TextDocument;

    public async Task<object?> HandleRequestAsync(DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var text = context.GetTrackedDocumentSourceText(request.TextDocument.Uri);

        // Per the LSP spec, each text change builds upon the previous, so we don't need to translate any text
        // positions between changes, which makes this quite easy. See
        // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#didChangeTextDocumentParams
        // for more details.
        foreach (var change in request.ContentChanges)
            text = text.WithChanges(ProtocolConversions.ContentChangeEventToTextChange(change, text));

        context.UpdateTrackedDocument(request.TextDocument.Uri, text);

        // Razor can't handle this request because they don't have access to the RequestContext, but they might want to do something with it
        await razorDocSyncHandler.NotifyRazorAsync(request.TextDocument.Uri, request.TextDocument.Version, context, cancellationToken).ConfigureAwait(false);

        return null;
    }
}

[Obsolete("This API is made of regret, no longer functions, and will be removed very soon")]
internal interface IRazorCohostDidChangeHandler
{
    Task HandleAsync(Uri uri, int version, SourceText sourceText, CancellationToken cancellationToken);
}
