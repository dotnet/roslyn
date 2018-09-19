﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo
{
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

        private QuickInfoItem(
            TextSpan span,
            ImmutableArray<string> tags,
            ImmutableArray<QuickInfoSection> sections,
            ImmutableArray<TextSpan> relatedSpans)
        {
            this.Span = span;
            this.Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;
            this.Sections = sections.IsDefault ? ImmutableArray<QuickInfoSection>.Empty : sections;
            this.RelatedSpans = relatedSpans.IsDefault ? ImmutableArray<TextSpan>.Empty : relatedSpans;
        }

        public static QuickInfoItem Create(
            TextSpan span,
            ImmutableArray<string> tags = default,
            ImmutableArray<QuickInfoSection> sections = default,
            ImmutableArray<TextSpan> relatedSpans = default)
        {
            return new QuickInfoItem(span, tags, sections, relatedSpans);
        }
    }
}
