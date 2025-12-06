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
        private sealed class NonNegativeIntValueSetFactory : IConstantValueSetFactory<int>
        {
            public static readonly NonNegativeIntValueSetFactory Instance = new NonNegativeIntValueSetFactory();
            private static readonly IConstantValueSetFactory<int> s_underlying = new NumericValueSetFactory<int>(IntTC.NonNegativeInstance);

            private NonNegativeIntValueSetFactory() { }

            public IConstantValueSet AllValues => NumericValueSet<int>.AllValues(IntTC.NonNegativeInstance);

            public IConstantValueSet NoValues => NumericValueSet<int>.NoValues(IntTC.NonNegativeInstance);

            public IConstantValueSet<int> Related(BinaryOperatorKind relation, int value)
            {
                var tc = IntTC.NonNegativeInstance;
                switch (relation)
                {
                    case LessThan:
                        if (value <= 0)
                            return NumericValueSet<int>.NoValues(tc);
                        return new NumericValueSet<int>(0, value - 1, tc);
                    case LessThanOrEqual:
                        if (value < 0)
                            return NumericValueSet<int>.NoValues(tc);
                        return new NumericValueSet<int>(0, value, tc);
                    case GreaterThan:
                        if (value == int.MaxValue)
                            return NumericValueSet<int>.NoValues(tc);
                        return new NumericValueSet<int>(Math.Max(0, value + 1), int.MaxValue, tc);
                    case GreaterThanOrEqual:
                        return new NumericValueSet<int>(Math.Max(0, value), int.MaxValue, tc);
                    case Equal:
                        if (value < 0)
                            return NumericValueSet<int>.NoValues(tc);
                        return new NumericValueSet<int>(value, value, tc);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }
            }

            IConstantValueSet IConstantValueSetFactory.Random(int expectedSize, Random random) => s_underlying.Random(expectedSize, random);

            ConstantValue IConstantValueSetFactory.RandomValue(Random random) => s_underlying.RandomValue(random);

            IConstantValueSet IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? AllValues : Related(relation, IntTC.NonNegativeInstance.FromConstantValue(value));

            bool IConstantValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right) => s_underlying.Related(relation, left, right);
        }
    }
}
