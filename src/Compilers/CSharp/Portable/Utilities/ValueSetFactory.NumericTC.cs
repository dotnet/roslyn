// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class providing the primitive operations needed to support a value set.
        /// </summary>
        /// <typeparam name="T">the underlying primitive numeric type</typeparam>
        private interface NumericTC<T> : EqualableValueTC<T>
        {
            /// <summary>
            /// Compute the value of the binary relational operator on the given operands.
            /// </summary>
            bool Related(BinaryOperatorKind relation, T left, T right);

            /// <summary>
            /// The smallest value of <typeparamref name="T"/>.
            /// </summary>
            T MinValue { get; }

            /// <summary>
            /// The largest value of <typeparamref name="T"/>.
            /// </summary>
            T MaxValue { get; }

            /// <summary>
            /// Returns the midpoints when subdividing an interval, which becomes the
            /// left subinterval's max value and the right subinterval's min value.
            /// The returned rightMin must be the successor value (see <see cref="Next(T)"/>) to leftMax.
            /// </summary>
            /// <param name="min">the parent interval's minimum value, inclusive.</param>
            /// <param name="max">the parent interval's maximum value, inclusive</param>
            (T leftMax, T rightMin) Partition(T min, T max);

            /// <summary>
            /// The successor (next larger) value to a given value. Used to determine when two intervals
            /// are contiguous to improve the output of <see cref="object.ToString"/>. The result is not defined
            /// when <paramref name="value"/> is <see cref="MaxValue"/>.
            /// </summary>
            T Next(T value);
        }
    }
}
