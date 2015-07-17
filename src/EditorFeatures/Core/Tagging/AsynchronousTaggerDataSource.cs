using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Helper class that provides a default implementation for most of the <see cref="IAsynchronousTaggerDataSource{TTag}"/>
    /// interface.  Useful for when you only need to augment a small set of functionality.
    /// need to 
    /// </summary>
    internal abstract class AsynchronousTaggerDataSource<TTag> : IAsynchronousTaggerDataSource<TTag> where TTag : ITag
    {
        public virtual IEqualityComparer<TTag> TagComparer => null;
        public virtual TaggerDelay? UIUpdateDelay => null;
        public virtual bool IgnoreCaretMovementToExistingTag => false;
        public virtual bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public virtual IEnumerable<Option<bool>> Options => null;
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public abstract bool RemoveTagsThatIntersectEdits { get; }
        public abstract SpanTrackingMode SpanTrackingMode { get; }

        protected AsynchronousTaggerDataSource() { }

        public virtual IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Use 'null' to indicate that the tagger should tag the default set of spans.
            return null;
        }

        public abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);
        public abstract ITagProducer<TTag> CreateTagProducer();
    }
}