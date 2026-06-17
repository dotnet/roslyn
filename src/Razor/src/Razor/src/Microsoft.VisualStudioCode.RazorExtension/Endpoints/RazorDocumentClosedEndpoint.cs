// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

namespace Microsoft.VisualStudioCode.RazorExtension.Endpoints;

[Shared]
[ExportRazorStatelessLspService(typeof(RazorDocumentClosedEndpoint))]
[Method("razor/documentClosed")]
[method: ImportingConstructor]
internal class RazorDocumentClosedEndpoint(IHtmlDocumentSynchronizer htmlDocumentSynchronizer) : ILspServiceRequestHandler<TextDocumentIdentifier, VoidResult>, ITextDocumentIdentifierHandler<TextDocumentIdentifier, TextDocumentIdentifier?>
{
    private readonly IHtmlDocumentSynchronizer _htmlDocumentSynchronizer = htmlDocumentSynchronizer;

    bool IMethodHandler.MutatesSolutionState => false;

    bool ISolutionRequiredHandler.RequiresLSPSolution => true;

    TextDocumentIdentifier? ITextDocumentIdentifierHandler<TextDocumentIdentifier, TextDocumentIdentifier?>.GetTextDocumentIdentifier(TextDocumentIdentifier request)
        => request;

    public Task<VoidResult> HandleRequestAsync(TextDocumentIdentifier textDocument, RequestContext requestContext, CancellationToken cancellationToken)
    {
        // ParsedUri can be null when the URI string from the client isn't parseable by System.Uri.
        // This is safe to skip because HtmlDocumentSynchronizer only tracks documents that were
        // opened with a valid URI (via TrySynchronizeAsync), so there's no entry to remove.
        if (textDocument.DocumentUri.ParsedUri is Uri parsedUri)
        {
            _htmlDocumentSynchronizer.DocumentRemoved(parsedUri, cancellationToken);
        }

        return SpecializedTasks.Default<VoidResult>();
    }
}
