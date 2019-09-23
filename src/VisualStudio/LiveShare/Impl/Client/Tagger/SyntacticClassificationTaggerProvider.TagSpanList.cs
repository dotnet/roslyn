// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attemp to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal sealed partial class SyntacticClassificationTaggerProvider
    {
        internal sealed class TagSpanList<TTag> where TTag : ITag
        {
            public ITextSnapshot Snapshot { get; }

            private readonly ImmutableArray<TagWrapper> tagWrappers;

            public TagSpanList(IReadOnlyList<ITagSpan<TTag>> tagSpans)
            {
                if (tagSpans == null || tagSpans.Count == 0)
                {
                    // Default field values are correct
                    return;
                }

                this.Snapshot = tagSpans[0].Span.Snapshot;

                var builder = ImmutableArray.CreateBuilder<TagWrapper>(tagSpans.Count);
                foreach (var tagSpan in GetSortedSpans(tagSpans))
                {
                    Debug.Assert(!tagSpan.Span.IsEmpty);
                    Debug.Assert(tagSpan.Span.Snapshot == this.Snapshot);
                    builder.Add(new TagWrapper(tagSpan));
                }
                this.tagWrappers = builder.ToImmutable();
            }

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                var snapshot = this.Snapshot;
                if (snapshot == null || spans.Count == 0)
                {
                    return null;
                }

                var tags = new List<ITagSpan<TTag>>();
                foreach (var span in spans)
                {
                    var translatedSpan = span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive);

                    int startIndex = GetStartIndex(translatedSpan.Start);
                    int endIndex = GetEndIndex(translatedSpan.End);

                    for (int i = startIndex; i < endIndex; i++)
                    {
                        var wrapper = this.tagWrappers[i];
                        if (wrapper.Tag.HasValue)
                        {
                            // There is no apparent benefit to translating the tag's span back into the requested snapshot.
                            tags.Add(wrapper.Tag.Value);
                        }
                    }
                }
                return tags;
            }

            private static IEnumerable<ITagSpan<TTag>> GetSortedSpans(IReadOnlyList<ITagSpan<TTag>> tagSpans)
            {
                for (int i = 1; i < tagSpans.Count; i++)
                {
                    if (tagSpans[i - 1].Span.Start > tagSpans[i].Span.Start)
                    {
                        return tagSpans.OrderBy(t => t.Span.Start);
                    }
                }

                return tagSpans;
            }

            private int GetStartIndex(int position)
            {
                var target = new TagWrapper(Span.FromBounds(position, int.MaxValue));
                var index = this.tagWrappers.BinarySearch(target, StartComparer.Instance);

                int result;
                if (index >= 0)
                {
                    result = index;
                }
                else
                {
                    result = ~index - 1;
                    if (result >= 0 && this.tagWrappers[result].Span.End <= position)
                    {
                        result++;
                    }
                    else
                    {
                        result = Math.Max(0, result);
                    }
                }

                return result;
            }

            private int GetEndIndex(int position)
            {
                var target = new TagWrapper(Span.FromBounds(0, position));
                var index = this.tagWrappers.BinarySearch(target, EndComparer.Instance);

                int result;
                if (index >= 0)
                {
                    result = index + 1;
                }
                else
                {
                    result = ~index;
                    if (result < this.tagWrappers.Length && this.tagWrappers[result].Span.Start < position)
                    {
                        result = Math.Min(this.tagWrappers.Length, result + 1);
                    }
                }

                return result;
            }

            private sealed class StartComparer : IComparer<TagWrapper>
            {
                public static readonly IComparer<TagWrapper> Instance = new StartComparer();

                private StartComparer() { }

                int IComparer<TagWrapper>.Compare(TagWrapper x, TagWrapper y)
                {
                    return x.Span.Start - y.Span.Start;
                }
            }

            private sealed class EndComparer : IComparer<TagWrapper>
            {
                public static readonly IComparer<TagWrapper> Instance = new EndComparer();

                private EndComparer() { }

                int IComparer<TagWrapper>.Compare(TagWrapper x, TagWrapper y)
                {
                    return x.Span.End - y.Span.End;
                }
            }

            private struct TagWrapper
            {
                public readonly Span Span;
                public readonly Optional<ITagSpan<TTag>> Tag;

                public TagWrapper(Span span)
                {
                    this.Span = span;
                    this.Tag = default;
                }

                public TagWrapper(ITagSpan<TTag> tag)
                {
                    this.Span = tag.Span.Span;
                    this.Tag = new Optional<ITagSpan<TTag>>(tag);
                }

                public override string ToString()
                {
                    return $"Wrapper [{Span.Start}, {Span.End}){(Tag.HasValue ? " " + Tag.Value.Tag : "")}";
                }
            }
        }
    }
}
