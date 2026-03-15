// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using RoslynCallHierarchyItem = Microsoft.CodeAnalysis.CallHierarchy.CallHierarchyItem;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy.CallHierarchyCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

internal sealed class CallHierarchyCache : ResolveCache<CallHierarchyCacheEntry>
{
    public CallHierarchyCache() : base(maxCacheSize: 3)
    {
    }

    /// <summary>
    /// Cached data needed to resolve a specific call hierarchy item.
    /// </summary>
    internal sealed record CallHierarchyCacheEntry(ImmutableArray<RoslynCallHierarchyItem> Items);
}
