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
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(DidChangeHandler)), Shared]
    [Method(LSP.Methods.TextDocumentDidChangeName)]
    internal class DidChangeHandler : AbstractStatelessRequestHandler<LSP.DidChangeTextDocumentParams, object?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidChangeHandler()
        {
        }

        public override bool MutatesSolutionState => true;
        public override bool RequiresLSPSolution => false;

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidChangeTextDocumentParams request) => request.TextDocument;

        public override Task<object?> HandleRequestAsync(LSP.DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var text = context.GetTrackedDocumentSourceText(request.TextDocument.Uri);

            // Per the LSP spec, each text change builds upon the previous, so we don't need to translate
            // any text positions between changes, which makes this quite easy.
            var changes = request.ContentChanges.Select(change => ProtocolConversions.ContentChangeEventToTextChange(change, text));

            text = text.WithChanges(changes);

            context.UpdateTrackedDocument(request.TextDocument.Uri, text);

            return SpecializedTasks.Default<object>();
        }
    }
}
