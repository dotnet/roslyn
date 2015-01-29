// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractAsynchronousViewTaggerProvider<TTag> :
        AbstractAsynchronousTaggerProvider<ProducerPopulatedTagSource<TTag>, TTag>, IViewTaggerProvider
        where TTag : ITag
    {
        public AbstractAsynchronousViewTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
            : base(asyncListener, notificationService)
        {
        }

        protected abstract bool RemoveTagsThatIntersectEdits { get; }
        protected abstract SpanTrackingMode SpanTrackingMode { get; }

        protected abstract ITagProducer<TTag> CreateTagProducer();
        protected abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.GetOrCreateTagger<T>(textView, subjectBuffer);
        }

        internal sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out ProducerPopulatedTagSource<TTag> tagSource)
        {
            return textViewOpt.TryGetPerSubjectBufferProperty(subjectBuffer, UniqueKey, out tagSource);
        }

        protected sealed override void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            textViewOpt.RemovePerSubjectBufferProperty<ProducerPopulatedTagSource<TTag>, ITextView>(subjectBuffer, UniqueKey);
        }

        protected sealed override void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, ProducerPopulatedTagSource<TTag> tagSource)
        {
            textViewOpt.AddPerSubjectBufferProperty(subjectBuffer, UniqueKey, tagSource);
        }

        protected override ProducerPopulatedTagSource<TTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return new ViewTagSource<TTag>(
                textViewOpt,
                subjectBuffer,
                CreateTagProducer(),
                CreateEventSource(textViewOpt, subjectBuffer),
                AsyncListener,
                NotificationService,
                this.RemoveTagsThatIntersectEdits,
                this.SpanTrackingMode);
        }
    }
}
