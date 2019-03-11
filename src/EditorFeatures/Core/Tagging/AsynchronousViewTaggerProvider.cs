// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        protected AsynchronousViewTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(threadingContext, asyncListener, notificationService)
        {
        }

        // TypeScript still is moving to calling the new constructor that takes an IThreadingContext. Until then, we can fetch one from another service of ours that
        // already does. When TypeScript moves calling the new constructor, this should be deleted.
        [Obsolete("This overload exists for TypeScript compatibility only and should not be used in new code.")]
        protected AsynchronousViewTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : this(((Implementation.ForegroundNotification.ForegroundNotificationService)notificationService).ThreadingContext, asyncListener, notificationService)
        {
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.CreateTaggerWorker<T>(textView, subjectBuffer);
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        {
            return CreateTagger<T>(textView, buffer);
        }
    }
}
