// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal sealed class QuickInfoElement
    {
        public string Kind { get; private set; }
        public ImmutableArray<string> Tags { get; private set; }
        public ImmutableArray<TaggedText> Text { get; private set; }
        public ImmutableArray<TextSpan> Spans { get; private set; }
        public ImmutableArray<QuickInfoElement> Elements { get; private set; }

        private QuickInfoElement(string kind, ImmutableArray<string> tags, ImmutableArray<TaggedText> text, ImmutableArray<TextSpan> spans, ImmutableArray<QuickInfoElement> elements)
        {
            this.Kind = kind ?? "";
            this.Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;
            this.Text = text.IsDefault ? ImmutableArray<TaggedText>.Empty : text;
            this.Spans = spans.IsDefault ? ImmutableArray<TextSpan>.Empty : spans;
            this.Elements = elements.IsDefault ? ImmutableArray<QuickInfoElement>.Empty : elements;
        }

        public static QuickInfoElement Create(
            string kind,
            ImmutableArray<string> tags = default(ImmutableArray<string>),
            ImmutableArray<TaggedText> text = default(ImmutableArray<TaggedText>),
            ImmutableArray<TextSpan> spans = default(ImmutableArray<TextSpan>),
            ImmutableArray<QuickInfoElement> elements = default(ImmutableArray<QuickInfoElement>))
        {
            return new QuickInfoElement(kind, tags, text, spans, elements);
        }

        private QuickInfoElement With(
            Optional<string> kind = default(Optional<string>),
            Optional<ImmutableArray<string>> tags = default(Optional<ImmutableArray<string>>),
            Optional<ImmutableArray<TaggedText>> text = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TextSpan>> spans = default(Optional<ImmutableArray<TextSpan>>),
            Optional<ImmutableArray<QuickInfoElement>> elements = default(Optional<ImmutableArray<QuickInfoElement>>))
        {
            var newKind = kind.HasValue ? kind.Value : this.Kind;
            var newTags = tags.HasValue ? tags.Value : this.Tags;
            var newText = text.HasValue ? text.Value : this.Text;
            var newSpans = spans.HasValue ? spans.Value : this.Spans;
            var newElements = elements.HasValue ? elements.Value : this.Elements;

            if (newKind != this.Kind && newTags != this.Tags && newText != this.Text && newSpans != this.Spans && newElements != this.Elements)
            {
                return new QuickInfoElement(newKind, newTags, newText, newSpans, newElements);
            }
            else
            {
                return this;
            }
        }

        public QuickInfoElement WithKind(string kind)
        {
            return this.With(kind: kind);
        }

        public QuickInfoElement WithTags(ImmutableArray<string> tags)
        {
            return this.With(tags: tags);
        }

        public QuickInfoElement WithText(ImmutableArray<TaggedText> text)
        {
            return this.With(text: text);
        }

        public QuickInfoElement WithSpans(ImmutableArray<TextSpan> spans)
        {
            return this.With(spans: spans);
        }

        public QuickInfoElement WithElements(ImmutableArray<QuickInfoElement> elements)
        {
            return this.With(elements: elements);
        }

        private string _rawText;

        public string RawText
        {
            get
            {
                if (_rawText == null)
                {
                    if (this.Text.Length > 0)
                    {
                        _rawText = string.Concat(this.Text.Select(t => t.Text));
                    }
                    else
                    {
                        _rawText = string.Empty;
                    }
                }

                return _rawText;
            }
        }

        public static readonly QuickInfoElement Empty = Create(kind: "");
    }
}