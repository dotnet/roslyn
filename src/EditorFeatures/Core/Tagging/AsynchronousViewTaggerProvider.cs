// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract class AsynchronousViewTaggerProvider<TTag> : AbstractAsynchronousTaggerProvider<TTag>,
        IViewTaggerProvider
        where TTag : ITag
    {
        public AsynchronousViewTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
            : base(asyncListener, notificationService)
        {
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

        protected sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out TagSource<TTag> tagSource)
        {
            return textViewOpt.TryGetPerSubjectBufferProperty(subjectBuffer, UniqueKey, out tagSource);
        }

        protected sealed override void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            textViewOpt.RemovePerSubjectBufferProperty<TagSource<TTag>, ITextView>(subjectBuffer, UniqueKey);
        }

        protected sealed override void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, TagSource<TTag> tagSource)
        {
            textViewOpt.AddPerSubjectBufferProperty(subjectBuffer, UniqueKey, tagSource);
        }
    }
}