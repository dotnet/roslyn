// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class RazorCodeDocumentExtensions
{
    /// <summary>
    /// Adjusts the position if it's on a component end tag to use the corresponding start tag position.
    /// This ensures that hover, go to definition, and find all references work consistently for both
    /// start and end tags, since only start tags have source mappings.
    /// </summary>
    /// <param name="codeDocument">The code document.</param>
    /// <param name="hostDocumentIndex">The position in the host document.</param>
    /// <returns>
    /// The adjusted position if on a component end tag's name, otherwise the original position.
    /// </returns>
    public static int AdjustPositionForComponentEndTag(this RazorCodeDocument codeDocument, int hostDocumentIndex)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        var owner = root.FindInnermostNode(hostDocumentIndex, includeWhitespace: false);
        if (owner is null)
        {
            return hostDocumentIndex;
        }

        // Check if we're on a component end tag and the position is within the tag name
        if (owner.FirstAncestorOrSelf<MarkupTagHelperEndTagSyntax>() is { } endTag &&
            endTag.Name.Span.IntersectsWith(hostDocumentIndex) &&
            endTag.GetStartTag() is MarkupTagHelperStartTagSyntax tagHelperStartTag)
        {
            // Calculate the offset within the end tag name
            var offsetInEndTag = hostDocumentIndex - endTag.Name.SpanStart;

            // Apply the same offset to the start tag name
            // This preserves the relative position within the tag name
            return tagHelperStartTag.Name.SpanStart + offsetInEndTag;
        }

        return hostDocumentIndex;
    }
}
