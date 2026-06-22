// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

internal static partial class LocalHtmlCompletionProvider
{
    /// <summary>
    /// Returns true if element completion should fall back to the external HTML server.
    /// This covers SVG content where our schema has no child element data.
    /// </summary>
    private static bool ShouldFallBackForElements(RazorSyntaxNode owner)
    {
        // SVG child elements aren't in our schema. If we're inside an <svg>,
        // fall back to the external HTML server which has the full SVG schema.
        return IsInsideSvg(elementInfo: null, owner);
    }

    /// <summary>
    /// Returns true if attribute completion should fall back to the external HTML server.
    /// This covers SVG elements (not in our schema) and data-* attributes (project-specific).
    /// </summary>
    private static bool ShouldFallBackForAttributes(HtmlElementInfo? elementInfo, string? expandedPrefix, RazorSyntaxNode owner)
    {
        // SVG elements aren't in our schema. If the element is unknown and we're inside an <svg>,
        // fall back to the external HTML server which has the full SVG schema with element-specific attributes.
        if (IsInsideSvg(elementInfo, owner))
        {
            return true;
        }

        // data-* attributes are project-specific; our schema has no useful entries.
        // Fall back to the external HTML server which collects attributes from document usage.
        if (expandedPrefix == "data-")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if attribute value completion should fall back to the external HTML server.
    /// This covers SVG elements (not in our schema) and data-* attributes (project-specific values).
    /// </summary>
    private static bool ShouldFallBackForAttributeValues(HtmlElementInfo? elementInfo, string attributeName, RazorSyntaxNode owner)
    {
        // Unknown data-* attribute — fall back to external server which may have
        // usage-derived values from document scanning.
        if (attributeName.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // SVG elements aren't in our schema. If the element is unknown and we're inside an <svg>,
        // fall back to the external HTML server which has SVG attribute values.
        if (IsInsideSvg(elementInfo, owner))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the element is not in our schema and the cursor is inside an <c>&lt;svg&gt;</c>
    /// element at any nesting depth. SVG content completion is deferred to the external HTML server
    /// which has the full SVG schema.
    /// </summary>
    private static bool IsInsideSvg(HtmlElementInfo? elementInfo, RazorSyntaxNode owner)
    {
        if (elementInfo is not null)
        {
            return false;
        }

        for (var node = owner.Parent; node is not null; node = node.Parent)
        {
            if (node is BaseMarkupElementSyntax markupNode &&
                markupNode.StartTag?.Name.Content is string tagName)
            {
                if (string.Equals(tagName, "svg", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // If we hit a known HTML element, we're in HTML context, not SVG.
                if (HtmlCompletionData.GetElement(tagName) is not null)
                {
                    return false;
                }
            }
        }

        return false;
    }
}
