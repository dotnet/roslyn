// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.using System;

using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export]
    internal class RecentItemsManager
    {
        private const int MaxMRUSize = 10;

        /// <summary>
        /// Guard for <see cref="RecentItems"/>
        /// </summary>
        private readonly object _mruUpdateLock = new object();

        [ImportingConstructor]
        public RecentItemsManager()
        {
        }

        public ImmutableArray<string> RecentItems { get; private set; } = ImmutableArray<string>.Empty;

        public void MakeMostRecentItem(string item)
        {
            lock (_mruUpdateLock)
            {
                var items = RecentItems;
                items = items.Remove(item);

                if (items.Length == MaxMRUSize)
                {
                    // Remove the least recent item.
                    items = items.RemoveAt(0);
                }

                RecentItems = items.Add(item);
            }
        }
    }
}
