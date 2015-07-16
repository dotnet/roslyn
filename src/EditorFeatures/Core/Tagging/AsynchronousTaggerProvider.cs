using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Convenience class that provides a default implementation for most of what is required to
    /// be an <see cref="ITaggerProvider"/> that operates in an asynchronous fashion.
    /// </summary>
    internal abstract class AsynchronousTaggerProvider<TTag> :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<TTag>
        where TTag : ITag
    {
        private readonly ITaggerProvider _underlyingTagger;

        public virtual IEqualityComparer<TTag> TagComparer => null;
        public virtual TaggerDelay? UIUpdateDelay => null;
        public virtual bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public virtual IEnumerable<Option<bool>> Options => null;
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public abstract bool RemoveTagsThatIntersectEdits { get; }
        public abstract SpanTrackingMode SpanTrackingMode { get; }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        protected AsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            _underlyingTagger = new AsynchronousBufferTaggerProviderWithTagSource<TTag>(
                this, asyncListener, notificationService, createTagSource: null);
        }

        public virtual ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return _underlyingTagger.CreateTagger<T>(buffer);
        }

        public virtual IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return null;
        }

        public abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);
        public abstract ITagProducer<TTag> CreateTagProducer();
    }
}
