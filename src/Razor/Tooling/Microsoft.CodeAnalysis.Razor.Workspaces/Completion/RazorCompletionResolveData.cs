// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed record RazorCompletionResolveData(
    TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData) : DocumentResolveData(TextDocument)
{
    public static RazorCompletionResolveData Unwrap(CompletionItem completionItem)
    {
        if (completionItem.Data is not JsonElement paramsObj)
        {
            throw new InvalidOperationException($"Invalid completion item received'{completionItem.Label}'.");
        }

        if (paramsObj.Deserialize<RazorCompletionResolveData>() is not { } context)
        {
            throw new InvalidOperationException($"completionItem.Data should be convertible to {nameof(RazorCompletionResolveData)}");
        }

        return context;
    }

    public static void Wrap(VSInternalCompletionList completionList, TextDocumentIdentifier textDocument, VSInternalClientCapabilities clientCapabilities)
    {
        var data = new RazorCompletionResolveData(textDocument, OriginalData: null);

        if (clientCapabilities.SupportsAnyCompletionListData())
        {
            if (clientCapabilities.SupportsCompletionListData() || completionList.Data is not null)
            {
                // Can set data at the completion list level
                completionList.Data = data with { OriginalData = completionList.Data };
            }

            if (clientCapabilities.SupportsCompletionListItemDefaultsData() || completionList.ItemDefaults?.Data is not null)
            {
                // Set data for the item defaults
                completionList.ItemDefaults ??= new();
                completionList.ItemDefaults.Data = data with { OriginalData = completionList.ItemDefaults.Data };
            }

            // Set data for items that won't inherit the default
            foreach (var completionItem in completionList.Items.Where(static c => c.Data is not null))
            {
                completionItem.Data = data with { OriginalData = completionItem.Data };
            }
        }
        else
        {
            // No CompletionList.Data support, so set data for all items
            foreach (var completionItem in completionList.Items)
            {
                completionItem.Data = data with { OriginalData = completionItem.Data };
            }
        }
    }
}
