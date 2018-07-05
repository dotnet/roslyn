// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract domain for a <see cref="DataFlowAnalysis"/> to merge and compare values.
    /// </summary>
    internal abstract class AbstractDomain<T>
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
        /// Returns a value that is greater than all <paramref name="values"/>
        /// </summary>
        /// <param name="values"> The values to be merged</param>
        /// <returns>A value that is greater than all <paramref name="values"/></returns>
        public virtual T Merge(IEnumerable<T> values)
        {
            var valuesArray = values.Where(v => v != null).ToArray();
            switch (valuesArray.Length)
            {
                case 0:
                    return Bottom;
                case 1:
                    return valuesArray[0];
                default:
                    return valuesArray.Aggregate(Merge);
            }
        }

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
        public abstract int Compare(T oldValue, T newValue);
    }
}
