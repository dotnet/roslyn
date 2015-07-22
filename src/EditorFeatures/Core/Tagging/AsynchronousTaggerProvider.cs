using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Convenience class that provides a default implementation for most of what is required to
    /// be an <see cref="ITaggerProvider"/> that operates in an asynchronous fashion.
    /// </summary>
    internal abstract class AsynchronousTaggerProvider<TTag> :
        AsynchronousTaggerDataSource<TTag>, ITaggerProvider
        where TTag : ITag
    {
        private readonly ITaggerProvider _underlyingTagger;

        protected AsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            _underlyingTagger = new AsynchronousBufferTaggerProviderWithTagSource<TTag>(
                this, asyncListener, notificationService);
        }

        public virtual ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return _underlyingTagger.CreateTagger<T>(buffer);
        }
    }
}