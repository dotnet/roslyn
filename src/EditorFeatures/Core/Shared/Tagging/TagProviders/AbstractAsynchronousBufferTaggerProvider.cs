// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractAsynchronousBufferTaggerProvider<TTag> :
        AbstractAsynchronousTaggerProvider<ProducerPopulatedTagSource<TTag>, TTag>, ITaggerProvider
        where TTag : ITag
    {
        public AbstractAsynchronousBufferTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
            : base(asyncListener, notificationService)
        {
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.GetOrCreateTagger<T>(null, subjectBuffer);
        }

        protected abstract bool RemoveTagsThatIntersectEdits { get; }
        protected abstract SpanTrackingMode SpanTrackingMode { get; }

        protected abstract ITagProducer<TTag> CreateTagProducer();
        protected abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        protected override ProducerPopulatedTagSource<TTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return new BufferTagSource<TTag>(
                subjectBuffer,
                CreateTagProducer(),
                CreateEventSource(textViewOpt, subjectBuffer),
                AsyncListener,
                NotificationService,
                this.RemoveTagsThatIntersectEdits,
                this.SpanTrackingMode);
        }

        internal sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out ProducerPopulatedTagSource<TTag> tagSource)
        {
            return subjectBuffer.Properties.TryGetProperty(UniqueKey, out tagSource);
        }

        protected sealed override void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, ProducerPopulatedTagSource<TTag> tagSource)
        {
            subjectBuffer.Properties.AddProperty(UniqueKey, tagSource);
        }

        protected sealed override void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            subjectBuffer.Properties.RemoveProperty(UniqueKey);
        }
    }
}
