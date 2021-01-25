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
    [ExportLspRequestHandlerProvider, Shared]
    [LspMethod(LSP.Methods.TextDocumentDidCloseName, mutatesSolutionState: true)]
    internal class DidCloseHandler : AbstractStatelessRequestHandler<LSP.DidCloseTextDocumentParams, object?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DidCloseHandler()
        {
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DidCloseTextDocumentParams request) => null;

        public override Task<object?> HandleRequestAsync(LSP.DidCloseTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
        {
            context.StopTracking(request.TextDocument.Uri);

            return SpecializedTasks.Default<object>();
        }
    }
}
