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
    [ExportXamlStatelessLspService(typeof(FindAllReferencesHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentReferencesName)]
    internal sealed class FindAllReferencesHandler : ILspServiceDocumentRequestHandler<LSP.ReferenceParams, LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(ReferenceParams request) => request.TextDocument;

        public Task<LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?> HandleRequestAsync(
            ReferenceParams referenceParams,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<SumType<VSInternalReferenceItem, LSP.Location>[]?>(null);
        }
    }
}
