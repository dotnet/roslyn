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

[ExportXamlStatelessLspService(typeof(DocumentSymbolsHandler)), Shared]
[XamlMethod(LSP.Methods.TextDocumentDocumentSymbolName)]
internal sealed class DocumentSymbolsHandler : XamlRequestHandlerBase<RoslynDocumentSymbolParams, object[]>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DocumentSymbolsHandler([Import(AllowDefault = true)] IXamlRequestHandler<RoslynDocumentSymbolParams, object[]> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(RoslynDocumentSymbolParams request) => request.TextDocument;
}
