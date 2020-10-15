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
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidChangeHandler(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidChangeTextDocumentParams request) => request.TextDocument;

        public async Task<object> HandleRequestAsync(LSP.DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var documents = _solutionProvider.GetDocuments(request.TextDocument.Uri, context.ClientName);
            Contract.ThrowIfTrue(documents.IsEmpty);

            // We can't get the text from the documents that come from the solution provider, above, because
            // they have not got the latest content. Only the document that comes from the RequestContext
            // has the right text according to the LSP view of the world.
            // TODO: Remove this after https://github.com/dotnet/roslyn/issues/48617 is fixed
            Contract.ThrowIfNull(context.Document);
            var text = await context.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Per the LSP spec, each text change builds upon the previous, so we don't need to translate
            // any text positions between changes, which makes this quite easy.
            var changes = request.ContentChanges.Select(change => ProtocolConversions.ContentChangeEventToTextChange(change, text));

            text = text.WithChanges(changes);

            foreach (var document in documents)
            {
                context.UpdateTrackedDocument(document, text);
            }

            return true;
        }
    }
}
