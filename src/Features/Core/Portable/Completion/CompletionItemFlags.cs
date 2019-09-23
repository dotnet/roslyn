// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
    [Flags]
    internal enum CompletionItemFlags
    {
        None = 0x0,

        /// <summary>
        /// Indicates this <see cref="CompletionItem"/> is cached and reused across completion sessions. 
        /// This might be used by completion system for things like deciding whether it can safaly cache and reuse
        /// other data correspodning to this item.
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
}
