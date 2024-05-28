// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

/// <summary>
/// Base type for async taggers that need access to an <see cref="ITextView"/>.  Used when a tagger needs things to
/// operate like determining what is visible to the user, or where the caret is.
/// </summary>
/// <typeparam name="TTag"></typeparam>
internal abstract class AsynchronousViewTaggerProvider<TTag>(TaggerHost taggerHost, string featureName)
    : AbstractAsynchronousTaggerProvider<TTag>(taggerHost, featureName), IViewTaggerProvider
    where TTag : ITag
{
#pragma warning disable CS8765 // Nullability of type of 'textView' doesn't match overridden member (derivations of this type will never receive null in this call)
    protected abstract override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer);
#pragma warning restore

    public EfficientTagger<TTag>? CreateTagger(ITextView textView, ITextBuffer subjectBuffer)
    {
        if (textView == null)
            throw new ArgumentNullException(nameof(subjectBuffer));

        if (subjectBuffer == null)
            throw new ArgumentNullException(nameof(subjectBuffer));

        return this.CreateEfficientTagger(textView, subjectBuffer);
    }

    ITagger<T>? IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
    {
        var tagger = CreateTagger(textView, buffer);
        if (tagger is null)
            return null;

        // If we're not able to convert the tagger we instantiated to the type the caller wants, then make sure we
        // dispose of it now.  The tagger will have added a ref to the underlying tagsource, and we have to make
        // sure we return that to the proper starting value.
        if (tagger is not ITagger<T> typedTagger)
        {
            tagger.Dispose();
            return null;
        }

        return typedTagger;
    }
}
