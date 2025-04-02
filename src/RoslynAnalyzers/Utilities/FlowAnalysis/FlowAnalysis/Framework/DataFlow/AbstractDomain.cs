// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract domain for a <see cref="DataFlowAnalysis"/> to merge and compare values.
    /// </summary>
    public abstract class AbstractDomain<T>
    {
        /// <summary>
        /// Returns the minor value of the domain.
        /// </summary>
        public abstract T Bottom { get; }

        /// <summary>
        /// Returns a value that is greater than <paramref name="value1"/> and <paramref name="value2"/>.
        /// </summary>
        /// <param name="value1">A value to be merged</param>
        /// <param name="value2">A value to be merged</param>
        /// <returns>A value that is greater than <paramref name="value1"/> and <paramref name="value2"/></returns>
        public abstract T Merge(T value1, T value2);

        /// <summary>
        /// Compares <paramref name="oldValue"/> with <paramref name="newValue"/>
        /// and returns a value indicating whether one value is less than,
        /// equal to, or greater than the other.
        /// </summary>
        /// <param name="oldValue">A value to compare</param>
        /// <param name="newValue">A value to compare</param>
        /// <returns>A signed integer that indicates the relative values of
        /// <paramref name="oldValue"/> and <paramref name="newValue"/>.
        /// <para>Less than zero: <paramref name="oldValue"/> is less than <paramref name="newValue"/>.</para>
        /// <para>Zero: <paramref name="oldValue"/> equals <paramref name="newValue"/>.</para>
        /// <para>Greater than zero: <paramref name="oldValue"/> is greater than <paramref name="newValue"/>.</para>
        ///</returns>
        public int Compare(T oldValue, T newValue)
            => Compare(oldValue, newValue, assertMonotonicity: true);

        /// <summary>
        /// Indicates if <paramref name="value1"/> and <paramref name="value2"/> are equal.
        /// </summary>
        /// <param name="value1">A value to compare</param>
        /// <param name="value2">A value to compare</param>
        public bool Equals(T value1, T value2)
            => Compare(value1, value2, assertMonotonicity: false) == 0;

        public abstract int Compare(T oldValue, T newValue, bool assertMonotonicity);

#pragma warning disable CA1030 // Use events where appropriate
        [Conditional("DEBUG")]
        protected static void FireNonMonotonicAssertIfNeeded(bool assertMonotonicity)
        {
            if (assertMonotonicity)
            {
                Debug.Fail("Non-monotonic merge");
            }
        }
#pragma warning restore CA1030 // Use events where appropriate
    }
}
