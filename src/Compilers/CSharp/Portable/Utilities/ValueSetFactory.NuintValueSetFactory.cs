// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class NuintValueSetFactory : IConstantValueSetFactory<uint>, IConstantValueSetFactory
        {
            public static readonly NuintValueSetFactory Instance = new NuintValueSetFactory();

            private NuintValueSetFactory() { }

            IConstantValueSet IConstantValueSetFactory.AllValues => NuintValueSet.AllValues;

            IConstantValueSet IConstantValueSetFactory.NoValues => NuintValueSet.NoValues;

            public IConstantValueSet<uint> Related(BinaryOperatorKind relation, uint value)
            {
                return new NuintValueSet(
                    values: new NumericValueSetFactory<uint>(UIntTC.Instance).Related(relation, value),
                    hasLarge: relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false }
                    );
            }

            IConstantValueSet IConstantValueSetFactory.Random(int expectedSize, Random random)
            {
                return new NuintValueSet(
                    values: (IConstantValueSet<uint>)new NumericValueSetFactory<uint>(UIntTC.Instance).Random(expectedSize, random),
                    hasLarge: random.NextDouble() < 0.25
                    );
            }

            ConstantValue IConstantValueSetFactory.RandomValue(Random random) => ConstantValue.CreateNativeUInt(UIntTC.Instance.Random(random));

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value)
            {
                return value.IsBad ? NuintValueSet.AllValues : Related(relation, UIntTC.Instance.FromConstantValue(value));
            }

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                var tc = UIntTC.Instance;
                return tc.Related(relation, tc.FromConstantValue(left), tc.FromConstantValue(right));
            }
        }
    }
}
