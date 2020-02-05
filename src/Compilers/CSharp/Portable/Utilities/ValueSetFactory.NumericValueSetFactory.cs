// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
        private class NumericValueSetFactory<T, TTC> : IValueSetFactory<T> where TTC : struct, NumericTC<T>
        {
            private NumericValueSetFactory() { }
            public static readonly NumericValueSetFactory<T, TTC> Instance = new NumericValueSetFactory<T, TTC>();
            private static readonly IValueSet<T> _all = new NumericValueSet<T, TTC>(Interval.Included.Instance);
            public IValueSet<T> All => _all;
            IValueSet IValueSetFactory.All => _all;
            private static readonly IValueSet<T> _none = new NumericValueSet<T, TTC>(Interval.Excluded.Instance);
            public IValueSet<T> None => _none;
            IValueSet IValueSetFactory.None => _none;

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
            private Interval RelatedInterval(BinaryOperatorKind relation, T value, T minValue, T maxValue)
            {
                TTC tc = default;
                return relation switch
                {
                    Equal when tc.Related(LessThan, value, minValue) => Interval.Excluded.Instance,
                    Equal when tc.Related(GreaterThan, value, maxValue) => Interval.Excluded.Instance,
                    GreaterThan when tc.Related(LessThan, value, minValue) => Interval.Included.Instance,
                    GreaterThan when tc.Related(GreaterThanOrEqual, value, maxValue) => Interval.Excluded.Instance,
                    LessThan when tc.Related(LessThanOrEqual, value, minValue) => Interval.Excluded.Instance,
                    LessThan when tc.Related(GreaterThan, value, maxValue) => Interval.Included.Instance,
                    LessThanOrEqual when tc.Related(LessThan, value, minValue) => Interval.Excluded.Instance,
                    LessThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue) => Interval.Included.Instance,
                    GreaterThanOrEqual when tc.Related(LessThanOrEqual, value, minValue) => Interval.Included.Instance,
                    GreaterThanOrEqual when tc.Related(GreaterThan, value, maxValue) => Interval.Excluded.Instance,
                    var _ when tc.Related(Equal, minValue, maxValue) =>
                        tc.Related(relation, minValue, value) ? Interval.Included.Instance : Interval.Excluded.Instance,
                    var _ when tc.Partition(minValue, maxValue) is var (leftMax, rightMin) =>
                        Interval.Mixed.Create(RelatedInterval(relation, value, minValue, leftMax), RelatedInterval(relation, value, rightMin, maxValue)),
                    var _ => throw new ArgumentException("relation"),
                };
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) => value.IsBad ? _all : Related(relation, default(TTC).FromConstantValue(value));
        }
    }
}
