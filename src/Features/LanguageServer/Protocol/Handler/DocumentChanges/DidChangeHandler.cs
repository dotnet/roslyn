// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentDidChangeName, mutatesSolutionState: true)]
    internal class DidChangeHandler : IRequestHandler<LSP.DidChangeTextDocumentParams, object>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidChangeHandler()
        {
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidChangeTextDocumentParams request) => request.TextDocument;

        public async Task<object> HandleRequestAsync(LSP.DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var change in request.ContentChanges)
            {
                // We very deliberately do not just call doc.ApplyTextChanges here, because that updates the workspace
                var textChange = ProtocolConversions.ContentChangeEventToTextChange(change, text);
                // Per the LSP spec, each text change builds upon the previous, so we don't need to translate
                // any text positions here.
                text = text.WithChanges(textChange);
            }

            document = document.WithText(text);

            context.UpdateTrackedDocument(document);

            return true;
        }
    }
}
