// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(DidCloseHandler)), Shared]
    [Method(LSP.Methods.TextDocumentDidCloseName)]
    internal class DidCloseHandler : ILspServiceNotificationHandler<LSP.DidCloseTextDocumentParams>, ITextDocumentIdentifierHandler<LSP.DidCloseTextDocumentParams, TextDocumentIdentifier>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidCloseHandler()
        {
        }

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => false;

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DidCloseTextDocumentParams request) => request.TextDocument;

        public Task HandleNotificationAsync(LSP.DidCloseTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            // GetTextDocumentIdentifier returns null to avoid creating the solution, so the queue is not able to log the uri.
            context.TraceInformation($"didClose for {request.TextDocument.Uri}");

            context.StopTracking(request.TextDocument.Uri);

            return Task.CompletedTask;
        }
    }
}
