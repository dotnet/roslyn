// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class NintValueSetFactory : IValueSetFactory<int>, IValueSetFactory
        {
            public static readonly NintValueSetFactory Instance = new NintValueSetFactory();

            private NintValueSetFactory() { }

            IValueSet IValueSetFactory.AllValues => NintValueSet.AllValues;

            IValueSet IValueSetFactory.NoValues => NintValueSet.NoValues;

            public IValueSet<int> Related(BinaryOperatorKind relation, int value)
            {
                return new NintValueSet(
                    hasSmall: relation switch { LessThan => true, LessThanOrEqual => true, _ => false },
                    values: NumericValueSetFactory<int, IntTC>.Instance.Related(relation, value),
                    hasLarge: relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false }
                    );
            }

            IValueSet IValueSetFactory.Random(int expectedSize, Random random)
            {
                return new NintValueSet(
                    hasSmall: random.NextDouble() < 0.25,
                    values: (IValueSet<int>)NumericValueSetFactory<int, IntTC>.Instance.Random(expectedSize, random),
                    hasLarge: random.NextDouble() < 0.25
                    );
            }

            ConstantValue IValueSetFactory.RandomValue(Random random) => ConstantValue.CreateNativeInt(default(IntTC).Random(random));

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                return value.IsBad ? NintValueSet.AllValues : Related(relation, default(IntTC).FromConstantValue(value));
            }

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                var tc = default(IntTC);
                return tc.Related(relation, tc.FromConstantValue(left), tc.FromConstantValue(right));
            }
        }
    }
}
