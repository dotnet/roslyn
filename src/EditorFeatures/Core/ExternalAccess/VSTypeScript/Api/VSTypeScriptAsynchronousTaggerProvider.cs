// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

// TODO: remove
internal abstract class VSTypeScriptAsynchronousTaggerProvider<TTag> : AsynchronousViewTaggerProvider<TTag>
    where TTag : ITag
{
    [Obsolete("Use constructor that takes VSTypeScriptTaggerHost")]
    protected VSTypeScriptAsynchronousTaggerProvider(TaggerHost taggerHost)
        : base(taggerHost, FeatureAttribute.Classification)
    {
    }

    protected VSTypeScriptAsynchronousTaggerProvider(VSTypeScriptTaggerHost taggerHost)
        : base(taggerHost.UnderlyingObject, FeatureAttribute.Classification)
    {
    }
}

internal abstract class VSTypeScriptAsynchronousTaggerProvider2<TTag> : AsynchronousViewTaggerProvider<TTag>
    where TTag : ITag
{
    protected VSTypeScriptAsynchronousTaggerProvider2(VSTypeScriptTaggerHost taggerHost)
        : base(taggerHost.UnderlyingObject, FeatureAttribute.Classification)
    {
    }

    protected sealed override bool TryAddSpansToTag(ITextView? textView, ITextBuffer subjectBuffer, ref TemporaryArray<SnapshotSpan> result)
    {
        using var _ = ArrayBuilder<SnapshotSpan>.GetInstance(out var builder);
        if (TryAddSpansToTagImpl(textView, subjectBuffer, builder))
        {
            foreach (var item in builder)
            {
                result.Add(item);
            }

            return true;
        }

        return false;
    }

    protected abstract bool TryAddSpansToTagImpl(ITextView? textView, ITextBuffer subjectBuffer, ICollection<SnapshotSpan> result);
}
