// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(DidOpenHandler)), Shared]
    [Method(LSP.Methods.TextDocumentDidOpenName)]
    internal class DidOpenHandler : AbstractStatelessRequestHandler<LSP.DidOpenTextDocumentParams, object?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidOpenHandler()
        {
        }

        public override bool MutatesSolutionState => true;
        public override bool RequiresLSPSolution => false;

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidOpenTextDocumentParams request) => new() { Uri = request.TextDocument.Uri };

        public override Task<object?> HandleRequestAsync(LSP.DidOpenTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            // GetTextDocumentIdentifier returns null to avoid creating the solution, so the queue is not able to log the uri.
            context.TraceInformation($"didOpen for {request.TextDocument.Uri}");

            // Add the document and ensure the text we have matches whats on the client
            var sourceText = SourceText.From(request.TextDocument.Text, System.Text.Encoding.UTF8);

            context.StartTracking(request.TextDocument.Uri, sourceText);

            return SpecializedTasks.Default<object>();
        }
    }
}
