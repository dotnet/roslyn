// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
/// An intermediate node representing an attribute whose final form (plain HTML attribute
/// vs tag helper bound attribute) is not yet determined. Pre-lowered fallback forms are
/// stored so the resolution phase does not need syntax tree access.
/// </summary>
internal sealed class UnresolvedAttributeIntermediateNode : IntermediateNode
{
    /// <summary>The attribute name (e.g., "Value", "@bind-Value", "@onclick").</summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>Whether this is a minimized attribute (no value).</summary>
    public bool IsMinimized { get; set; }

    /// <summary>The raw value content of the attribute (for all-literal values only).</summary>
    public string? ValueContent { get; set; }

    /// <summary>
    /// Source span covering the attribute value content (between quotes).
    /// Computed from syntax tree positions during lowering so the resolution phase
    /// can use it without needing syntax tree access.
    /// </summary>
    public SourceSpan? ValueSourceSpan { get; set; }

    /// <summary>
    /// The attribute structure (quote style). Pre-computed during initial lowering
    /// so the resolution phase does not need to inspect syntax.
    /// </summary>
    public AttributeStructure AttributeStructure { get; set; }

    /// <summary>
    /// Source span of the attribute name only (not the whole attribute).
    /// Pre-computed during initial lowering for tag helper property OriginalAttributeSpan.
    /// </summary>
    public SourceSpan? AttributeNameSpan { get; set; }

    /// <summary>
    /// Pre-lowered form of this attribute for when the element IS a tag helper but this
    /// attribute is unbound (doesn't match any tag helper property). The tag helper runtime
    /// represents unbound HTML attributes as <see cref="TagHelperHtmlAttributeIntermediateNode"/>
    /// wrapping a structured <see cref="HtmlAttributeIntermediateNode"/> with merged value tokens.
    /// Produced during lowering via <c>VisitAttributeValue</c>, which merges adjacent literal
    /// tokens into a single <see cref="HtmlContentIntermediateNode"/>.
    /// </summary>
    public IntermediateNode? AsTagHelperAttribute { get; set; }

    /// <summary>
    /// Pre-lowered form of this attribute for when the element is NOT a tag helper and needs
    /// to be unwrapped back to plain HTML markup. Plain HTML elements represent attributes with
    /// individual value tokens preserving the original parse structure. Produced during lowering
    /// via <c>LowerAttributeAsHtml</c>, which preserves individual tokens without merging.
    /// Used by <c>ConvertToMarkupElement</c> and <c>UnwrapElement</c> to restore the element
    /// to its non-tag-helper form.
    /// </summary>
    public IntermediateNode? AsMarkupAttribute { get; set; }

    /// <summary>
    /// The <see cref="HtmlAttributeIntermediateNode"/> child containing unresolved attribute
    /// value children. Set during lowering to avoid linear scans in the resolution phase.
    /// </summary>
    public HtmlAttributeIntermediateNode? HtmlAttributeNode { get; set; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDefault(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);
        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(IsMinimized), IsMinimized.ToString());
    }
}
