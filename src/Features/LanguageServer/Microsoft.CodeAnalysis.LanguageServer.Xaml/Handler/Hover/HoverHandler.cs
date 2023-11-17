// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(HoverHandler)), Shared]
[XamlMethod(Methods.TextDocumentHoverName)]
internal sealed class HoverHandler : XamlRequestHandlerBase<TextDocumentPositionParams, Hover?>
{
    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public HoverHandler([Import(AllowDefault = true)] IXamlRequestHandler<TextDocumentPositionParams, Hover?> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;
}
