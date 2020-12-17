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
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentDidOpenName, mutatesSolutionState: true)]
    internal class DidOpenHandler : IRequestHandler<LSP.DidOpenTextDocumentParams, object?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidOpenHandler()
        {
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidOpenTextDocumentParams request) => null;

        public Task<object?> HandleRequestAsync(LSP.DidOpenTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            // Add the document and ensure the text we have matches whats on the client
            var sourceText = SourceText.From(request.TextDocument.Text, System.Text.Encoding.UTF8);

            context.StartTracking(request.TextDocument.Uri, sourceText);

            return SpecializedTasks.Default<object>();
        }
    }
}
