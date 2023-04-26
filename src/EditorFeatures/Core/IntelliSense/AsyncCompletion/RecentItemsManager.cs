// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export]
    internal class RecentItemsManager
    {
        private const int MaxMRUSize = 10;

        /// <summary>
        /// Guard for <see cref="RecentItems"/>
        /// </summary>
        private readonly object _mruUpdateLock = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RecentItemsManager()
        {
        }

        private ImmutableArray<string> RecentItems { get; set; } = ImmutableArray<string>.Empty;

        public void MakeMostRecentItem(CompletionItem item)
        {
            lock (_mruUpdateLock)
            {
                var items = RecentItems;
                items = items.Remove(item.FilterText);

                if (items.Length == MaxMRUSize)
                {
                    // Remove the least recent item.
                    items = items.RemoveAt(0);
                }

                RecentItems = items.Add(item.FilterText);
            }
        }

        public int GetRecentItemIndex(CompletionItem item)
        {
            return RecentItems.IndexOf(item.FilterText);
        }
    }
}
