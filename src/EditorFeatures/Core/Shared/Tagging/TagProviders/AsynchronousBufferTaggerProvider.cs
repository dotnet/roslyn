// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal sealed class AsynchronousBufferTaggerProviderWithTagSource<TTag> :
        AbstractAsynchronousTaggerProvider<ProducerPopulatedTagSource<TTag>, TTag>,
        ITaggerProvider,
        IAsynchronousTaggerDataSource<TTag>
        where TTag : ITag
    {
        private readonly IAsynchronousTaggerDataSource<TTag> dataSource;
        private readonly CreateTagSource<ProducerPopulatedTagSource<TTag>, TTag> createTagSource;

        public IEqualityComparer<TTag> TagComparer => dataSource.TagComparer;
        public override TaggerDelay? UIUpdateDelay => dataSource.UIUpdateDelay;
        public SpanTrackingMode SpanTrackingMode => dataSource.SpanTrackingMode;
        public bool RemoveTagsThatIntersectEdits => dataSource.RemoveTagsThatIntersectEdits;
        public bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted => dataSource.ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted;
        public override IEnumerable<Option<bool>> Options => dataSource.Options;
        public override IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => dataSource.PerLanguageOptions;

        public AsynchronousBufferTaggerProviderWithTagSource(
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService,
            CreateTagSource<ProducerPopulatedTagSource<TTag>, TTag> createTagSource)
            : base(asyncListener, notificationService)
        {
            this.dataSource = dataSource;
            this.createTagSource = createTagSource;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException("subjectBuffer");
            }

            return this.GetOrCreateTagger<T>(null, subjectBuffer);
        }

        public ITagProducer<TTag> CreateTagProducer()
        {
            return dataSource.CreateTagProducer();
        }

        public ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return dataSource.CreateEventSource(textViewOpt, subjectBuffer);
        }

        protected override ProducerPopulatedTagSource<TTag> CreateTagSourceCore(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            var tagSource = createTagSource == null ? null : createTagSource(textViewOpt, subjectBuffer, AsyncListener, NotificationService);
            return tagSource ?? new BufferTagSource<TTag>(subjectBuffer, this, AsyncListener, NotificationService);
        }

        protected sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out ProducerPopulatedTagSource<TTag> tagSource)
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
