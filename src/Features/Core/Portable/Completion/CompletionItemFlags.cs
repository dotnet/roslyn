// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Completion;

[Flags]
internal enum CompletionItemFlags
{
    None = 0x0,

    /// <summary>
    /// Indicates this <see cref="CompletionItem"/> is cached and reused across completion sessions. 
    /// This might be used by completion system for things like deciding whether it can safely cache and reuse
    /// other data corresponding to this item.
    ///
    /// TODO: Revisit the approach we used for caching VS items.
    ///       https://github.com/dotnet/roslyn/issues/35160
    /// </summary>
    Cached = 0x1,

    /// <summary>
    /// Indicates this <see cref="CompletionItem"/> should be shown only when expanded items is requested.
    /// </summary>
    Expanded = 0x2,

    CachedAndExpanded = Cached | Expanded,
}

internal static class CompletionItemFlagsExtensions
{
    public static bool IsCached(this CompletionItemFlags flags)
        => (flags & CompletionItemFlags.Cached) != 0;

    public static bool IsExpanded(this CompletionItemFlags flags)
        => (flags & CompletionItemFlags.Expanded) != 0;
}
