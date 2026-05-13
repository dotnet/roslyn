// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;

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
            // Fallback: the value lowered to a more complex shape, e.g. literal text mixed
            // with one or more Razor expressions ("leading @x", "@x trailing", multiple
            // expressions, etc.). Concatenate the token content from all descendants into a
            // single CSharp token so downstream code generation can emit the user-authored
            // text as a type argument. The C# compiler will then produce a normal diagnostic
            // if the result is not a valid type, instead of the Razor compiler crashing.
            _ => MergeChildTokens(node),
        };

    private static CSharpIntermediateToken MergeChildTokens(TagHelperPropertyIntermediateNode node)
    {
        using var _sb = StringBuilderPool.GetPooledObject(out var sb);
        SourceSpan? firstSpan = null;
        SourceSpan? lastSpan = null;

        AppendTokens(node, sb, ref firstSpan, ref lastSpan);

        SourceSpan? mergedSpan = null;
        if (firstSpan is { } first && lastSpan is { } last && first.AbsoluteIndex <= last.AbsoluteIndex)
        {
            mergedSpan = DefaultTagHelperResolutionPhase.MergeSourceSpans(first, last);
        }
        else
        {
            mergedSpan = firstSpan ?? lastSpan ?? node.Source;
        }

        return new CSharpIntermediateToken(sb.ToString(), mergedSpan);
    }

    private static void AppendTokens(IntermediateNode node, StringBuilder sb, ref SourceSpan? firstSpan, ref SourceSpan? lastSpan)
    {
        foreach (var child in node.Children)
        {
            if (child is IntermediateToken token)
            {
                sb.Append(token.Content);
                if (token.Source is { } s)
                {
                    firstSpan ??= s;
                    lastSpan = s;
                }
            }
            else
            {
                // Unwrap container nodes (CSharpExpressionIntermediateNode, HtmlContentIntermediateNode, etc.).
                AppendTokens(child, sb, ref firstSpan, ref lastSpan);
            }
        }
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitComponentTypeArgument(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TypeParameterName);

        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper.DisplayName);
    }
}
