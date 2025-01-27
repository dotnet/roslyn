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
        private struct NumericValueSetFactory<T> : IValueSetFactory<T>
        {
            private readonly INumericTC<T> _tc;

            IValueSet IValueSetFactory.AllValues => NumericValueSet<T>.AllValues(_tc);

            IValueSet IValueSetFactory.NoValues => NumericValueSet<T>.NoValues(_tc);

            public NumericValueSetFactory(INumericTC<T> tc) { this._tc = tc; }

            public IValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case LessThan:
                        if (_tc.Related(LessThanOrEqual, value, _tc.MinValue))
                            return NumericValueSet<T>.NoValues(_tc);
                        return new NumericValueSet<T>(_tc.MinValue, _tc.Prev(value), _tc);
                    case LessThanOrEqual:
                        return new NumericValueSet<T>(_tc.MinValue, value, _tc);
                    case GreaterThan:
                        if (_tc.Related(GreaterThanOrEqual, value, _tc.MaxValue))
                            return NumericValueSet<T>.NoValues(_tc);
                        return new NumericValueSet<T>(_tc.Next(value), _tc.MaxValue, _tc);
                    case GreaterThanOrEqual:
                        return new NumericValueSet<T>(value, _tc.MaxValue, _tc);
                    case Equal:
                        return new NumericValueSet<T>(value, value, _tc);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? NumericValueSet<T>.AllValues(_tc) : Related(relation, _tc.FromConstantValue(value));

            public IValueSet Random(int expectedSize, Random random) =>
                NumericValueSet<T>.Random(expectedSize, random, _tc);

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.Random(random));
            }

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                return _tc.Related(relation, _tc.FromConstantValue(left), _tc.FromConstantValue(right));
            }
        }
    }
}
