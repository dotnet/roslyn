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

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(DocumentSymbolsHandler)), Shared]
[XamlMethod(Methods.TextDocumentDocumentSymbolName)]
internal sealed class DocumentSymbolsHandler : ILspServiceDocumentRequestHandler<RoslynDocumentSymbolParams, object[]>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DocumentSymbolsHandler()
    {
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(RoslynDocumentSymbolParams request) => request.TextDocument;

    public Task<object[]> HandleRequestAsync(RoslynDocumentSymbolParams request, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Array.Empty<object>());
    }
}
