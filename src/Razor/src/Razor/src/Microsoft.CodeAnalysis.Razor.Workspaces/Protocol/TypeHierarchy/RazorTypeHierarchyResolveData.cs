// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Razor.TypeHierarchy;

internal sealed record RazorTypeHierarchyResolveData(
    TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData,
    [property: JsonPropertyName("razorTypeHierarchy")] bool IsRazorTypeHierarchy = true) : DocumentResolveData(TextDocument)
{
    public static RazorTypeHierarchyResolveData? Unwrap(TypeHierarchyItem item)
    {
        if (item.Data is RazorTypeHierarchyResolveData wrapper)
        {
            return wrapper is { IsRazorTypeHierarchy: true, OriginalData: not null } ? wrapper : null;
        }

        if (item.Data is not JsonElement paramsObj)
        {
            return null;
        }

        return paramsObj.Deserialize<RazorTypeHierarchyResolveData>() is { IsRazorTypeHierarchy: true, OriginalData: not null } parsedWrapper
            ? parsedWrapper
            : null;
    }

    public static TypeHierarchyItem Wrap(TypeHierarchyItem item, TextDocumentIdentifier textDocument)
        => WithData(item, new RazorTypeHierarchyResolveData(textDocument, item.Data));

    public static TypeHierarchyItem WithData(TypeHierarchyItem item, object? data)
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
