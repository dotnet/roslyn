// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

internal static class CallHierarchyHelpers
{
    /// <summary>
    /// Extracts CallHierarchyResolveData from the LSP CallHierarchyItem.Data field.
    /// </summary>
    public static CallHierarchyResolveData? GetCallHierarchyResolveData(LSP.CallHierarchyItem item)
    {
        if (item.Data == null)
            return null;

        try
        {
            // The Data field is a JsonElement when deserialized
            if (item.Data is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<CallHierarchyResolveData>(jsonElement.GetRawText(), ProtocolConversions.LspJsonSerializerOptions);
            }

            // If it's already our type, return it
            if (item.Data is CallHierarchyResolveData data)
            {
                return data;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reconstructs a CallHierarchyItem from the cache using resolve data.
    /// </summary>
    public static CodeAnalysis.CallHierarchy.CallHierarchyItem? GetCallHierarchyItem(
        CallHierarchyResolveData resolveData,
        CallHierarchyCache cache)
    {
        var cacheEntry = cache.GetCachedEntry(resolveData.ResultId);
        if (cacheEntry == null || resolveData.ItemIndex >= cacheEntry.CallHierarchyItems.Length)
            return null;

        return cacheEntry.CallHierarchyItems[resolveData.ItemIndex];
    }
}
