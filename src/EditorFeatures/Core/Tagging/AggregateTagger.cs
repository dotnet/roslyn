// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal abstract class RoslynTagger<TTag> : ITagger<TTag> where TTag : ITag
{
    public abstract void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<TTag>> tags);

    public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        var pooledTags = Classifier.GetPooledList<ITagSpan<TTag>>(out var tags);
        this.AddTags(spans, tags);

        if (tags.Count == 0)
        {
            pooledTags.Dispose();
            return Array.Empty<ITagSpan<TTag>>();
        }

        // intentionally do not dispose.
        return tags;
    }

    public virtual event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    protected void OnTagsChanged(object? sender, SnapshotSpanEventArgs e)
        => TagsChanged?.Invoke(this, e);
}

internal abstract class AbstractAggregateTagger<TTag>(ImmutableArray<RoslynTagger<TTag>> taggers) : RoslynTagger<TTag>, IDisposable
    where TTag : ITag
{
    protected readonly ImmutableArray<RoslynTagger<TTag>> Taggers = taggers;

    //IEnumerable<ITagSpan<TTag>> ITagger<TTag>.GetTags(NormalizedSnapshotSpanCollection spans)
    //    => GetTags(spans);

    // public abstract SegmentedList<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans);

    public void Dispose()
    {
        foreach (var tagger in this.Taggers)
            (tagger as IDisposable)?.Dispose();
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged
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
/// Simple tagger that aggregates the underlying syntax/semantic compiler/analyzer taggers and presents them as
/// a single event source and source of tags.
/// </summary>
internal sealed class SimpleAggregateTagger<TTag>(ImmutableArray<RoslynTagger<TTag>> taggers)
    : AbstractAggregateTagger<TTag>(taggers)
    where TTag : ITag
{
    public override void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<TTag>> tags)
    {
        foreach (var tagger in this.Taggers)
            tagger.AddTags(spans, tags);
    }
}
