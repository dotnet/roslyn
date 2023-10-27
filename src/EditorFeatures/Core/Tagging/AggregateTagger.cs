// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal abstract class AbstractAggregateTagger<TTag>(ImmutableArray<ITagger<TTag>> taggers) : ITagger<TTag>, IDisposable
    where TTag : ITag
{
    protected readonly ImmutableArray<ITagger<TTag>> Taggers = taggers;

    IEnumerable<ITagSpan<TTag>> ITagger<TTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        => GetTags(spans);

    public abstract SegmentedList<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans);

    public void Dispose()
    {
        foreach (var tagger in this.Taggers)
            (tagger as IDisposable)?.Dispose();
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged
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
internal sealed class SimpleAggregateTagger<TTag>(ImmutableArray<ITagger<TTag>> taggers)
    : AbstractAggregateTagger<TTag>(taggers)
    where TTag : ITag
{
    public override SegmentedList<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        var result = new SegmentedList<ITagSpan<TTag>>();

        foreach (var tagger in this.Taggers)
            result.AddRange(tagger.GetTags(spans));

        return result;
    }
}
