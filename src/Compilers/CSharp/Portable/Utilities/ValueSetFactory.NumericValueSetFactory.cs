// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// The implementation of a value set factory of any numeric type <typeparamref name="T"/>,
        /// parameterized by a type class
        /// <see cref="NumericTC{T}"/> that provides the primitives for that type.
        /// </summary>
        private sealed class NumericValueSetFactory<T, TTC> : IValueSetFactory<T> where TTC : struct, NumericTC<T>
        {
            public static readonly NumericValueSetFactory<T, TTC> Instance = new NumericValueSetFactory<T, TTC>();

            private static readonly IValueSet<T> _all = new NumericValueSet<T, TTC>(Interval.Included.Instance);

            private NumericValueSetFactory() { }

            public IValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                return new NumericValueSet<T, TTC>(RelatedInterval(relation, value, tc.MinValue, tc.MaxValue));
            }

            /// <summary>
            /// Produce the interval underlying the representation of the result of <see cref="NumericValueSetFactory{T, TTC}"/>.
            /// </summary>
            /// <param name="minValue">the interval's minimum value, inclusive</param>
            /// <param name="maxValue">the interval's maximum value, inclusive</param>
            private static Interval RelatedInterval(BinaryOperatorKind relation, T value, T minValue, T maxValue)
            {
                TTC tc = default;
                Debug.Assert(tc.Related(LessThanOrEqual, minValue, maxValue));
                switch (relation)
                {
                    case Equal when tc.Related(LessThan, value, minValue):
                        return Interval.Excluded.Instance;
                    case Equal when tc.Related(GreaterThan, value, maxValue):
                        return Interval.Excluded.Instance;
                    case GreaterThan when tc.Related(LessThan, value, minValue):
                        return Interval.Included.Instance;
                    case GreaterThan when tc.Related(GreaterThanOrEqual, value, maxValue):
                        return Interval.Excluded.Instance;
                    case LessThan when tc.Related(LessThanOrEqual, value, minValue):
                        return Interval.Excluded.Instance;
                    case LessThan when tc.Related(GreaterThan, value, maxValue):
                        return Interval.Included.Instance;
                    case LessThanOrEqual when tc.Related(LessThan, value, minValue):
                        return Interval.Excluded.Instance;
                    case LessThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue):
                        return Interval.Included.Instance;
                    case GreaterThanOrEqual when tc.Related(LessThanOrEqual, value, minValue):
                        return Interval.Included.Instance;
                    case GreaterThanOrEqual when tc.Related(GreaterThan, value, maxValue):
                        return Interval.Excluded.Instance;
                    default:
                        if (tc.Related(Equal, minValue, maxValue))
                            return tc.Related(relation, minValue, value) ? Interval.Included.Instance : Interval.Excluded.Instance;
                        var (leftMax, rightMin) = tc.Partition(minValue, maxValue);
                        return Interval.Mixed.Create(RelatedInterval(relation, value, minValue, leftMax), RelatedInterval(relation, value, rightMin, maxValue));
                };
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? _all : Related(relation, default(TTC).FromConstantValue(value));

            public IValueSet<T> Random(int expectedSize, Random random) =>
                NumericValueSet<T, TTC>.Random(expectedSize, random);
        }
    }
}
