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
    [ExportXamlStatelessLspService(typeof(FormatDocumentHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentFormattingName)]
    internal class FormatDocumentHandler : XamlRequestHandlerBase<LSP.DocumentFormattingParams, LSP.TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.DocumentFormattingParams, LSP.TextEdit[]> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DocumentFormattingParams request) => request.TextDocument;
    }
}
