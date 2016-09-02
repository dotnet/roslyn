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
        /// The primary description of the item.
        /// </summary>
        public ImmutableArray<TaggedText> Description { get; }

        /// <summary>
        /// Text derived from the documentation comments in the item's declaration.
        /// </summary>
        public ImmutableArray<TaggedText> DocumentationComments { get; }

        /// <summary>
        /// Text describing the item's type parameters.
        /// </summary>
        public ImmutableArray<TaggedText> TypeParameters { get; }

        /// <summary>
        /// Text describing the associated anonymous type declaration.
        /// </summary>
        public ImmutableArray<TaggedText> AnonymousTypes { get; }

        /// <summary>
        /// Text describing usage of the item.
        /// </summary>
        public ImmutableArray<TaggedText> Usage { get; }

        /// <summary>
        /// Text detailing exceptions associated with the item.
        /// </summary>
        public ImmutableArray<TaggedText> Exception { get; }

        /// <summary>
        /// Alternate regions of the document that help describe the item.
        /// </summary>
        public ImmutableArray<TextSpan> RelatedSpans { get; }

        private QuickInfoItem(
            TextSpan span,
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> description,
            ImmutableArray<TaggedText> documentationComments,
            ImmutableArray<TaggedText> typeParameters,
            ImmutableArray<TaggedText> anonymousTypes,
            ImmutableArray<TaggedText> usage,
            ImmutableArray<TaggedText> exception,
            ImmutableArray<TextSpan> relatedSpans)
        {
            this.Span = span;
            this.Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;
            this.Description = description.IsDefault ? ImmutableArray<TaggedText>.Empty : description;
            this.DocumentationComments = documentationComments.IsDefault ? ImmutableArray<TaggedText>.Empty : documentationComments;
            this.TypeParameters = typeParameters.IsDefault ? ImmutableArray<TaggedText>.Empty : typeParameters;
            this.AnonymousTypes = anonymousTypes.IsDefault ? ImmutableArray<TaggedText>.Empty : anonymousTypes;
            this.Usage = usage.IsDefault ? ImmutableArray<TaggedText>.Empty : usage;
            this.Exception = exception.IsDefault ? ImmutableArray<TaggedText>.Empty : exception;
            this.RelatedSpans = relatedSpans.IsDefault ? ImmutableArray<TextSpan>.Empty : relatedSpans;
        }

        public static QuickInfoItem Create(
            TextSpan span,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            ImmutableArray<TaggedText> description = default(ImmutableArray<TaggedText>),
            ImmutableArray<TaggedText> documentationComments = default(ImmutableArray<TaggedText>),
            ImmutableArray<TaggedText> typeParameters = default(ImmutableArray<TaggedText>),
            ImmutableArray<TaggedText> anonymousTypes = default(ImmutableArray<TaggedText>),
            ImmutableArray<TaggedText> usage = default(ImmutableArray<TaggedText>),
            ImmutableArray<TaggedText> exception = default(ImmutableArray<TaggedText>),
            ImmutableArray<TextSpan> relatedSpans = default(ImmutableArray<TextSpan>))
        {
            return new QuickInfoItem(
                span,
                tags,
                description,
                documentationComments,
                typeParameters,
                anonymousTypes,
                usage,
                exception,
                relatedSpans);
        }

        public bool IsEmpty
        {
            get
            {
                return this == Empty
                    || (this.Span == default(TextSpan)
                    && this.Tags.Length == 0
                    && this.Description.Length == 0
                    && this.DocumentationComments.Length == 0
                    && this.TypeParameters.Length == 0
                    && this.AnonymousTypes.Length == 0
                    && this.Usage.Length == 0
                    && this.Exception.Length == 0
                    && this.RelatedSpans.Length == 0);
            }
        }

        public static readonly QuickInfoItem Empty = Create(default(TextSpan));
    }
}