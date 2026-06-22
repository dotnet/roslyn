// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class DecimalValueSetFactory : IConstantValueSetFactory<decimal>, IConstantValueSetFactory
        {
            public static readonly DecimalValueSetFactory Instance = new DecimalValueSetFactory();

            private readonly IConstantValueSetFactory<decimal> _underlying = new NumericValueSetFactory<decimal>(DecimalTC.Instance);

            IConstantValueSet IConstantValueSetFactory.AllValues => NumericValueSet<decimal>.AllValues(DecimalTC.Instance);

            IConstantValueSet IConstantValueSetFactory.NoValues => NumericValueSet<decimal>.NoValues(DecimalTC.Instance);

            public IConstantValueSet<decimal> Related(BinaryOperatorKind relation, decimal value) => _underlying.Related(relation, DecimalTC.Normalize(value));

            IConstantValueSet IConstantValueSetFactory.Random(int expectedSize, Random random) => _underlying.Random(expectedSize, random);

            ConstantValue IConstantValueSetFactory.RandomValue(Random random) => ConstantValue.Create(DecimalTC.Instance.Random(random));

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? NumericValueSet<decimal>.AllValues(DecimalTC.Instance) : Related(relation, DecimalTC.Instance.FromConstantValue(value));

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right) => _underlying.Related(relation, left, right);
        }
    }
}
