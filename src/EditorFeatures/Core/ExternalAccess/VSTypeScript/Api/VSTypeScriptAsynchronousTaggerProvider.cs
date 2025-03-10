// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

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
