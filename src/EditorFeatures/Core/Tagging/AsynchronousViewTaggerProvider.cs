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
    /// be an <see cref="IViewTaggerProvider"/> that operates in an asynchronous fashion.
    /// </summary>
    internal abstract class AsynchronousViewTaggerProvider<TTag> :
        ForegroundThreadAffinitizedObject,
        IViewTaggerProvider,
        IAsynchronousTaggerDataSource<TTag>
        where TTag : ITag
    {
        private readonly IViewTaggerProvider _underlyingTagger;

        public virtual TaggerDelay? UIUpdateDelay => null;
        public virtual IEqualityComparer<TTag> TagComparer => null;
        public virtual bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => false;
        public virtual IEnumerable<Option<bool>> Options => null;
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        public abstract bool RemoveTagsThatIntersectEdits { get; }
        public abstract SpanTrackingMode SpanTrackingMode { get; }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        protected AsynchronousViewTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            _underlyingTagger = new AsynchronousViewTaggerProviderWithTagSource<TTag>(
                this, asyncListener, notificationService, createTagSource: null);
        }

        public virtual ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _underlyingTagger.CreateTagger<T>(textView, buffer);
        }

        public virtual IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return null;
        }

        public abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);
        public abstract ITagProducer<TTag> CreateTagProducer();
    }
}