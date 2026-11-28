// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperIntermediateNode : IntermediateNode
{
    public required TagMode TagMode { get; init; }
    public required string TagName { get; init; }

    /// <summary>
    /// The source span of the start tag of the component that this tag helper represents, or null for an Mvc tag helper
    /// </summary>
    public SourceSpan? StartTagSpan { get; init; }

    public TagHelperCollection TagHelpers { get; init => field = value ?? []; } = [];

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelper(this);

    protected override IntermediateNode CloneNode()
        => new TagHelperIntermediateNode
        {
            TagMode = TagMode,
            TagName = TagName,
            StartTagSpan = StartTagSpan,
            TagHelpers = TagHelpers,
            IsSynthesizedHelper = IsSynthesizedHelper,
        };

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TagName);

        formatter.WriteProperty(nameof(TagHelpers), string.Join(", ", TagHelpers.Select(t => t.DisplayName)));
        formatter.WriteProperty(nameof(TagMode), TagMode.ToString());
        formatter.WriteProperty(nameof(TagName), TagName);
    }
}
