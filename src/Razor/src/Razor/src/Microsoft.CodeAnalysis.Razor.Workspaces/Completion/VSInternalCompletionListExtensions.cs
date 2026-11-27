// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class VSInternalCompletionListExtensions
{
    public static void SetResultId(
        this RazorVSInternalCompletionList completionList,
        int resultId,
        VSInternalClientCapabilities clientCapabilities)
    {
        var data = JsonSerializer.SerializeToElement(new JsonObject()
        {
            [VSInternalCompletionItemExtensions.ResultIdKey] = resultId,
        });

        if (clientCapabilities.SupportsAnyCompletionListData())
        {
            if (clientCapabilities.SupportsCompletionListData() || completionList.Data is not null)
            {
                completionList.Data = CompletionListMerger.MergeData(data, completionList.Data);
            }

            if (clientCapabilities.SupportsCompletionListItemDefaultsData() || completionList.ItemDefaults?.Data is not null)
            {
                completionList.ItemDefaults ??= new();
                completionList.ItemDefaults.Data = CompletionListMerger.MergeData(data, completionList.ItemDefaults.Data);
            }

            // Merge data for items that won't inherit the default
            foreach (var completionItem in completionList.Items.Where(c => c.Data is not null))
            {
                completionItem.Data = CompletionListMerger.MergeData(data, completionItem.Data);
            }
        }
        else
        {
            // No CompletionList.Data support
            foreach (var completionItem in completionList.Items)
            {
                completionItem.Data = CompletionListMerger.MergeData(data, completionItem.Data);
            }
        }
    }
}
