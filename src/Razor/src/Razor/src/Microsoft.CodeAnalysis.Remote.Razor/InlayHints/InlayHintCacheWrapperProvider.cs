// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Shared]
[Export(typeof(InlayHintCacheWrapperProvider))]
internal class InlayHintCacheWrapperProvider
{
    private InlayHintCacheWrapper? _inlayHintCacheWrapper;

    public InlayHintCacheWrapper GetCache()
    {
        _inlayHintCacheWrapper ??= new();
        return _inlayHintCacheWrapper;
    }
}
