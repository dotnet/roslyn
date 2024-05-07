// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

/// <summary>
/// Root type of all roslyn <see cref="ITagger{T}"/> implementations.  This adds specialized hooks that allow for tags
/// to be added to a preexisting list, rather than generating a fresh list instance.  This can help with avoiding
/// garbage by allowing us to pool intermediary lists (especially as some high-level aggregating taggers defer to
/// intermediary taggers).
/// </summary>
internal abstract class EfficientTagger<TTag> : ITagger<TTag>, IDisposable where TTag : ITag
{
    /// <summary>
    /// Produce the set of tags with the requested <paramref name="spans"/>, adding those tags to <paramref name="tags"/>.
    /// </summary>
    public abstract void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<ITagSpan<TTag>> tags);

    public abstract void Dispose();

    /// <summary>
    /// Default impl of the core <see cref="ITagger{T}"/> interface.  Forces an allocation.
    /// </summary>
    public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        => SegmentedListPool.ComputeList(
            static (args, tags) => args.@this.AddTags(args.spans, tags),
            (@this: this, spans),
            _: (ITagSpan<TTag>?)null);

    public virtual event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

    protected void OnTagsChanged(object? sender, SnapshotSpanEventArgs e)
        => TagsChanged?.Invoke(this, e);
}
