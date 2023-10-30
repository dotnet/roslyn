// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(FindImplementationsHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentImplementationName)]
    internal sealed class FindImplementationsHandler : ILspServiceDocumentRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindImplementationsHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

        public Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<LSP.Location>());
        }
    }
}
