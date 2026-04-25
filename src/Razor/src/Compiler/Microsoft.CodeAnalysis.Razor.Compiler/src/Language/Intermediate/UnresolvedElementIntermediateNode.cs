// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
/// An intermediate node that represents an HTML element which may or may not be a tag helper.
/// All syntax-tree-derived information is stored directly on this node during lowering,
/// so the resolution phase does not need access to the syntax tree.
/// </summary>
internal sealed class UnresolvedElementIntermediateNode : IntermediateNode
{
    public string TagName { get; set; } = string.Empty;
    public bool IsComponent { get; set; }

    /// <summary>Whether the element is escaped with ! (e.g., &lt;!input&gt;).</summary>
    public bool IsEscaped { get; set; }

    /// <summary>Whether the start tag is self-closing (ends with /&gt;).</summary>
    public bool IsSelfClosing { get; set; }

    /// <summary>Whether the element has an end tag.</summary>
    public bool HasEndTag { get; set; }

    /// <summary>The end tag name (may differ in case from TagName for case-mismatch diagnostics).</summary>
    public string? EndTagName { get; set; }

    /// <summary>Source span of the end tag (for case-mismatch diagnostics).</summary>
    public SourceSpan? EndTagSpan { get; set; }

    /// <summary>Whether the element is a void element (e.g., input, br).</summary>
    public bool IsVoidElement { get; set; }

    /// <summary>Source span of the start tag's name (for component StartTagSpan).</summary>
    public SourceSpan? StartTagNameSpan { get; set; }

    /// <summary>Source span of the entire start tag (e.g., &lt;Component attr="val"&gt;), used for diagnostics.</summary>
    public SourceSpan? StartTagSpan { get; set; }

    /// <summary>Attribute name/value pairs for tag helper binding.</summary>
    public ImmutableArray<KeyValuePair<string, string>> AttributeData { get; set; }

    /// <summary>Whether the start tag is missing its close angle (&gt;).</summary>
    public bool HasMissingCloseAngle { get; set; }

    /// <summary>Whether the element has C# expression or code children in the start-tag region (e.g., &lt;div @expr&gt;).</summary>
    public bool HasDynamicExpressionChild { get; set; }

    /// <summary>Whether the end tag is missing its close angle (&gt;).</summary>
    public bool HasMissingEndCloseAngle { get; set; }

    /// <summary>
    /// The index in <see cref="Children"/> where body children begin (one past the last
    /// start-tag child). Children in <c>[0, StartTagEndIndex)</c> are start-tag children.
    /// Set during lowering; -1 if not computed.
    /// </summary>
    public int StartTagEndIndex { get; set; } = -1;

    /// <summary>
    /// The index in <see cref="Children"/> where end-tag children begin (one past the last
    /// body child). Children in <c>[StartTagEndIndex, BodyEndIndex)</c> are body children.
    /// Set during lowering; -1 if not computed.
    /// </summary>
    public int BodyEndIndex { get; set; } = -1;

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDefault(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TagName);
        formatter.WriteProperty(nameof(TagName), TagName);
        formatter.WriteProperty(nameof(IsComponent), IsComponent.ToString());
    }
}
