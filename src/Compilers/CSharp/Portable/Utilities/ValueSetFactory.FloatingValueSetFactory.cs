// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class FloatingValueSetFactory<TFloating> : IConstantValueSetFactory<TFloating>
        {
            private readonly FloatingTC<TFloating> _tc;

            public FloatingValueSetFactory(FloatingTC<TFloating> tc)
            {
                _tc = tc;
            }

            IConstantValueSet IConstantValueSetFactory.AllValues => FloatingValueSet<TFloating>.AllValues(_tc);

            IConstantValueSet IConstantValueSetFactory.NoValues => FloatingValueSet<TFloating>.NoValues(_tc);

            public IConstantValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value) =>
                FloatingValueSet<TFloating>.Related(relation, value, _tc);

            IConstantValueSet IConstantValueSetFactory.Random(int expectedSize, Random random) =>
                FloatingValueSet<TFloating>.Random(expectedSize, random, _tc);

            ConstantValue IConstantValueSetFactory.RandomValue(Random random)
            {
                return _tc.ToConstantValue(_tc.Random(random));
            }

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad
                    ? FloatingValueSet<TFloating>.AllValues(_tc)
                    : FloatingValueSet<TFloating>.Related(relation, _tc.FromConstantValue(value), _tc);

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                return _tc.Related(relation, _tc.FromConstantValue(left), _tc.FromConstantValue(right));
            }
        }
    }
}
