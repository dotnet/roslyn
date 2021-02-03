// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
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
            Contract.ThrowIfNull(context.Document);

            var text = await context.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Per the LSP spec, each text change builds upon the previous, so we don't need to translate
            // any text positions between changes, which makes this quite easy.
            var changes = request.ContentChanges.Select(change => ProtocolConversions.ContentChangeEventToTextChange(change, text));

            text = text.WithChanges(changes);

            context.UpdateTrackedDocument(request.TextDocument.Uri, text);

            return true;
        }
    }
}
