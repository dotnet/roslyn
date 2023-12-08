// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

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
/// The final set of tags produced by any <see cref="AddTags"/> request is just the aggregation of all the tags produced
/// by the individual <paramref name="taggers"/>.
/// </summary>
internal sealed class SimpleAggregateTagger<TTag>(ImmutableArray<EfficientTagger<TTag>> taggers)
    : AbstractAggregateTagger<TTag>(taggers)
    where TTag : ITag
{
    public override void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<TTag>> tags)
    {
        foreach (var tagger in this.Taggers)
            tagger.AddTags(spans, tags);
    }
}
