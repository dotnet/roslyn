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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(InlayHintHandler)), Shared]
    [XamlMethod(Methods.TextDocumentInlayHintName)]
    internal sealed class InlayHintHandler : ILspServiceDocumentRequestHandler<InlayHintParams, LSP.InlayHint[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintHandler()
        {
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(InlayHintParams request)
            => request.TextDocument;

        public Task<LSP.InlayHint[]?> HandleRequestAsync(InlayHintParams request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<InlayHint[]?>(null);
        }
    }
}

