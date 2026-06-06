// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

/// <summary>
/// Resolve context for completion items produced by the local HTML completion provider.
/// Stores the attribute arrays used to build the completion list so resolve can find
/// Description and DocumentationUrl without walking the entire HTML schema.
/// Element resolve uses <see cref="HtmlCompletionData.GetElement"/> which is already O(1).
/// </summary>
internal sealed class LocalHtmlCompletionResolveContext(
    ImmutableArray<HtmlAttributeInfo> elementAttributes,
    ImmutableArray<HtmlAttributeInfo> globalAttributes) : ICompletionResolveContext
{
    internal static readonly LocalHtmlCompletionResolveContext Empty = new([], []);

    public bool TryGetResolveData(string label, CompletionItemKind kind, [NotNullWhen(true)] out string? description, [NotNullWhen(true)] out string? documentationUrl, out string? baseline, out string? baselineDate)
    {
        if (kind == CompletionItemKind.Element)
        {
            if (HtmlCompletionData.GetElement(label) is { } elementInfo)
            {
                description = elementInfo.Description;
                documentationUrl = elementInfo.DocumentationUrl;
                baseline = elementInfo.Baseline;
                baselineDate = elementInfo.BaselineDate;
                return true;
            }
        }
        else
        {
            if (TryFindAttribute(elementAttributes, label, out description, out documentationUrl, out baseline, out baselineDate) ||
                TryFindAttribute(globalAttributes, label, out description, out documentationUrl, out baseline, out baselineDate))
            {
                return true;
            }
        }

        description = null;
        documentationUrl = null;
        baseline = null;
        baselineDate = null;
        return false;
    }

    private static bool TryFindAttribute(ImmutableArray<HtmlAttributeInfo> attributes, string name,
        [NotNullWhen(true)] out string? description, [NotNullWhen(true)] out string? documentationUrl, out string? baseline, out string? baselineDate)
    {
        foreach (var attr in attributes)
        {
            if (string.Equals(attr.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                description = attr.Description;
                documentationUrl = attr.DocumentationUrl;
                baseline = attr.Baseline;
                baselineDate = attr.BaselineDate;
                return true;
            }
        }

        description = null;
        documentationUrl = null;
        baseline = null;
        baselineDate = null;
        return false;
    }
}
