// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperHtmlAttributeIntermediateNode : IntermediateNode
{
    public required string AttributeName { get; init; }
    public required AttributeStructure AttributeStructure { get; init; }
    internal SourceSpan? AttributeNameSpan { get; init; }
    internal bool IsDirectiveAttributeCandidate { get; init; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperHtmlAttribute(this);

    protected override IntermediateNode CloneNode()
        => new TagHelperHtmlAttributeIntermediateNode
        {
            AttributeName = AttributeName,
            AttributeStructure = AttributeStructure,
            AttributeNameSpan = AttributeNameSpan,
            IsDirectiveAttributeCandidate = IsDirectiveAttributeCandidate,
            IsSynthesizedHelper = IsSynthesizedHelper,
        };

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
    }
}
