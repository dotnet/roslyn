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

/// <summary>
/// Supports built in legacy snippets for razor scenarios.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(InlineCompletionsHandler)), Shared]
[XamlMethod(LSP.VSInternalMethods.TextDocumentInlineCompletionName)]
internal class InlineCompletionsHandler : XamlRequestHandlerBase<LSP.VSInternalInlineCompletionRequest, LSP.VSInternalInlineCompletionList?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineCompletionsHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.VSInternalInlineCompletionRequest, LSP.VSInternalInlineCompletionList?> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.VSInternalInlineCompletionRequest request) => request.TextDocument;
}
