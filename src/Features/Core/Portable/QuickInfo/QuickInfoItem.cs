// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal sealed class QuickInfoItem
    {
        /// <summary>
        /// The span of the document that the item is based on.
        /// </summary>
        public TextSpan Span { get; set; }

        /// <summary>
        /// Descriptive tags from the <see cref="Microsoft.CodeAnalysis.Completion.CompletionTags"/> type.
        /// These tags may influence how the item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// One or more <see cref="QuickInfoTextBlock"/> describing the item.
        /// </summary>
        public ImmutableArray<QuickInfoTextBlock> TextBlocks { get; }

        /// <summary>
        /// Alternate regions of the document that help describe the item.
        /// </summary>
        public ImmutableArray<TextSpan> RelatedSpans { get; }

        private QuickInfoItem(
            TextSpan span,
            ImmutableArray<string> tags,
            ImmutableArray<QuickInfoTextBlock> textBlocks,
            ImmutableArray<TextSpan> relatedSpans)
        {
            this.Span = span;
            this.Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;
            this.TextBlocks = textBlocks.IsDefault ? ImmutableArray<QuickInfoTextBlock>.Empty : textBlocks;
            this.RelatedSpans = relatedSpans.IsDefault ? ImmutableArray<TextSpan>.Empty : relatedSpans;
        }

        public static QuickInfoItem Create(
            TextSpan span,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            ImmutableArray<QuickInfoTextBlock> textBlocks = default(ImmutableArray<QuickInfoTextBlock>),
            ImmutableArray<TextSpan> relatedSpans = default(ImmutableArray<TextSpan>))
        {
            return new QuickInfoItem(span, tags, textBlocks, relatedSpans);
        }

        public bool IsEmpty
        {
            get
            {
                return this == Empty
                    || (this.Span == default(TextSpan)
                    && this.Tags.Length == 0
                    && this.TextBlocks.Length == 0
                    && this.RelatedSpans.Length == 0);
            }
        }

        public static readonly QuickInfoItem Empty = Create(default(TextSpan));
    }
}