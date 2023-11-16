// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(DocumentHighlightsHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentDocumentHighlightName)]
    internal class DocumentHighlightsHandler : XamlRequestHandlerBase<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentHighlightsHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]?> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;
    }
}
