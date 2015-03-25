using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal class MostRecentlyUsedList<T>
    {
        private readonly object gate = new object();
        private readonly List<T> items = new List<T>();
        private readonly int maxSize;

        public MostRecentlyUsedList(int maxSize = 10)
        {
            this.maxSize = maxSize;
        }

        public void Add(T value)
        {
            lock (gate)
            {
                var removed = items.Remove(value);
                if (!removed && items.Count == maxSize)
                {
                    items.RemoveAt(0);
                }

                items.Add(value);
            }
        }

        public int IndexOf(T value)
        {
            lock (gate)
            {
                return items.IndexOf(value);
            }
        }

        public bool Contains(T value)
        {
            lock (gate)
            {
                return items.Contains(value);
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                items.Clear();
            }
        }
    }
}
