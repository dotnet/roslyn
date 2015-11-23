using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private sealed partial class Tagger : IAccurateTagger<TTag>, IDisposable
        {
            #region Fields that can be accessed from either thread

            private readonly ITextBuffer _subjectBuffer;

            private readonly TagSource _tagSource;

            #endregion

            #region Fields that can only be accessed from the foreground thread

            /// <summary>
            /// The batch change notifier that we use to throttle update to the UI.
            /// </summary>
            private readonly BatchChangeNotifier _batchChangeNotifier;

            #endregion

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public Tagger(
                IAsynchronousOperationListener listener,
                IForegroundNotificationService notificationService,
                TagSource tagSource,
                ITextBuffer subjectBuffer)
            {
                Contract.ThrowIfNull(subjectBuffer);

                _subjectBuffer = subjectBuffer;
                _batchChangeNotifier = new BatchChangeNotifier(subjectBuffer, listener, notificationService, ReportChangedSpan);

                _tagSource = tagSource;

                _tagSource.OnTaggerAdded(this);
                _tagSource.TagsChangedForBuffer += OnTagsChangedForBuffer;
                _tagSource.Paused += OnPaused;
                _tagSource.Resumed += OnResumed;
            }

            public void Dispose()
            {
                _tagSource.Resumed -= OnResumed;
                _tagSource.Paused -= OnPaused;
                _tagSource.TagsChangedForBuffer -= OnTagsChangedForBuffer;
                _tagSource.OnTaggerDisposed(this);
            }

            private void ReportChangedSpan(SnapshotSpan changeSpan)
            {
                _batchChangeNotifier.AssertIsForeground();
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(changeSpan));
            }

            private void OnPaused(object sender, EventArgs e)
            {
                _batchChangeNotifier.Pause();
            }

            private void OnResumed(object sender, EventArgs e)
            {
                _batchChangeNotifier.Resume();
            }

            private void OnTagsChangedForBuffer(ICollection<KeyValuePair<ITextBuffer, NormalizedSnapshotSpanCollection>> changes)
            {
                this._tagSource.AssertIsForeground();

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
                    _batchChangeNotifier.EnqueueChanges(change.Value);
                }
            }

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection requestedSpans)
            {
                return GetTagsWorker(requestedSpans, accurate: false, cancellationToken: CancellationToken.None);
            }

            public IEnumerable<ITagSpan<TTag>> GetAllTags(NormalizedSnapshotSpanCollection requestedSpans, CancellationToken cancellationToken)
            {
                return GetTagsWorker(requestedSpans, accurate: true, cancellationToken: cancellationToken);
            }

            private IEnumerable<ITagSpan<TTag>> GetTagsWorker(
                NormalizedSnapshotSpanCollection requestedSpans,
                bool accurate,
                CancellationToken cancellationToken)
            {
                if (requestedSpans.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                var buffer = requestedSpans.First().Snapshot.TextBuffer;
                var tags = accurate
                    ? _tagSource.GetAccurateTagIntervalTreeForBuffer(buffer, cancellationToken)
                    : _tagSource.GetTagIntervalTreeForBuffer(buffer);

                if (tags == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                return tags.GetIntersectingTagSpans(requestedSpans);
            }
        }
    }
}
