// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A set of ints that is small, thread-safe and lock free.
    /// Several assumptions have been made that allow it to be small and fast:
    /// 1. Deletes never happen.
    /// 2. The size is small. In dogfooding experiements, 89% had 4 or fewer elements and
    ///    98% had 8 or fewer elements. The largest size was 17.
    /// 3. As a result of assumption 2, linear look-up is good enough.
    /// 4. One value, in this case int.MinValue, is used as a sentinel and may never appear in the set.
    /// </summary>
    internal class SmallConcurrentSetOfInts
    {
        // The set is a singly-linked list of nodes each containing up to 4 values.
        // Empty slots contain the "unoccupied" sentinel value.
        private int v1;
        private int v2;
        private int v3;
        private int v4;
        private SmallConcurrentSetOfInts next;

        private const int unoccupied = int.MinValue;

        public SmallConcurrentSetOfInts()
        {
            v1 = v2 = v3 = v4 = unoccupied;
        }

        private SmallConcurrentSetOfInts(int initialValue)
        {
            v1 = initialValue;
            v2 = v3 = v4 = unoccupied;
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
            do
            {
                // PERF: Not testing for unoccupied slots since it adds complexity. The extra comparisons
                // would slow down this inner loop such that any benefit of an 'early out' would be lost.
                if (set.v1 == i || set.v2 == i || set.v3 == i || set.v4 == i)
                {
                    return true;
                }

                set = set.next;
            }
            while (set != null);

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
                if (AddHelper(ref set.v1, i, ref added) ||
                    AddHelper(ref set.v2, i, ref added) ||
                    AddHelper(ref set.v3, i, ref added) ||
                    AddHelper(ref set.v4, i, ref added))
                {
                    return added;
                }

                var nextSet = set.next;
                if (nextSet == null)
                {
                    // Need to add a new 'block'.
                    SmallConcurrentSetOfInts tail = new SmallConcurrentSetOfInts(initialValue: i);

                    nextSet = Interlocked.CompareExchange(ref set.next, tail, null);
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
