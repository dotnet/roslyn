// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal sealed class AsynchronousViewTaggerProviderWithTagSource<TTag> :
        AbstractAsynchronousTaggerProvider<ProducerPopulatedTagSource<TTag>, TTag>,
        IViewTaggerProvider
        where TTag : ITag
    {
        private readonly IAsynchronousTaggerDataSource<TTag> _dataSource;
        private readonly CreateTagSource<ProducerPopulatedTagSource<TTag>, TTag> _createTagSource;

        public AsynchronousViewTaggerProviderWithTagSource(
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService,
            CreateTagSource<ProducerPopulatedTagSource<TTag>, TTag> createTagSource)
            : base(asyncListener, notificationService)
        {
            this._dataSource = dataSource;
            this._createTagSource = createTagSource;
        }

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

        protected sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out ProducerPopulatedTagSource<TTag> tagSource)
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
            var tagSource = _createTagSource == null ? null : _createTagSource(textViewOpt, subjectBuffer, this.AsyncListener, this.NotificationService);
            return tagSource ?? new ProducerPopulatedTagSource<TTag>(textViewOpt, subjectBuffer, _dataSource, AsyncListener, NotificationService);
        }
    }
}