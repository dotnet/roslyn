// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[Export(typeof(IResolveDataService))]
internal class ResolveDataService : IResolveDataService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ResolveDataService()
    {
    }

    public object ToResolveData(XamlRequestContext context, object data, TextDocumentIdentifier document)
    {
        var resolveDataCache = context.GetRequiredLspService<ResolveDataCache>();
        var documentCache = context.GetRequiredLspService<DocumentCache>();
        var dataId = resolveDataCache.UpdateCache(data);
        var documentId = documentCache.UpdateCache(document);

        return new DataIdResolveData(dataId, documentId);
    }

    public (object? data, TextDocumentIdentifier? document) FromResolveData(XamlRequestContext context, object? lspData)
    {
        DataIdResolveData? resolveData = null;
        if (lspData is JToken token)
        {
            resolveData = token.ToObject<DataIdResolveData>();
            Assumes.Present(resolveData);
        }
        else
        {
            return (null, null);
        }

        var resolveDataCache = context.GetRequiredLspService<ResolveDataCache>();
        var documentCache = context.GetRequiredLspService<DocumentCache>();
        var data = resolveDataCache.GetCachedEntry(resolveData.DataId);
        var document = documentCache.GetCachedEntry(resolveData.DocumentId);

        return (data, document);
    }
}
