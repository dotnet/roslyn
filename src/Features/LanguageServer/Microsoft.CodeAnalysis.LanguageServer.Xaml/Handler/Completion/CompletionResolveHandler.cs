// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json.Linq;
using LSH = Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle a completion resolve request to add description.
/// </summary>
[XamlMethod(LSP.Methods.TextDocumentCompletionResolveName)]
internal class CompletionResolveHandler : XamlRequestHandlerBase<LSP.CompletionItem, LSP.CompletionItem>, ITextDocumentIdentifierHandler<LSP.CompletionItem, LSP.TextDocumentIdentifier>
{
    private readonly LSH.DocumentCache _documentCache;

    public CompletionResolveHandler(IXamlRequestHandler<LSP.CompletionItem, LSP.CompletionItem> xamlHandler, LSH.DocumentCache documentCache)
        : base(xamlHandler)
    {
        _documentCache = documentCache;
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CompletionItem request)
        => GetTextDocumentCacheEntry(request);

    private LSP.TextDocumentIdentifier GetTextDocumentCacheEntry(LSP.CompletionItem request)
    {
        var requestData = request.Data ?? throw new InvalidOperationException("Completion item data should not be null.");
        var resolveData = ((JToken)requestData).ToObject<DocumentIdResolveData>() ?? throw new InvalidOperationException($"Completion item data should be of type {nameof(DocumentIdResolveData)}.");
        var document = _documentCache.GetCachedEntry(resolveData.DocumentId) ?? throw new InvalidOperationException($"Completion item data document id should be in cache.");
        return document;
    }
}
