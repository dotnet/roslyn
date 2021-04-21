// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private partial class TagSource
        {
            public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

            private void OnTagsChangedForBuffer(
                ICollection<KeyValuePair<ITextBuffer, DiffResult>> changes, bool initialTags)
            {
                this.AssertIsForeground();

                foreach (var change in changes)
                {
                    if (change.Key != _subjectBuffer)
                        continue;

                    // Removed tags are always treated as high pri, so we can clean their stale
                    // data out from the ui immediately.
                    _highPriTagsChangedQueue.AddWork(change.Value.Removed);

                    // Added tags are run at normal priority, except in the case where this is the
                    // initial batch of tags.  We want those to appear immediately to make the UI
                    // show up quickly.
                    var addedTagsQueue = initialTags ? _highPriTagsChangedQueue : _normalPriTagsChangedQueue;
                    addedTagsQueue.AddWork(change.Value.Added);
                }
            }

            private Task ProcessTagsChangedAsync(
                ImmutableArray<NormalizedSnapshotSpanCollection> snapshotSpans, CancellationToken cancellationToken)
            {
                var tagsChanged = this.TagsChanged;
                if (tagsChanged == null)
                    return Task.CompletedTask;

                foreach (var collection in snapshotSpans)
                {
                    var coalesced = CoalesceSpans(collection);
                    if (coalesced.Count == 0)
                        continue;

                    foreach (var span in coalesced)
                        tagsChanged(this, new SnapshotSpanEventArgs(span));
                }

                return Task.CompletedTask;
            }

            internal static NormalizedSnapshotSpanCollection CoalesceSpans(NormalizedSnapshotSpanCollection normalizedSpans)
            {
                var snapshot = normalizedSpans.First().Snapshot;

                // Coalesce the spans if there are a lot of them.
                if (normalizedSpans.Count > CoalesceDifferenceCount)
                {
                    // Spans are normalized.  So to find the whole span we just go from the
                    // start of the first span to the end of the last span.
                    normalizedSpans = new NormalizedSnapshotSpanCollection(snapshot.GetSpanFromBounds(
                        normalizedSpans.First().Start,
                        normalizedSpans.Last().End));
                }

                return normalizedSpans;
            }
        }
    }
}
