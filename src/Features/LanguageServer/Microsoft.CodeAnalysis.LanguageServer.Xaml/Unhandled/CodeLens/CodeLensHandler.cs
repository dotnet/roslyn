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

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(CodeLensHandler)), Shared]
[XamlMethod(LSP.Methods.TextDocumentCodeLensName)]
internal sealed class CodeLensHandler : ILspServiceDocumentRequestHandler<LSP.CodeLensParams, LSP.CodeLens[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLensParams request)
        => request.TextDocument;

    public Task<LSP.CodeLens[]?> HandleRequestAsync(LSP.CodeLensParams request, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<LSP.CodeLens[]?>(null);
    }
}

