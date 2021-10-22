// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class DecimalValueSetFactory : IValueSetFactory<decimal>, IValueSetFactory
        {
            public static readonly DecimalValueSetFactory Instance = new DecimalValueSetFactory();

            private readonly IValueSetFactory<decimal> _underlying = NumericValueSetFactory<decimal, DecimalTC>.Instance;

            IValueSet IValueSetFactory.AllValues => NumericValueSet<decimal, DecimalTC>.AllValues;

            IValueSet IValueSetFactory.NoValues => NumericValueSet<decimal, DecimalTC>.NoValues;

            public IValueSet<decimal> Related(BinaryOperatorKind relation, decimal value) => _underlying.Related(relation, DecimalTC.Normalize(value));

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) => _underlying.Random(expectedSize, random);

            ConstantValue IValueSetFactory.RandomValue(Random random) => ConstantValue.Create(default(DecimalTC).Random(random));

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? NumericValueSet<decimal, DecimalTC>.AllValues : Related(relation, default(DecimalTC).FromConstantValue(value));

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right) => _underlying.Related(relation, left, right);
        }
    }
}
