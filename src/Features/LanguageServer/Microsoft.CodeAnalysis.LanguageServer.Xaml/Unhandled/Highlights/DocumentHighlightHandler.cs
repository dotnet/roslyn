// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(DocumentHighlightsHandler)), Shared]
    [XamlMethod(Methods.TextDocumentDocumentHighlightName)]
    internal class DocumentHighlightsHandler : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, DocumentHighlight[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentHighlightsHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public Task<DocumentHighlight[]?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<DocumentHighlight[]?>(null);
        }
    }
}
