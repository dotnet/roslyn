// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// The <see cref="IResolveCachedDataService"/> of an LSP server, backed by that server's <see cref="ResolveDataCache"/>.
/// </summary>
[Export(typeof(IResolveCachedDataService)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
[method: Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
internal sealed class ResolveCachedDataService(LspService<ResolveDataCache> resolveDataCache) : IResolveCachedDataService
{
    [Obsolete("Use overload that takes a DocumentUri instead of Uri. This method will be removed in a future version. Tracking: https://github.com/dotnet/roslyn/issues/84785")]
    public object ToResolveData(object data, Uri uri)
        => ResolveDataConversions.ToCachedResolveData(data, new(uri), resolveDataCache.Value);

    [Obsolete("Use FromResolveDataDocumentUri instead. This method will be removed in a future version. Tracking: https://github.com/dotnet/roslyn/issues/84785")]
    public (object? data, Uri? uri) FromResolveData(object? lspData)
    {
        var (data, documentUri) = ResolveDataConversions.FromCachedResolveData(lspData, resolveDataCache.Value);
        return (data, documentUri?.ParsedUri);
    }

    public object ToResolveData(object data, DocumentUri uri)
        => ResolveDataConversions.ToCachedResolveData(data, uri, resolveDataCache.Value);

    public (object? data, DocumentUri? uri) FromResolveDataDocumentUri(object? resolveData)
        => ResolveDataConversions.FromCachedResolveData(resolveData, resolveDataCache.Value);
}
