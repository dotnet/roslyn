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
    [ExportXamlStatelessLspService(typeof(RenameHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentRenameName)]
    internal class RenameHandler : XamlRequestHandlerBase<LSP.RenameParams, LSP.WorkspaceEdit?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.RenameParams, LSP.WorkspaceEdit?> xamlHandler)
        : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.RenameParams request) => request.TextDocument;
    }
}
