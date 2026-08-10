// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Shared]
[Export(typeof(InlayHintCacheProvider))]
internal class InlayHintCacheProvider
{
    private InlayHintCache? _cache;

    public InlayHintCache GetCache()
    {
        _cache ??= new();
        return _cache;
    }
}
