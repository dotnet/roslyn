// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract class AsynchronousViewTaggerProvider<TTag> : AbstractAsynchronousTaggerProvider<TTag>, IViewTaggerProvider
        where TTag : ITag
    {
        protected AsynchronousViewTaggerProvider(IThreadingContext threadingContext, IAsynchronousOperationListener asyncListener)
            : base(threadingContext, asyncListener)
        {
        }

        // TypeScript still is moving to calling the new constructor that does not take a IForegroundNotificationService.
        // Until then, we can fetch one from another service of ours that already does. When TypeScript moves calling the
        // new constructor, this should be deleted.
        [Obsolete("This overload exists for TypeScript compatibility only and should not be used in new code.")]
        protected AsynchronousViewTaggerProvider(IThreadingContext threadingContext, IAsynchronousOperationListener asyncListener, IForegroundNotificationService _)
            : base(threadingContext, asyncListener)
        {
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(subjectBuffer));

            if (subjectBuffer == null)
                throw new ArgumentNullException(nameof(subjectBuffer));

            return this.CreateTaggerWorker<T>(textView, subjectBuffer);
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            => CreateTagger<T>(textView, buffer);
    }
}
