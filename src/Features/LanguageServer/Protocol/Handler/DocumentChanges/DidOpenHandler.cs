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
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(DidOpenHandler)), Shared]
    [Method(LSP.Methods.TextDocumentDidOpenName)]
    internal class DidOpenHandler : ILspServiceNotificationHandler<LSP.DidOpenTextDocumentParams>, ITextDocumentIdentifierHandler<LSP.DidOpenTextDocumentParams, Uri>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidOpenHandler()
        {
        }

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => false;

        public Uri GetTextDocumentIdentifier(LSP.DidOpenTextDocumentParams request) => request.TextDocument.Uri;

        public async Task HandleNotificationAsync(LSP.DidOpenTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            // GetTextDocumentIdentifier returns null to avoid creating the solution, so the queue is not able to log the uri.
            context.TraceInformation($"didOpen for {request.TextDocument.Uri}");

            // Add the document and ensure the text we have matches whats on the client
            // TODO (https://github.com/dotnet/roslyn/issues/63583):
            // Create SourceText from binary representation of the document, retrieve encoding from the request and checksum algorithm from the project.
            var sourceText = SourceText.From(request.TextDocument.Text, System.Text.Encoding.UTF8, SourceHashAlgorithms.OpenDocumentChecksumAlgorithm);

            await context.StartTrackingAsync(request.TextDocument.Uri, sourceText, request.TextDocument.LanguageId, cancellationToken).ConfigureAwait(false);
        }
    }
}
