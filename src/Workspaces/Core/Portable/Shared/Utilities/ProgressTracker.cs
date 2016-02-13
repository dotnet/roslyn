// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private int _completedItems;
        private int _totalItems;

        private readonly Action<int, int> _updateActionOpt;

        public ProgressTracker()
            : this(null)
        {
        }

        public ProgressTracker(Action<int, int> updateActionOpt)
        {
            _updateActionOpt = updateActionOpt;
        }

        public int CompletedItems
        {
            get
            {
                return _completedItems;
            }
        }

        public int TotalItems
        {
            get
            {
                return _totalItems;
            }
        }

        public void AddItems(int count)
        {
            Interlocked.Add(ref _totalItems, count);
            Update();
        }

        public void ItemCompleted()
        {
            Interlocked.Increment(ref _completedItems);
            Update();
        }

        private void Update()
        {
            _updateActionOpt?.Invoke(_completedItems, _totalItems);
        }
    }
}
