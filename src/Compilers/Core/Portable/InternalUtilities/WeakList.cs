// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Represents an ordered sequence of weak references.
    /// </summary>
    internal sealed class WeakList<T> : IEnumerable<T>
        where T : class
    {
        private WeakReference<T>[] items;
        private int size;

        public WeakList()
        {
            items = SpecializedCollections.EmptyArray<WeakReference<T>>();
        }

        private void Resize()
        {
            Debug.Assert(size == items.Length);
            Debug.Assert(items.Length == 0 || items.Length >= MinimalNonEmptySize);

            int alive = items.Length;
            int firstDead = -1;
            for (int i = 0; i < items.Length; i++)
            {
                T target;
                if (!items[i].TryGetTarget(out target))
                {
                    if (firstDead == -1)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive < items.Length / 4)
            {
                // If we have just a few items left we shrink the array.
                // We avoid expanding the array until the number of new items added exceeds half of its capacity.
                Shrink(firstDead, alive);
            }
            else if (alive >= 3 * items.Length / 4)
            {
                // If we have a lot of items alive we expand the array since just compacting them 
                // wouldn't free up much space (we would end up calling Resize again after adding a few more items).
                var newItems = new WeakReference<T>[GetExpandedSize(items.Length)];

                if (firstDead >= 0)
                {
                    Compact(firstDead, newItems);
                }
                else
                {
                    Array.Copy(items, 0, newItems, 0, items.Length);
                    Debug.Assert(size == items.Length);
                }

                items = newItems;
            }
            else
            {
                // Compact in-place to make space for new items at the end.
                // We will free up to length/4 slots in the array.
                Compact(firstDead, items);
            }

            Debug.Assert(items.Length > 0 && size < 3 * items.Length / 4, "length: " + items.Length + " size: " + size);
        }

        private void Shrink(int firstDead, int alive)
        {
            int newSize = GetExpandedSize(alive);
            var newItems = (newSize == items.Length) ? items : new WeakReference<T>[newSize];
            Compact(firstDead, newItems);
            items = newItems;
        }

        private const int MinimalNonEmptySize = 4;

        private static int GetExpandedSize(int baseSize)
        {
            return Math.Max((baseSize * 2) + 1, MinimalNonEmptySize);
        }

        /// <summary>
        /// Copies all live references from <see cref="items"/> to <paramref name="result"/>.
        /// Assumes that all references prior <paramref name="firstDead"/> are alive.
        /// </summary>
        private void Compact(int firstDead, WeakReference<T>[] result)
        {
            Debug.Assert(items[firstDead].IsNull());

            if (!ReferenceEquals(items, result))
            {
                Array.Copy(items, 0, result, 0, firstDead);
            }

            int oldSize = size;
            int j = firstDead;
            for (int i = firstDead + 1; i < oldSize; i++)
            {
                var item = items[i];

                T target;
                if (item.TryGetTarget(out target))
                {
                    result[j++] = item;
                }
            }

            size = j;

            // free WeakReferences
            if (ReferenceEquals(items, result))
            {
                while (j < oldSize)
                {
                    items[j++] = null;
                }
            }
        }

        /// <summary>
        /// Returns the number of weak references in this list. 
        /// Note that some of them might not point to live objects anymore.
        /// </summary>
        public int WeakCount
        {
            get { return size; }
        }

        public WeakReference<T> GetWeakReference(int index)
        {
            if (index < 0 || index >= size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return items[index];
        }

        public void Add(T item)
        {
            if (size == items.Length)
            {
                Resize();
            }

            Debug.Assert(size < items.Length);
            items[size++] = new WeakReference<T>(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = size;
            int alive = size;
            int firstDead = -1;

            for (int i = 0; i < count; i++)
            {
                T item;
                if (items[i].TryGetTarget(out item))
                {
                    yield return item;
                }
                else
                {
                    // object has been collected 

                    if (firstDead < 0)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive == 0)
            {
                items = SpecializedCollections.EmptyArray<WeakReference<T>>();
                size = 0;
            }
            else if (alive < items.Length / 4)
            {
                // If we have just a few items left we shrink the array.
                Shrink(firstDead, alive);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal WeakReference<T>[] TestOnly_UnderlyingArray { get { return items; } }
    }
}
