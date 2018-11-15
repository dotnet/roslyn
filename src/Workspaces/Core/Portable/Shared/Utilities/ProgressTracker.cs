// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Utility class that can be used to track the progress of an operation in a threadsafe manner.
    /// </summary>
    internal class ProgressTracker : IProgressTracker
    {
        private string _description;
        private int _completedItems;
        private int _totalItems;

        private readonly Action<string, int, int> _updateActionOpt;

        public ProgressTracker()
            : this(null)
        {
        }

        public ProgressTracker(Action<string, int, int> updateActionOpt)
        {
            _updateActionOpt = updateActionOpt;
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                Update();
            }
        }

        public int CompletedItems => _completedItems;

        public int TotalItems => _totalItems;

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

        public void Clear()
        {
            _totalItems = 0;
            _completedItems = 0;
            _description = null;
            Update();
        }

        private void Update()
        {
            _updateActionOpt?.Invoke(_description, _completedItems, _totalItems);
        }
    }
}
