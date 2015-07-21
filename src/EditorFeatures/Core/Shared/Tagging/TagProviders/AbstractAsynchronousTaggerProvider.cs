// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal delegate TTagSource CreateTagSource<TTagSource, TTag>(
        ITextView textView, ITextBuffer subjectBuffer,
        IAsynchronousOperationListener asyncListener,
        IForegroundNotificationService notificationService) where TTagSource : TagSource<TTag> where TTag : ITag;

    /// <summary>
    /// Base type of all asynchronous tagger providers (<see cref="ITaggerProvider"/> and <see cref="IViewTaggerProvider"/>). 
    /// </summary>
    internal abstract class AbstractAsynchronousTaggerProvider<TTagSource, TTag>
        where TTagSource : TagSource<TTag>
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

        protected abstract TTagSource CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer);

        private TTagSource CreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            var options = this.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
            var perLanguageOptions = this.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

            if (options.Any((option) => !this.GetOption(subjectBuffer, option)) ||
                perLanguageOptions.Any((option) => !this.GetOption(subjectBuffer, option)))
            {
                return null;
            }

            return CreateTagSourceCore(textViewOpt, subjectBuffer);
        }

        /// <summary>
        /// Feature on/off options.
        /// </summary>
        public virtual IEnumerable<Option<bool>> Options => SpecializedCollections.EmptyEnumerable<Option<bool>>();
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

        protected abstract bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out TTagSource tagSource);
        protected abstract void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, TTagSource tagSource);
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

        protected TTagSource GetOrCreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            TTagSource tagSource;
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
