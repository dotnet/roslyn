// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(SignatureHelpHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentSignatureHelpName)]
    internal class SignatureHelpHandler : XamlRequestHandlerBase<LSP.TextDocumentPositionParams, LSP.SignatureHelp?>
    {
        [ImportingConstructor]
        [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
        public SignatureHelpHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.TextDocumentPositionParams, LSP.SignatureHelp?> xamlHandler)
        : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;
    }
}
