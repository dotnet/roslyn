// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using System.Composition;
using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(CodeLensResolveHandler)), Shared]
[XamlMethod(LSP.Methods.CodeLensResolveName)]
internal sealed class CodeLensResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeLens, LSP.CodeLens>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensResolveHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLens request)
        => ProtocolConversions.GetTextDocument(request.Data) ?? throw new ArgumentException();

    public Task<LSP.CodeLens> HandleRequestAsync(LSP.CodeLens request, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }
}

