// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class FloatingValueSetFactory<TFloating> : IValueSetFactory<TFloating>
        {
            private readonly FloatingTC<TFloating> _tc;

            public FloatingValueSetFactory(FloatingTC<TFloating> tc)
            {
                _tc = tc;
            }

            IValueSet IValueSetFactory.AllValues => FloatingValueSet<TFloating>.AllValues(_tc);

            IValueSet IValueSetFactory.NoValues => FloatingValueSet<TFloating>.NoValues(_tc);

            public IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value) =>
                FloatingValueSet<TFloating>.Related(relation, value, _tc);

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) =>
                FloatingValueSet<TFloating>.Random(expectedSize, random, _tc);

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.Random(random));
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad
                    ? FloatingValueSet<TFloating>.AllValues(_tc)
                    : FloatingValueSet<TFloating>.Related(relation, _tc.FromConstantValue(value), _tc);

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                return _tc.Related(relation, _tc.FromConstantValue(left), _tc.FromConstantValue(right));
            }
        }
    }
}
