// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A set of ints that is small, thread-safe and lock free.
    /// Several assumptions have been made that allow it to be small and fast:
    /// 1. Deletes never happen.
    /// 2. The size is small. In dogfooding experiments, 89% had 4 or fewer elements and
    ///    98% had 8 or fewer elements. The largest size was 17.
    /// 3. As a result of assumption 2, linear look-up is good enough.
    /// 4. One value, in this case int.MinValue, is used as a sentinel and may never appear in the set.
    /// </summary>
    internal class SmallConcurrentSetOfInts
    {
        // The set is a singly-linked list of nodes each containing up to 4 values.
        // Empty slots contain the "unoccupied" sentinel value.
        private int _v1;
        private int _v2;
        private int _v3;
        private int _v4;
        private SmallConcurrentSetOfInts? _next;

        private const int unoccupied = int.MinValue;

        public SmallConcurrentSetOfInts()
        {
            _v1 = _v2 = _v3 = _v4 = unoccupied;
        }

        private SmallConcurrentSetOfInts(int initialValue)
        {
            _v1 = initialValue;
            _v2 = _v3 = _v4 = unoccupied;
        }

        /// <summary>
        /// Determine if the given integer appears in the set.
        /// </summary>
        /// <param name="i">The value to look up.</param>
        /// <returns>true if <paramref name="i"/> appears in the set. false otherwise.</returns>
        public bool Contains(int i)
        {
            Debug.Assert(i != unoccupied);
            return SmallConcurrentSetOfInts.Contains(this, i);
        }

        private static bool Contains(SmallConcurrentSetOfInts set, int i)
        {
            SmallConcurrentSetOfInts? current = set;
            do
            {
                // PERF: Not testing for unoccupied slots since it adds complexity. The extra comparisons
                // would slow down this inner loop such that any benefit of an 'early out' would be lost.
                if (current._v1 == i || current._v2 == i || current._v3 == i || current._v4 == i)
                {
                    return true;
                }

                current = current._next;
            }
            while (current != null);

            return false;
        }

        /// <summary>
        /// Insert the given value into the set.
        /// </summary>
        /// <param name="i">The value to insert</param>
        /// <returns>true if <paramref name="i"/> was added. false if it was already present.</returns>
        public bool Add(int i)
        {
            Debug.Assert(i != unoccupied);
            return SmallConcurrentSetOfInts.Add(this, i);
        }

        private static bool Add(SmallConcurrentSetOfInts set, int i)
        {
            bool added = false;

            while (true)
            {
                if (AddHelper(ref set._v1, i, ref added) ||
                    AddHelper(ref set._v2, i, ref added) ||
                    AddHelper(ref set._v3, i, ref added) ||
                    AddHelper(ref set._v4, i, ref added))
                {
                    return added;
                }

                var nextSet = set._next;
                if (nextSet == null)
                {
                    // Need to add a new 'block'.
                    SmallConcurrentSetOfInts tail = new SmallConcurrentSetOfInts(initialValue: i);

                    nextSet = Interlocked.CompareExchange(ref set._next, tail, null);
                    if (nextSet == null)
                    {
                        // Successfully added a new tail
                        return true;
                    }
                    // Lost the race. Another thread added a new tail so resume searching from there.
                }

                set = nextSet;
            }
        }

        /// <summary>
        /// If the given slot is unoccupied, then try to replace it with a new value.
        /// </summary>
        /// <param name="slot">The slot to examine.</param>
        /// <param name="i">The new value to insert if the slot is unoccupied.</param>
        /// <param name="added">An out param indicating whether the slot was successfully updated.</param>
        /// <returns>true if the value in the slot either now contains, or already contained <paramref name="i"/>. false otherwise.</returns>
        private static bool AddHelper(ref int slot, int i, ref bool added)
        {
            Debug.Assert(!added);

            int val = slot;

            if (val == unoccupied)
            {
                val = Interlocked.CompareExchange(ref slot, i, unoccupied);

                if (val == unoccupied)
                {
                    // Successfully replaced the value
                    added = true;
                    return true;
                }
                // Lost the race with another thread
            }

            return (val == i);
        }
    }
}
