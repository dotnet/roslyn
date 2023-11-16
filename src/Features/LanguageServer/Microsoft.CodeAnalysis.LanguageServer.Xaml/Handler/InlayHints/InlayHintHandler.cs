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
    [ExportXamlStatelessLspService(typeof(InlayHintHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentInlayHintName)]
    internal sealed class InlayHintHandler : XamlRequestHandlerBase<LSP.InlayHintParams, LSP.InlayHint[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.InlayHintParams, LSP.InlayHint[]?> xamlHandler)
        : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.InlayHintParams request)
            => request.TextDocument;
    }
}

