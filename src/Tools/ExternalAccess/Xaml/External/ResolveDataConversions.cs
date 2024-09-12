// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class ResolveDataConversions
{
    private record DataResolveData(object Data, LSP.TextDocumentIdentifier Document) : DocumentResolveData(Document);
    private record DataIdResolveData(long DataId, LSP.TextDocumentIdentifier Document) : DocumentResolveData(Document);

    public static object ToResolveData(object data, Uri uri)
        => new DataResolveData(data, new LSP.TextDocumentIdentifier { Uri = uri });

    public static (object? data, Uri? uri) FromResolveData(object? requestData)
    {
        Contract.ThrowIfNull(requestData);
        var resolveData = ((JToken)requestData).ToObject<DataResolveData>();
        return (resolveData?.Data, resolveData?.Document.Uri);
    }

    internal static object ToCachedResolveData(object data, Uri uri, ResolveDataCache resolveDataCache)
    {
        var dataId = resolveDataCache.UpdateCache(data);

        return new DataIdResolveData(dataId, new LSP.TextDocumentIdentifier { Uri = uri });
    }

    internal static (object? data, Uri? uri) FromCachedResolveData(object? lspData, ResolveDataCache resolveDataCache)
    {
        DataIdResolveData? resolveData;
        if (lspData is JToken token)
        {
            resolveData = token.ToObject<DataIdResolveData>();
            Assumes.Present(resolveData);
        }
        else
        {
            return (null, null);
        }

        var data = resolveDataCache.GetCachedEntry(resolveData.DataId);
        var document = resolveData.Document;

        return (data, document.Uri);
    }
}
