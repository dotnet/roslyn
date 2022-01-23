// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// The implementation of a value set factory of any numeric type <typeparamref name="T"/>,
        /// parameterized by a type class
        /// <see cref="INumericTC{T}"/> that provides the primitives for that type.
        /// </summary>
        private sealed class NumericValueSetFactory<T, TTC> : IValueSetFactory<T> where TTC : struct, INumericTC<T>
        {
            public static readonly NumericValueSetFactory<T, TTC> Instance = new NumericValueSetFactory<T, TTC>();

            IValueSet IValueSetFactory.AllValues => NumericValueSet<T, TTC>.AllValues;

            IValueSet IValueSetFactory.NoValues => NumericValueSet<T, TTC>.NoValues;

            private NumericValueSetFactory() { }

            public IValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                switch (relation)
                {
                    case LessThan:
                        if (tc.Related(LessThanOrEqual, value, tc.MinValue))
                            return NumericValueSet<T, TTC>.NoValues;
                        return new NumericValueSet<T, TTC>(tc.MinValue, tc.Prev(value));
                    case LessThanOrEqual:
                        return new NumericValueSet<T, TTC>(tc.MinValue, value);
                    case GreaterThan:
                        if (tc.Related(GreaterThanOrEqual, value, tc.MaxValue))
                            return NumericValueSet<T, TTC>.NoValues;
                        return new NumericValueSet<T, TTC>(tc.Next(value), tc.MaxValue);
                    case GreaterThanOrEqual:
                        return new NumericValueSet<T, TTC>(value, tc.MaxValue);
                    case Equal:
                        return new NumericValueSet<T, TTC>(value, value);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? NumericValueSet<T, TTC>.AllValues : Related(relation, default(TTC).FromConstantValue(value));

            public IValueSet Random(int expectedSize, Random random) =>
                NumericValueSet<T, TTC>.Random(expectedSize, random);

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                var tc = default(TTC);
                return tc.ToConstantValue(tc.Random(random));
            }

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                var tc = default(TTC);
                return tc.Related(relation, tc.FromConstantValue(left), tc.FromConstantValue(right));
            }
        }
    }
}
