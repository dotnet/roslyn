// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(asyncListener, notificationService)
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

            return this.GetOrCreateTagger<T>(textView, subjectBuffer);
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        {
            return CreateTagger<T>(textView, buffer);
        }
    }
}
