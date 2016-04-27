using System;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface IProgressTracker
    {
        int CompletedItems { get; }
        int TotalItems { get; }

        void AddItems(int count);
        void ItemCompleted();
        void Clear();
    }

    internal class SimpleProgressTracker : IProgressTracker
    {
        private readonly object gate = new object();
        private int _completedItems;
        private int _totalItems;

        public SimpleProgressTracker()
        {
        }

        public int CompletedItems
        {
            get
            {
                lock (gate)
                {
                    return _completedItems;
                }
            }
        }

        public int TotalItems
        {
            get
            {
                lock (gate)
                {
                    return _totalItems;
                }
            }
        }

        public void AddItems(int count)
        {
            lock (gate)
            {
                _totalItems += count;
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                _completedItems = 0;
                _totalItems = 0;
            }
        }

        public void ItemCompleted()
        {
            lock (gate)
            {
                _completedItems++;
            }
        }
    }
}