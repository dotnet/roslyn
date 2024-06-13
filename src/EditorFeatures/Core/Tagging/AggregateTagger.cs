// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

/// <summary>
/// Base type of all taggers that wrap a set of other <paramref name="taggers"/>, presenting them all as if they were a
/// single <see cref="ITagger{T}"/>.
/// </summary>
internal abstract class AbstractAggregateTagger<TTag>(ImmutableArray<EfficientTagger<TTag>> taggers) : EfficientTagger<TTag>, IDisposable
    where TTag : ITag
{
    protected readonly ImmutableArray<EfficientTagger<TTag>> Taggers = taggers;

    /// <summary>
    /// Disposes all the underlying taggers (if they themselves are <see cref="IDisposable"/>.
    /// </summary>
    public override void Dispose()
    {
        foreach (var tagger in this.Taggers)
            tagger.Dispose();
    }

    /// <summary>
    /// This tagger considers itself changed if any underlying taggers signal that they are changed.
    /// </summary>
    public override event EventHandler<SnapshotSpanEventArgs>? TagsChanged
    {
        add
        {
            foreach (var tagger in this.Taggers)
                tagger.TagsChanged += value;
        }

        remove
        {
            foreach (var tagger in this.Taggers)
                tagger.TagsChanged -= value;
        }
    }
}

/// <summary>
/// Simple tagger that aggregates the underlying taggers and presents them as a single event source and source of tags.
/// The final set of tags produced by any <see cref="AddTags"/> request is a deduped set of all the tags produced
/// by the individual <paramref name="taggers"/>.
/// </summary>
internal sealed class DeduplicateAggregateTagger<TTag>(ImmutableArray<EfficientTagger<TTag>> taggers)
    : AbstractAggregateTagger<TTag>(taggers)
    where TTag : ITag
{
    private readonly ObjectPool<HashSet<TagSpan<TTag>>> _tagSpanSetPool = new ObjectPool<HashSet<TagSpan<TTag>>>(() => new(TagSpanEqualityComparer.Instance));

    public override void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<TagSpan<TTag>> tags)
    {
        if (spans.Count > 0)
        {
            var producedTags = new SegmentedList<TagSpan<TTag>>();
            foreach (var tagger in this.Taggers)
            {
                tagger.AddTags(spans, producedTags);
            }

            using var _ = _tagSpanSetPool.GetPooledObject(out var dedupedSet);
            dedupedSet.AddRange(producedTags);
            tags.AddRange(dedupedSet);
        }
    }

    private sealed class TagSpanEqualityComparer : IEqualityComparer<TagSpan<TTag>>
    {
        public static TagSpanEqualityComparer Instance = new();
        private TagSpanEqualityComparer() { }

        public bool Equals(TagSpan<TTag>? x, TagSpan<TTag>? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            var span1 = x.Span;
            var span2 = y.Span;

            var tag1 = x.Tag;
            var tag2 = y.Tag;

            return span1.Equals(span2) && tag1.Equals(tag2);
        }

        public int GetHashCode([DisallowNull] TagSpan<TTag> obj)
            => Hash.Combine(obj.Tag.GetHashCode(), obj.Span.GetHashCode());
    }
}

