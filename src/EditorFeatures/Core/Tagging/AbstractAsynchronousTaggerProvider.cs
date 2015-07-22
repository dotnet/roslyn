// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Base type of all asynchronous tagger providers (<see cref="ITaggerProvider"/> and <see cref="IViewTaggerProvider"/>). 
    /// </summary>
    internal abstract class AbstractAsynchronousTaggerProvider<TTag> : AsynchronousTaggerDataSource<TTag>
        where TTag : ITag
    {
        protected readonly object UniqueKey = new object();
        protected readonly IAsynchronousOperationListener AsyncListener;
        protected readonly IForegroundNotificationService NotificationService;

        public AbstractAsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            this.AsyncListener = asyncListener;
            this.NotificationService = notificationService;
        }

        protected T GetOption<T>(ITextBuffer buffer, Option<T> option)
        {
            return buffer.GetOption(option);
        }

        protected T GetOption<T>(ITextBuffer buffer, PerLanguageOption<T> option)
        {
            return buffer.GetOption(option);
        }

        private TagSource<TTag> CreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            var options = this.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
            var perLanguageOptions = this.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

            if (options.Any(option => !this.GetOption(subjectBuffer, option)) ||
                perLanguageOptions.Any(option => !this.GetOption(subjectBuffer, option)))
            {
                return null;
            }

            return new TagSource<TTag>(textViewOpt, subjectBuffer, this, AsyncListener, NotificationService);
        }

        protected abstract bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out TagSource<TTag> tagSource);
        protected abstract void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, TagSource<TTag> tagSource);
        protected abstract void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        protected ITagger<T> GetOrCreateTagger<T>(ITextView textViewOpt, ITextBuffer subjectBuffer) where T : ITag
        {
            if (!this.GetOption(subjectBuffer, EditorComponentOnOffOptions.Tagger))
            {
                return null;
            }

            var tagSource = GetOrCreateTagSource(textViewOpt, subjectBuffer);
            return tagSource == null
                ? null
                : new AsynchronousTagger<TTag>(this.AsyncListener, this.NotificationService, tagSource, subjectBuffer) as ITagger<T>;
        }

        protected TagSource<TTag> GetOrCreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            TagSource<TTag> tagSource;
            if (!this.TryRetrieveTagSource(textViewOpt, subjectBuffer, out tagSource))
            {
                tagSource = this.CreateTagSource(textViewOpt, subjectBuffer);
                if (tagSource == null)
                {
                    return null;
                }

                this.StoreTagSource(textViewOpt, subjectBuffer, tagSource);
                tagSource.Disposed += (s, e) => this.RemoveTagSource(textViewOpt, subjectBuffer);
            }

            return tagSource;
        }
    }
}
