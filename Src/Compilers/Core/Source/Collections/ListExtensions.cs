using System;
using System.Collections.Generic;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Defines extension methods for the <see cref="IList&lt;T&gt;"/> interface.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Searches a list for a given element using a binary search algorithm. Elements of the
        /// array are compared to the search value using provided comparer on the elements of the
        /// list and the given search value. This method assumes that the list is already sorted
        /// according to the comparer function; if this is not the case, the result will be 
        /// incorrect.
        /// </summary>
        ///
        /// <returns>
        /// The method returns the index of the given value in the list. If the list does not contain
        /// the given value, the method returns a negative integer. The bitwise complement operator
        /// (~) can be applied to a negative result to produce the index of the first element (if
        /// any) that is larger than the given search value.
        /// </returns>
        public static int BinarySearch<T1, T2>(this IList<T1> list, T2 value, Func<T2, T1, int> compare)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            if (compare == null)
            {
                throw new ArgumentNullException("compare");
            }

            var low = 0;
            var high = list.Count - 1;
            while (low <= high)
            {
                // Compute the middle, without overflowing for large values of
                // "high" and "low".
                var middle = low + ((high - low) >> 1);

                var compValue = compare(value, list[middle]);

                if (compValue == 0)
                {
                    return middle;
                }
                else if (compValue < 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }
    }
}