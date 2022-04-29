// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
                ICollection<KeyValuePair<ITextBuffer, DiffResult>> changes, bool highPriority)
            {
                _dataSource.ThreadingContext.ThrowIfNotOnUIThread();

                foreach (var change in changes)
                {
                    if (change.Key != _subjectBuffer)
                        continue;

                    // Removed tags are always treated as high pri, so we can clean their stale
                    // data out from the ui immediately.
                    _highPriTagsChangedQueue.AddWork(change.Value.Removed);

                    // Added tags are run at the requested priority.
                    var addedTagsQueue = highPriority ? _highPriTagsChangedQueue : _normalPriTagsChangedQueue;
                    addedTagsQueue.AddWork(change.Value.Added);
                }
            }

            private ValueTask ProcessTagsChangedAsync(
                ImmutableSegmentedList<NormalizedSnapshotSpanCollection> snapshotSpans, CancellationToken cancellationToken)
            {
                var tagsChanged = this.TagsChanged;
                if (tagsChanged == null)
                    return ValueTaskFactory.CompletedTask;

                foreach (var collection in snapshotSpans)
                {
                    if (collection.Count == 0)
                        continue;

                    var snapshot = collection.First().Snapshot;

                    // Coalesce the spans if there are a lot of them.
                    var coalesced = collection.Count > CoalesceDifferenceCount
                        ? new NormalizedSnapshotSpanCollection(snapshot.GetSpanFromBounds(collection.First().Start, collection.Last().End))
                        : collection;

                    foreach (var span in coalesced)
                        tagsChanged(this, new SnapshotSpanEventArgs(span));
                }

                return ValueTaskFactory.CompletedTask;
            }
        }
    }
}
