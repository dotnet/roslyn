// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal abstract class AsynchronousTaggerProvider<TTag>(TaggerHost taggerHost, string featureName)
    : AbstractAsynchronousTaggerProvider<TTag>(taggerHost, featureName), ITaggerProvider
    where TTag : ITag
{
    public EfficientTagger<TTag>? CreateTagger(ITextBuffer subjectBuffer)
    {
        if (subjectBuffer == null)
            throw new ArgumentNullException(nameof(subjectBuffer));

        return this.CreateEfficientTagger(null, subjectBuffer);
    }

    ITagger<T>? ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
    {
        var tagger = CreateTagger(buffer);
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
