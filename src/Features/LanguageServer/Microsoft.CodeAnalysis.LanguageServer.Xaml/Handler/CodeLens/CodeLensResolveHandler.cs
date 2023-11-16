// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(CodeLensResolveHandler)), Shared]
[XamlMethod(LSP.Methods.CodeLensResolveName)]
internal sealed class CodeLensResolveHandler : XamlRequestHandlerBase<LSP.CodeLens, LSP.CodeLens>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensResolveHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.CodeLens, LSP.CodeLens> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLens request)
        => ProtocolConversions.GetTextDocument(request.Data) ?? throw new ArgumentException($"Expected resolve data to derive from {nameof(DocumentResolveData)}");
}

