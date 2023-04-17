﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens.CodeLensCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

internal sealed class CodeLensCache : ResolveCache<CodeLensCacheEntry>
{
    /// <summary>
    /// Use a cache size of 3 as a rough baseline - generally 1 is enough
    /// however there are certain edge cases (like quickly switching active documents)
    /// where a resolve request could be made for an older textDocument/codelens request.
    /// 
    /// If we discover this number is too low, we can adjust in the future.
    /// </summary>
    public CodeLensCache() : base(maxCacheSize: 3)
    {
    }

    /// <summary>
    /// Cached data need to resolve a specific code lens item
    /// </summary>
    /// <param name="CodeLensMembers">the list of nodes and locations for codelens members</param>
    /// <param name="SyntaxVersion">the syntax version the codelenses were calculated against (to validate the resolve request)</param>
    internal record CodeLensCacheEntry(ImmutableArray<CodeLensMember> CodeLensMembers, VersionStamp SyntaxVersion);
}
