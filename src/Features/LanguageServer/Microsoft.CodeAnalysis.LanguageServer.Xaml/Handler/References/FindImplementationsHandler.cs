// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(FindImplementationsHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentImplementationName)]
    internal sealed class FindImplementationsHandler : XamlRequestHandlerBase<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        [System.Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
        public FindImplementationsHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]> xamlHandler)
        : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;
    }
}
