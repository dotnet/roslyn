// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class FloatingValueSetFactory<TFloating, TFloatingTC> : IValueSetFactory<TFloating> where TFloatingTC : class, FloatingTC<TFloating>
        {
            public readonly TFloatingTC _tc;
            public readonly NumericValueSetFactory<TFloating, TFloatingTC> _numericValueSetFactory;

            public FloatingValueSetFactory(TFloatingTC tc)
            {
                _tc = tc;
                _numericValueSetFactory = new NumericValueSetFactory<TFloating, TFloatingTC>(tc);
            }

            IValueSet IValueSetFactory.AllValues => FloatingValueSet<TFloating, TFloatingTC>.AllValues(_tc);

            IValueSet IValueSetFactory.NoValues => FloatingValueSet<TFloating, TFloatingTC>.NoValues(_tc);

            public IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value) =>
                FloatingValueSet<TFloating, TFloatingTC>.Related(relation, value, _tc, _numericValueSetFactory);

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) =>
                FloatingValueSet<TFloating, TFloatingTC>.Random(expectedSize, random, _tc, _numericValueSetFactory);

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.Random(random));
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad
                    ? FloatingValueSet<TFloating, TFloatingTC>.AllValues(_tc)
                    : FloatingValueSet<TFloating, TFloatingTC>.Related(relation, _tc.FromConstantValue(value), _tc, _numericValueSetFactory);

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                return _tc.Related(relation, _tc.FromConstantValue(left), _tc.FromConstantValue(right));
            }
        }
    }
}
