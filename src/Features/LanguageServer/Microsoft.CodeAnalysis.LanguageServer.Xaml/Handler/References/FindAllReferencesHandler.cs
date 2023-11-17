// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(FindAllReferencesHandler)), Shared]
    [XamlMethod(LSP.Methods.TextDocumentReferencesName)]
    internal sealed class FindAllReferencesHandler : XamlRequestHandlerBase<LSP.ReferenceParams, LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?>
    {
        [ImportingConstructor]
        [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.ReferenceParams, LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]?> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.ReferenceParams request) => request.TextDocument;
    }
}
