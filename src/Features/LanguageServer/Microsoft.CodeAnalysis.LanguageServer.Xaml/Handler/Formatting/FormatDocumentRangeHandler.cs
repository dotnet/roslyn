// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(FormatDocumentRangeHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentRangeFormattingName)]
    internal class FormatDocumentRangeHandler : XamlRequestHandlerBase<LSP.DocumentRangeFormattingParams, LSP.TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
        public FormatDocumentRangeHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.DocumentRangeFormattingParams, LSP.TextEdit[]> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DocumentRangeFormattingParams request) => request.TextDocument;
    }
}
