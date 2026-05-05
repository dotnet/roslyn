// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
/// A unresolved intermediate node representing a literal attribute value (e.g. the <c>123</c> in
/// <c>IntProperty="123"</c>) whose final IR form depends on whether the containing attribute
/// is a bound tag helper property. Produced by initial lowering when inside an
/// <see cref="UnresolvedElementIntermediateNode"/>.
///
/// <para>The resolution phase converts this to:</para>
/// <list type="bullet">
///   <item>A <see cref="CSharpIntermediateToken"/> (for bound non-string tag helper properties)</item>
///   <item>An <see cref="HtmlContentIntermediateNode"/> (for bound string tag helper properties)</item>
///   <item>An <see cref="HtmlAttributeValueIntermediateNode"/> (for unbound/plain HTML attributes)</item>
/// </list>
/// </summary>
internal sealed class UnresolvedAttributeValueIntermediateNode : IntermediateNode
{
    /// <summary>The whitespace/text prefix before the value (from parser splitting on spaces).</summary>
    public string Prefix { get; set; } = string.Empty;

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDefault(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteChildren(Children);
        formatter.WriteProperty(nameof(Prefix), Prefix);
    }
}
