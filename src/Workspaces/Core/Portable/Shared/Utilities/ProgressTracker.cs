// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Utility class that can be used to track the progress of an operation in a threadsafe manner.
    /// </summary>
    internal class ProgressTracker
    {
        private int completedItems = 0;
        private int totalItems = 0;

        private readonly Action<int, int> updateActionOpt;

        public ProgressTracker()
            : this(null)
        {
        }

        public ProgressTracker(Action<int, int> updateActionOpt)
        {
            this.updateActionOpt = updateActionOpt;
        }

        public int CompletedItems
        {
            get
            {
                return completedItems;
            }
        }

        public int TotalItems
        {
            get
            {
                return totalItems;
            }
        }

        public void AddItems(int count)
        {
            Interlocked.Add(ref totalItems, count);
            Update();
        }

        public void ItemCompleted()
        {
            Interlocked.Increment(ref completedItems);
            Update();
        }

        private void Update()
        {
            if (updateActionOpt != null)
            {
                updateActionOpt(completedItems, totalItems);
            }
        }
    }
}
