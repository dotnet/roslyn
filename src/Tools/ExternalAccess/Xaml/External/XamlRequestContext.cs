// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal struct XamlRequestContext
{
    private readonly RequestContext _context;

    public static XamlRequestContext FromRequestContext(RequestContext context)
        => new(context);

    private XamlRequestContext(RequestContext context)
    {
        _context = context;
    }

    public TextDocument? TextDocument => _context.TextDocument;

    public LSP.ClientCapabilities ClientCapabilities => _context.GetRequiredClientCapabilities();

    public object ToCachedResolveData(object data, LSP.TextDocumentIdentifier document)
    {
        var resolveDataCache = _context.GetRequiredLspService<ResolveDataCache>();

        return ResolveDataConversions.ToCachedResolveData(data, document, resolveDataCache);
    }

    public (object? data, LSP.TextDocumentIdentifier? document) FromCachedResolveData(object? lspData)
    {
        var resolveDataCache = _context.GetRequiredLspService<ResolveDataCache>();

        return ResolveDataConversions.FromCachedResolveData(lspData, resolveDataCache);
    }
}
