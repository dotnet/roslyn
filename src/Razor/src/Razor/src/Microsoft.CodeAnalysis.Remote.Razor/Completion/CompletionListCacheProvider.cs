// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Shared]
[Export(typeof(CompletionListCacheProvider))]
internal class CompletionListCacheProvider
{
    private CompletionListCache? _cache;

    public CompletionListCache GetCache()
    {
        _cache ??= new();
        return _cache;
    }
}
