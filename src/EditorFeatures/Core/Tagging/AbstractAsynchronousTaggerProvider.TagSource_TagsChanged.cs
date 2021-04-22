// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        /// <summary>
        /// <para>The <see cref="TagSource"/> is the core part of our asynchronous
        /// tagging infrastructure. It is the coordinator between <see cref="ProduceTagsAsync(TaggerContext{TTag})"/>s,
        /// <see cref="ITaggerEventSource"/>s, and <see cref="ITagger{T}"/>s.</para>
        /// 
        /// <para>The <see cref="TagSource"/> is the type that actually owns the
        /// list of cached tags. When an <see cref="ITaggerEventSource"/> says tags need to be  recomputed,
        /// the tag source starts the computation and calls <see cref="ProduceTagsAsync(TaggerContext{TTag})"/> to build
        /// the new list of tags. When that's done, the tags are stored in <see cref="CachedTagTrees"/>. The 
        /// tagger, when asked for tags from the editor, then returns the tags that are stored in 
        /// <see cref="CachedTagTrees"/></para>
        /// 
        /// <para>There is a one-to-many relationship between <see cref="TagSource"/>s
        /// and <see cref="ITagger{T}"/>s. Special cases, like reference highlighting (which processes multiple
        /// subject buffers at once) have their own providers and tag source derivations.</para>
        /// </summary>
        private partial class TagSource
        {
            /// <summary>
            /// The batch change notifier that we use to throttle update to the UI.
            /// </summary>
            private readonly BatchChangeNotifier _batchChangeNotifier;
            private readonly CancellationTokenSource _batchChangeTokenSource;

            public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

            private void OnTagsChangedForBuffer(
                ICollection<KeyValuePair<ITextBuffer, DiffResult>> changes, bool initialTags)
            {
                this.AssertIsForeground();

                // Note: This operation is uncancellable. Once we've been notified here, our cached tags
                // in the tag source are new. If we don't update the UI of the editor then we will end
                // up in an inconsistent state between us and the editor where we have new tags but the
                // editor will never know.

                foreach (var change in changes)
                {
                    if (change.Key != _subjectBuffer)
                    {
                        continue;
                    }

                    // Now report them back to the UI on the main thread.

                    // We ask to update UI immediately for removed tags, or for the very first set of tags created.
                    NotifyEditors(change.Value.Removed, TaggerDelay.NearImmediate);
                    NotifyEditors(change.Value.Added, initialTags ? TaggerDelay.NearImmediate : this.AddedTagNotificationDelay);
                }
            }

            private void NotifyEditors(NormalizedSnapshotSpanCollection changes, TaggerDelay delay)
            {
                this.AssertIsForeground();

                if (changes.Count == 0)
                {
                    // nothing to do.
                    return;
                }

                if (delay == TaggerDelay.NearImmediate)
                {
                    // if delay is immediate, we let notifier knows about the change right away
                    _batchChangeNotifier.EnqueueChanges(changes);
                    return;
                }

                // if delay is anything more than that, we let notifier knows about the change after given delay
                // event notification is only cancellable when disposing of the tagger.
                this.RegisterNotification(
                    () => _batchChangeNotifier.EnqueueChanges(changes),
                    (int)delay.ComputeTimeDelay(_subjectBuffer).TotalMilliseconds,
                    _batchChangeTokenSource.Token);
            }

            private void NotifyEditorNow(NormalizedSnapshotSpanCollection normalizedSpans)
            {
                _batchChangeNotifier.AssertIsForeground();

                using (Logger.LogBlock(FunctionId.Tagger_BatchChangeNotifier_NotifyEditorNow, CancellationToken.None))
                {
                    if (normalizedSpans.Count == 0)
                    {
                        return;
                    }

                    var tagsChanged = this.TagsChanged;
                    if (tagsChanged == null)
                    {
                        return;
                    }

                    normalizedSpans = CoalesceSpans(normalizedSpans);

                    // Don't use linq here.  It's a hotspot.
                    foreach (var span in normalizedSpans)
                        this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
                }
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
