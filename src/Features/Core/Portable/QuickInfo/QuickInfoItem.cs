// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo;

public sealed class QuickInfoItem
{
    /// <summary>
    /// The span of the document that the item is based on.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Descriptive tags from the <see cref="Tags.WellKnownTags"/> type.
    /// These tags may influence how the item is displayed.
    /// </summary>
    public ImmutableArray<string> Tags { get; }

    /// <summary>
    /// One or more <see cref="QuickInfoSection"/> describing the item.
    /// </summary>
    public ImmutableArray<QuickInfoSection> Sections { get; }

    /// <summary>
    /// Alternate regions of the document that help describe the item.
    /// </summary>
    public ImmutableArray<TextSpan> RelatedSpans { get; }

    internal OnTheFlyDocsElement? OnTheFlyDocsElement { get; }

    private QuickInfoItem(
        TextSpan span,
        ImmutableArray<string> tags,
        ImmutableArray<QuickInfoSection> sections,
        ImmutableArray<TextSpan> relatedSpans,
        OnTheFlyDocsElement? onTheFlyDocsElement)
    {
        Span = span;
        Tags = tags.IsDefault ? [] : tags;
        Sections = sections.IsDefault ? [] : sections;
        RelatedSpans = relatedSpans.IsDefault ? [] : relatedSpans;
        OnTheFlyDocsElement = onTheFlyDocsElement;
    }

    public static QuickInfoItem Create(
        TextSpan span,
        ImmutableArray<string> tags = default,
        ImmutableArray<QuickInfoSection> sections = default,
        ImmutableArray<TextSpan> relatedSpans = default)
    {
        return Create(span, tags, sections, relatedSpans, onTheFlyDocsElement: null);
    }

    internal static QuickInfoItem Create(
        TextSpan span,
        ImmutableArray<string> tags,
        ImmutableArray<QuickInfoSection> sections,
        ImmutableArray<TextSpan> relatedSpans,
        OnTheFlyDocsElement? onTheFlyDocsElement)
    {
        return new QuickInfoItem(span, tags, sections, relatedSpans, onTheFlyDocsElement);
    }
}
