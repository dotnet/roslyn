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
    [ExportXamlStatelessLspService(typeof(SemanticTokensRangeHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : XamlRequestHandlerBase<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens> xamlHandler)
        : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request) => request.TextDocument;
    }
}
