// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
/// An unresolved intermediate node representing a dynamic/expression attribute value (e.g. the
/// <c>@someExpr</c> in <c>Value="@someExpr"</c>) whose final IR form depends on whether
/// the containing attribute is a bound tag helper property. Produced by initial lowering when
/// inside an <see cref="UnresolvedElementIntermediateNode"/>.
///
/// <para>The resolution phase converts this to:</para>
/// <list type="bullet">
///   <item>Direct <see cref="CSharpIntermediateToken"/> children (for bound non-string tag helper properties)</item>
///   <item>A <see cref="CSharpExpressionAttributeValueIntermediateNode"/> or
///         <see cref="CSharpCodeAttributeValueIntermediateNode"/> (for unbound/plain HTML attributes)</item>
/// </list>
/// </summary>
internal sealed class UnresolvedExpressionAttributeValueIntermediateNode : IntermediateNode
{
    /// <summary>The whitespace/text prefix before the expression.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether the dynamic value contains a top-level expression (implicit or explicit).
    /// When true, the non-tag-helper path produces <see cref="CSharpExpressionAttributeValueIntermediateNode"/>;
    /// when false, it produces <see cref="CSharpCodeAttributeValueIntermediateNode"/>.
    /// </summary>
    public bool ContainsExpression { get; set; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitDefault(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteChildren(Children);
        formatter.WriteProperty(nameof(Prefix), Prefix);
        formatter.WriteProperty(nameof(ContainsExpression), ContainsExpression.ToString());
    }
}
