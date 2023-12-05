// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Base type for async taggers that need access to an <see cref="ITextView"/>.  Used when a tagger needs things to
    /// operate like determining what is visible to the user, or where the caret is.
    /// </summary>
    /// <typeparam name="TTag"></typeparam>
    internal abstract class AsynchronousViewTaggerProvider<TTag> : AbstractAsynchronousTaggerProvider<TTag>, IViewTaggerProvider
        where TTag : ITag
    {
        protected AsynchronousViewTaggerProvider(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, globalOptions, visibilityTracker, asyncListener)
        {
        }

#pragma warning disable CS8765 // Nullability of type of 'textView' doesn't match overridden member (derivations of this type will never receive null in this call)
        protected abstract override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer);
#pragma warning restore

        public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(subjectBuffer));

            if (subjectBuffer == null)
                throw new ArgumentNullException(nameof(subjectBuffer));

            return this.CreateTaggerWorker<T>(textView, subjectBuffer);
        }

        ITagger<T>? IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            => CreateTagger<T>(textView, buffer);
    }
}
