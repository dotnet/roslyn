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

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(GoToTypeDefinitionHandler)), Shared]
[XamlMethod(LSP.Methods.TextDocumentTypeDefinitionName)]
internal class GoToTypeDefinitionHandler : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, LSP.Location[]>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public GoToTypeDefinitionHandler()
    {
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

    public Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Array.Empty<LSP.Location>());
    }
}
