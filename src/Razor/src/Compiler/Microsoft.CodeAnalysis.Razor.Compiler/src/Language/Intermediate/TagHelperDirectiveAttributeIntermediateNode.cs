// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperDirectiveAttributeIntermediateNode : IntermediateNode
{
    private readonly TagHelperAttributeMatch _match;

    public required string AttributeName { get; init; }
    public required string OriginalAttributeName { get; init; }

    public required AttributeStructure AttributeStructure { get; init; }
    public SourceSpan? OriginalAttributeSpan { get; init; }

    public bool IsIndexerNameMatch => _match.IsIndexerMatch;

    public BoundAttributeDescriptor BoundAttribute => _match.Attribute;
    public TagHelperDescriptor TagHelper => BoundAttribute.Parent;

    public override IntermediateNodeCollection Children { get => field ??= []; }

    internal TagHelperDirectiveAttributeIntermediateNode(TagHelperAttributeMatch match)
    {
        Debug.Assert(!match.IsParameterMatch);

        _match = match;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperDirectiveAttribute(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(OriginalAttributeName), OriginalAttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute.DisplayName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(TagHelper), TagHelper.DisplayName);
    }
}
