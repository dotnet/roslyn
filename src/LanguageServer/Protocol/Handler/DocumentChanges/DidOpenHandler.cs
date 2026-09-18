// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

[ExportCSharpVisualBasicStatelessLspService(typeof(DidOpenHandler)), Shared]
[Method(LSP.Methods.TextDocumentDidOpenName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DidOpenHandler() : ILspServiceNotificationHandler<LSP.DidOpenTextDocumentParams>, ITextDocumentIdentifierHandler<LSP.DidOpenTextDocumentParams, TextDocumentItem>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public TextDocumentItem GetTextDocumentIdentifier(LSP.DidOpenTextDocumentParams request) => request.TextDocument;

    public async Task HandleNotificationAsync(LSP.DidOpenTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        // GetTextDocumentIdentifier returns null to avoid creating the solution, so the queue is not able to log the uri.
        context.TraceDebug($"didOpen for {request.TextDocument.DocumentUri}");

        SourceText sourceText;
        if (request.TextDocument.Text is string text)
        {
            // Add the document and ensure the text we have matches whats on the client
            // TODO (https://github.com/dotnet/roslyn/issues/63583):
            // Create SourceText from binary representation of the document, retrieve encoding from the request and checksum algorithm from the project.
            sourceText = SourceText.From(text, System.Text.Encoding.UTF8, SourceHashAlgorithms.OpenDocumentChecksumAlgorithm);
        }
        else
        {
            var workspaceManager = context.GetRequiredLspService<LspWorkspaceManager>();
            var (_, _, document) = await workspaceManager.GetLspDocumentInfoAsync(
                new LSP.TextDocumentIdentifier { DocumentUri = request.TextDocument.DocumentUri }, cancellationToken).ConfigureAwait(false);

            sourceText = document is null
                ? throw new InvalidOperationException($"Unable to find document '{request.TextDocument.DocumentUri}' after receiving a didOpen notification without text.")
                : await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        }

        await context.StartTrackingAsync(request.TextDocument.DocumentUri, sourceText, request.TextDocument.LanguageId, request.TextDocument.Version, cancellationToken).ConfigureAwait(false);
    }
}
