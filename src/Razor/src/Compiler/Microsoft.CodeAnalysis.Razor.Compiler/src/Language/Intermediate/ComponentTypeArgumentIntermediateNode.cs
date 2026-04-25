// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentTypeArgumentIntermediateNode(
    BoundAttributeDescriptor boundAttribute, CSharpIntermediateToken value) : IntermediateNode
{
    public BoundAttributeDescriptor BoundAttribute { get; } = boundAttribute;
    public TagHelperDescriptor TagHelper => BoundAttribute.Parent;

    public string TypeParameterName => BoundAttribute.Name;

    public CSharpIntermediateToken Value { get; } = value;

    public override IntermediateNodeCollection Children { get; } = [value];

    public ComponentTypeArgumentIntermediateNode(TagHelperPropertyIntermediateNode node)
        : this(node.BoundAttribute, GetValue(node))
    {
        Source = node.Source;
        AddDiagnosticsFromNode(node);
    }

    private static CSharpIntermediateToken GetValue(TagHelperPropertyIntermediateNode node)
        => node.Children switch
        {
            [CSharpIntermediateToken t] => t,
            [CSharpExpressionIntermediateNode { Children: [CSharpIntermediateToken t] }] => t,
            // Handle the case where the value was lowered as HTML content (from the unresolved tag helper pipeline).
            [HtmlContentIntermediateNode { Children: [HtmlIntermediateToken t] }] => t.IsLazy
                ? IntermediateNodeFactory.CSharpToken(
                    arg: t,
                    contentFactory: static token => token.Content,
                    source: t.Source)
                : new CSharpIntermediateToken(t.Content, t.Source),
            _ => Assumed.Unreachable<CSharpIntermediateToken>()
        };

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitComponentTypeArgument(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TypeParameterName);

        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper.DisplayName);
    }
}
