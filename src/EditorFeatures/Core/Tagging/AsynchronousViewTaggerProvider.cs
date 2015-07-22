#if false
using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
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
        AsynchronousTaggerDataSource<TTag>, IViewTaggerProvider
        where TTag : ITag
    {
        private readonly IViewTaggerProvider _underlyingTagger;

        protected AsynchronousViewTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            _underlyingTagger = new AsynchronousViewTaggerProviderWithTagSource<TTag>(
                this, asyncListener, notificationService);
        }

        public virtual ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return _underlyingTagger.CreateTagger<T>(textView, buffer);
        }
    }
}
#endif