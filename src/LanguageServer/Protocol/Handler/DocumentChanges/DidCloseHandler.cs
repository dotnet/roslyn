// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

[ExportCSharpVisualBasicStatelessLspService(typeof(DidCloseHandler)), Shared]
[Method(LSP.Methods.TextDocumentDidCloseName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DidCloseHandler() : ILspServiceNotificationHandler<LSP.DidCloseTextDocumentParams>, ITextDocumentIdentifierHandler<LSP.DidCloseTextDocumentParams, TextDocumentIdentifier>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DidCloseTextDocumentParams request) => request.TextDocument;

    public async Task HandleNotificationAsync(LSP.DidCloseTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        // GetTextDocumentIdentifier returns null to avoid creating the solution, so the queue is not able to log the uri.
        context.TraceDebug($"didClose for {request.TextDocument.DocumentUri}");

        await context.StopTrackingAsync(request.TextDocument.DocumentUri, cancellationToken).ConfigureAwait(false);
    }
}
