// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
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

        /// <summary>
        /// Indicates this <see cref="CompletionItem"/> is a preferred item.
        /// This is determined by the ★ prefix of display text as a temporarily measure to support preference of 
        /// IntelliCode items comparing to non-IntelliCode items. We expect that Editor will introduce this support
        /// and we will get rid of relying on the "★" then. We check both the display text and the display text
        /// prefix to account for IntelliCode item providers that may be using the prefix to include the ★.
        /// </summary>
        Preferred = 0x4,

        CachedAndExpanded = Cached | Expanded,
    }

    internal static class CompletionItemFlagsExtensions
    {
        public static bool IsCached(this CompletionItemFlags flags)
            => (flags & CompletionItemFlags.Cached) != 0;

        public static bool IsExpanded(this CompletionItemFlags flags)
            => (flags & CompletionItemFlags.Expanded) != 0;

        public static bool IsPreferredItem(this CompletionItem completionItem)
            => (completionItem.Flags & CompletionItemFlags.Preferred) != 0;

        public static CompletionItem MarkPreferredItem(this CompletionItem item)
        {
            if (item.DisplayText.StartsWith("★") || item.DisplayTextPrefix.StartsWith("★"))
                item.Flags |= CompletionItemFlags.Preferred;

            return item;
        }
    }
}
