// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CallHierarchy;

internal record RazorCallHierarchyResolveData(
    // NOTE: Uppercase T here is required to match Roslyn's DocumentResolveData structure, so that the Roslyn
    //       language server can correctly route requests to us in cohosting. In future when we normalize
    //       on to Roslyn types, we should inherit from that class so we don't have to remember to do this.
    [property: JsonPropertyName("TextDocument")] TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData,
    [property: JsonPropertyName("razorCallHierarchy")] bool IsRazorCallHierarchy = true)
{
    public static RazorCallHierarchyResolveData? Unwrap(CallHierarchyItem item)
    {
        if (item.Data is RazorCallHierarchyResolveData wrapper)
        {
            return wrapper is { IsRazorCallHierarchy: true, OriginalData: not null } ? wrapper : null;
        }

        if (item.Data is not JsonElement paramsObj)
        {
            return null;
        }

        return paramsObj.Deserialize<RazorCallHierarchyResolveData>() is { IsRazorCallHierarchy: true, OriginalData: not null } parsedWrapper
            ? parsedWrapper
            : null;
    }

    public static CallHierarchyItem Wrap(CallHierarchyItem item, TextDocumentIdentifier textDocument)
        => WithData(item, new RazorCallHierarchyResolveData(textDocument, item.Data));

    public static CallHierarchyItem WithData(CallHierarchyItem item, object? data)
        => new()
        {
            Name = item.Name,
            Kind = item.Kind,
            Tags = item.Tags,
            Detail = item.Detail,
            Uri = item.Uri,
            Range = item.Range,
            SelectionRange = item.SelectionRange,
            Data = data,
        };
}
