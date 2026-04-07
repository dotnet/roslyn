// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class NintValueSetFactory : IConstantValueSetFactory<int>, IConstantValueSetFactory
        {
            public static readonly NintValueSetFactory Instance = new NintValueSetFactory();

            private NintValueSetFactory() { }

            IConstantValueSet IConstantValueSetFactory.AllValues => NintValueSet.AllValues;

            IConstantValueSet IConstantValueSetFactory.NoValues => NintValueSet.NoValues;

            public IConstantValueSet<int> Related(BinaryOperatorKind relation, int value)
            {
                return new NintValueSet(
                    hasSmall: relation switch { LessThan => true, LessThanOrEqual => true, _ => false },
                    values: new NumericValueSetFactory<int>(IntTC.DefaultInstance).Related(relation, value),
                    hasLarge: relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false }
                    );
            }

            IConstantValueSet IConstantValueSetFactory.Random(int expectedSize, Random random)
            {
                return new NintValueSet(
                    hasSmall: random.NextDouble() < 0.25,
                    values: (IConstantValueSet<int>)new NumericValueSetFactory<int>(IntTC.DefaultInstance).Random(expectedSize, random),
                    hasLarge: random.NextDouble() < 0.25
                    );
            }

            ConstantValue IConstantValueSetFactory.RandomValue(Random random) => ConstantValue.CreateNativeInt(IntTC.DefaultInstance.Random(random));

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                return value.IsBad ? NintValueSet.AllValues : Related(relation, IntTC.DefaultInstance.FromConstantValue(value));
            }

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                var tc = IntTC.DefaultInstance;
                return tc.Related(relation, tc.FromConstantValue(left), tc.FromConstantValue(right));
            }
        }
    }
}
