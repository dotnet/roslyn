// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentAttributeIntermediateNode : IntermediateNode
{
    public bool HasExplicitTypeName { get; set; }
    public bool IsOpenGeneric { get; set; }

    /// <summary>
    /// Used to track if this node was synthesized by the compiler and not explicitly written by a user.
    /// </summary>
    public bool IsSynthesized { get; set; }

    public bool IsDesignTimePropertyAccessHelper { get; set; }

    /// <summary>
    /// Represents the sub-span of the bind node that actually represents the property
    /// </summary>
    /// <remarks>
    /// <pre>
    /// @bind-Value:get=""
    /// ^----------------^ Regular node span
    ///       ^---^        Property span
    /// </pre>
    /// </remarks>
    public SourceSpan? PropertySpan { get; set; }

    public string OriginalAttributeName { get; set; }

    public SourceSpan? OriginalAttributeSpan { get; set; }

    public string AddAttributeMethodName { get; set; }

    /// <summary>
    /// When a generic component is re-written with its concrete implementation type
    /// We use this metadata on its bound attributes to track the updated type.
    /// </summary>
    public string ConcreteContainingType { get; set; }

    public ComponentAttributeIntermediateNode()
    {
    }

    public ComponentAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode attributeNode)
    {
        if (attributeNode == null)
        {
            throw new ArgumentNullException(nameof(attributeNode));
        }

        AttributeName = attributeNode.AttributeName;
        AttributeStructure = attributeNode.AttributeStructure;
        Source = attributeNode.Source;

        for (var i = 0; i < attributeNode.Children.Count; i++)
        {
            Children.Add(attributeNode.Children[i]);
        }

        AddDiagnosticsFromNode(attributeNode);
    }

    public ComponentAttributeIntermediateNode(TagHelperPropertyIntermediateNode propertyNode)
    {
        if (propertyNode == null)
        {
            throw new ArgumentNullException(nameof(propertyNode));
        }

        var attributeName = propertyNode.AttributeName;

        AttributeName = attributeName;
        AttributeStructure = propertyNode.AttributeStructure;
        BoundAttribute = propertyNode.BoundAttribute;
        OriginalAttributeSpan = propertyNode.OriginalAttributeSpan;
        PropertyName = propertyNode.BoundAttribute.PropertyName;
        Source = propertyNode.Source;
        TypeName = propertyNode.BoundAttribute.IsWeaklyTyped ? null : propertyNode.BoundAttribute.TypeName;

        for (var i = 0; i < propertyNode.Children.Count; i++)
        {
            Children.Add(propertyNode.Children[i]);
        }

        AddDiagnosticsFromNode(propertyNode);
    }

    private ComponentAttributeIntermediateNode(TagHelperDirectiveAttributeIntermediateNode node, bool addChildren)
    {
        AttributeName = node.AttributeName;
        AttributeStructure = node.AttributeStructure;
        BoundAttribute = node.BoundAttribute;
        OriginalAttributeSpan = node.OriginalAttributeSpan;
        PropertyName = node.BoundAttribute.PropertyName;
        Source = node.Source;
        TypeName = node.BoundAttribute.IsWeaklyTyped
            ? null
            : node.BoundAttribute.TypeName;

        if (addChildren)
        {
            Children.AddRange(node.Children);
        }

        AddDiagnosticsFromNode(node);
    }

    private ComponentAttributeIntermediateNode(TagHelperDirectiveAttributeParameterIntermediateNode node, bool addChildren)
    {
        AttributeName = node.AttributeNameWithoutParameter;
        AttributeStructure = node.AttributeStructure;
        BoundAttribute = node.BoundAttribute;
        OriginalAttributeSpan = node.OriginalAttributeSpan;
        PropertyName = node.BoundAttributeParameter.PropertyName;
        Source = node.Source;
        TypeName = node.BoundAttributeParameter.TypeName;

        if (addChildren)
        {
            Children.AddRange(node.Children);
        }

        AddDiagnosticsFromNode(node);
    }

    public static ComponentAttributeIntermediateNode From(TagHelperDirectiveAttributeIntermediateNode node, bool addChildren)
        => new(node, addChildren);

    public static ComponentAttributeIntermediateNode From(TagHelperDirectiveAttributeParameterIntermediateNode node, bool addChildren)
        => new(node, addChildren);

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public string AttributeName { get; set; }

    public AttributeStructure AttributeStructure { get; set; }

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public string PropertyName { get; set; }

    public TagHelperDescriptor TagHelper => BoundAttribute?.Parent;

    public string TypeName { get; set; }

    public string GloballyQualifiedTypeName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentAttribute(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(PropertyName), PropertyName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(GloballyQualifiedTypeName), GloballyQualifiedTypeName);
    }

    public bool TryParseEventCallbackTypeArgument(out string argument)
    {
        if (TryParseEventCallbackTypeArgument(out ReadOnlyMemory<char> memory))
        {
            argument = memory.ToString();
            return true;
        }

        argument = null;
        return false;
    }

    internal bool TryParseEventCallbackTypeArgument(out ReadOnlyMemory<char> argument)
    {
        // This is ugly and ad-hoc, but for various layering reasons we can't just use Roslyn APIs
        // to parse this. We need to parse this just before we write it out to the code generator,
        // so we can't compute it up front either.

        if (BoundAttribute == null || !BoundAttribute.IsEventCallbackProperty())
        {
            throw new InvalidOperationException("This attribute is not an EventCallback attribute.");
        }

        return TryGetEventCallbackArgument(TypeName.AsMemory(), out argument);
    }

    internal static bool TryGetEventCallbackArgument(ReadOnlyMemory<char> candidate, out ReadOnlyMemory<char> argument)
    {
        // Strip 'global::' from the candidate.
        if (candidate.Span.StartsWith("global::".AsSpan()))
        {
            candidate = candidate["global::".Length..];
        }

        var eventCallbackName = ComponentsApi.EventCallback.FullTypeName.AsSpan();

        // Check to see if this is the non-generic form. If so, there's no argument to retrieve.
        if (candidate.Span.Equals(eventCallbackName, StringComparison.Ordinal))
        {
            argument = default;
            return false;
        }

        if (candidate.Length <= eventCallbackName.Length + "<>".Length ||
            !candidate.Span.StartsWith(eventCallbackName, StringComparison.Ordinal))
        {
            argument = default;
            return false;
        }

        var afterCallbackName = candidate[eventCallbackName.Length..];
        if (afterCallbackName.Span is ['<', .., '>'])
        {
            argument = afterCallbackName[1..^1];
            return true;
        }

        // If we get here this is a failure. This should only happen if someone manages to mangle the name with extensibility.
        // We don't really want to crash though.
        argument = default;
        return false;
    }

    internal static bool TryGetActionArgument(ReadOnlyMemory<char> candidate, out ReadOnlyMemory<char> argument)
    {
        // Strip 'global::' from the candidate.
        if (candidate.Span.StartsWith("global::".AsSpan()))
        {
            candidate = candidate["global::".Length..];
        }

        if (!candidate.Span.StartsWith("System.Action".AsSpan()))
        {
            argument = default;
            return false;
        }


        candidate = candidate["System.Action".Length..];
        if (candidate.Span is ['<', .., '>'])
        {
            argument = candidate[1..^1];
            return true;
        }

        argument = default;
        return false;
    }

    /// <summary>
    /// Returns a string where the generic type argument of a System.Action&lt;MyType&lt;TItem&gt;&gt; this method
    /// constructs a new string replacing 'TItem' with <paramref name="genericType"/>
    /// </summary>
    /// <param name="candidate">The full candidate string to parse, e.g System.Action&lt;MyType&lt;TItem&gt;&gt;</param>
    /// <param name="genericType">The type name to replace the generic argument with</param>
    /// <param name="argument"/>The resulting string with the generic argument replaced with <paramref name="genericType"/></param>
    /// <returns>True if the argument could be constructed</returns>
    internal static bool TryGetGenericActionArgument(ReadOnlyMemory<char> candidate, string genericType, [NotNullWhen(true)] out string argument)
    {
        if (!TryGetActionArgument(candidate, out var actionArgument))
        {
            argument = default;
            return false;
        }

        var genericTypeStart = actionArgument.Span.IndexOf('<');
        var genericTypeEnd = actionArgument.Span.IndexOf('>');

        // Check (start + 1) to ensure there is at least one character
        // between the start and end
        if (genericTypeStart <= 0 || genericTypeEnd < (genericTypeStart + 1))
        {
            argument = default;
            return false;
        }

        argument = actionArgument[0..(genericTypeStart + 1)] + genericType + actionArgument[genericTypeEnd..];
        return true;
    }
}
