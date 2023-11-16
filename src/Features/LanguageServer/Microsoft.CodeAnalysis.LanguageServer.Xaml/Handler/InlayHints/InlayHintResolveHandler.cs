// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(InlayHintResolveHandler)), Shared]
[XamlMethod(LSP.Methods.InlayHintResolveName)]
internal sealed class InlayHintResolveHandler : XamlRequestHandlerBase<LSP.InlayHint, LSP.InlayHint>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlayHintResolveHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.InlayHint, LSP.InlayHint> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.InlayHint request)
        => ProtocolConversions.GetTextDocument(request.Data) ?? throw new ArgumentException($"Expected resolve data to derive from {nameof(DocumentResolveData)}");
}
