// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle a completion request.
/// </summary>
[ExportXamlStatelessLspService(typeof(CompletionHandler)), Shared]
[XamlMethod(LSP.Methods.TextDocumentCompletionName)]
internal class CompletionHandler : XamlRequestHandlerBase<LSP.CompletionParams, LSP.CompletionList?>
{
    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public CompletionHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.CompletionParams, LSP.CompletionList?> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;
}
