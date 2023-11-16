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
    [ExportXamlStatelessLspService(typeof(FormatDocumentOnTypeHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentOnTypeFormattingName)]
    internal class FormatDocumentOnTypeHandler : XamlRequestHandlerBase<LSP.DocumentOnTypeFormattingParams, LSP.TextEdit[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FormatDocumentOnTypeHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.DocumentOnTypeFormattingParams, LSP.TextEdit[]> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.DocumentOnTypeFormattingParams request) => request.TextDocument;
    }
}
