// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal static class ResolveDataConversions
{
    private record DataResolveData(object Data, LSP.TextDocumentIdentifier Document) : DocumentResolveData(Document);
    private record DataIdResolveData(long DataId, long DocumentId) : DocumentIdResolveData(DocumentId);

    public static object ToResolveData(object data, LSP.TextDocumentIdentifier document)
        => new DataResolveData(data, document);

    public static (object? data, LSP.TextDocumentIdentifier? document) FromResolveData(object? requestData)
    {
        Contract.ThrowIfNull(requestData);
        var resolveData = ((JToken)requestData).ToObject<DataResolveData>();
        return (resolveData?.Data, resolveData?.Document);
    }

    internal static object ToCachedResolveData(object data, LSP.TextDocumentIdentifier document, ResolveDataCache resolveDataCache, DocumentCache documentCache)
    {
        var dataId = resolveDataCache.UpdateCache(data);
        var documentId = documentCache.UpdateCache(document);

        return new DataIdResolveData(dataId, documentId);
    }

    internal static (object? data, LSP.TextDocumentIdentifier? document) FromCachedResolveData(object? lspData, ResolveDataCache resolveDataCache, DocumentCache documentCache)
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
        var document = documentCache.GetCachedEntry(resolveData.DocumentId);

        return (data, document);
    }
}
