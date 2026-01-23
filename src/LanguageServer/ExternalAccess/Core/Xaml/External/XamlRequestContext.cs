// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Roslyn.LanguageServer.Protocol;

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

    public readonly LSP.ClientCapabilities ClientCapabilities => _context.GetRequiredClientCapabilities();

    public readonly TextDocument? TextDocument => _context.TextDocument;

    [Obsolete("Use ClientCapabilities instead.")]
    public readonly IClientCapabilityProvider ClientCapabilityProvider => new ClientCapabilityProvider(_context.GetRequiredClientCapabilities());

    public object ToCachedResolveData(object data, Uri uri)
    {
        var resolveDataCache = _context.GetRequiredLspService<ResolveDataCache>();

        return ResolveDataConversions.ToCachedResolveData(data, uri, resolveDataCache);
    }

    public (object? data, Uri? uri) FromCachedResolveData(object? lspData)
    {
        var resolveDataCache = _context.GetRequiredLspService<ResolveDataCache>();

        return ResolveDataConversions.FromCachedResolveData(lspData, resolveDataCache);
    }
}
