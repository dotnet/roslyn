// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Razor.CallHierarchy;

internal sealed record RazorCallHierarchyResolveData(
    TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData,
    [property: JsonPropertyName("razorCallHierarchy")] bool IsRazorCallHierarchy = true) : DocumentResolveData(TextDocument)
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
