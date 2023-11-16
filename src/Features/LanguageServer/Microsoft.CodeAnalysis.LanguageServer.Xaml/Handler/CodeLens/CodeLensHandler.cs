// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(CodeLensHandler)), Shared]
[XamlMethod(LSP.Methods.TextDocumentCodeLensName)]
internal sealed class CodeLensHandler : XamlRequestHandlerBase<LSP.CodeLensParams, LSP.CodeLens[]?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.CodeLensParams, LSP.CodeLens[]?> xamlHandler)
        : base(xamlHandler)
    {
    }


    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLensParams request)
        => request.TextDocument;
}

