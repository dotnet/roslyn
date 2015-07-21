using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Helper class that provides a default implementation for most of the 
    /// <see cref="IAsynchronousTaggerDataSource{TTag}"/> interface.  Useful for when you 
    /// only need to augment a small set of functionality.
    /// </summary>
    internal abstract class AsynchronousTaggerDataSource<TTag> : IAsynchronousTaggerDataSource<TTag> where TTag : ITag
    {
        public virtual TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.None;
        public virtual TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.None;
        public virtual SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;
        public virtual bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;

        public virtual IEqualityComparer<TTag> TagComparer => null;

        public virtual IEnumerable<Option<bool>> Options => null;
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        protected AsynchronousTaggerDataSource() { }

        public virtual SnapshotPoint? GetCaretPoint(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Use 'null' to indicate that the tagger should get the default caret position.
            return null;
        }

        public virtual IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Use 'null' to indicate that the tagger should tag the default set of spans.
            return null;
        }

        public abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        public virtual async Task ProduceTagsAsync(AsynchronousTaggerContext<TTag> context)
        {
            foreach (var spanToTag in context.SpansToTag)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                await ProduceTagsAsync(context, spanToTag, GetCaretPosition(context.CaretPosition, spanToTag.SnapshotSpan)).ConfigureAwait(false);
            }
        }

        private static int? GetCaretPosition(SnapshotPoint? caretPosition, SnapshotSpan snapshotSpan)
        {
            return caretPosition.HasValue && caretPosition.Value.Snapshot == snapshotSpan.Snapshot
                ? caretPosition.Value.Position : (int?)null;
        }

        public virtual Task ProduceTagsAsync(AsynchronousTaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            return SpecializedTasks.EmptyTask;
        }
    }
}