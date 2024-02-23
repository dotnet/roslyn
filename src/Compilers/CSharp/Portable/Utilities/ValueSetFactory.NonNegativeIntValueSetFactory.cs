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
        private sealed class NonNegativeIntValueSetFactory : IValueSetFactory<int>
        {
            public static readonly NonNegativeIntValueSetFactory Instance = new NonNegativeIntValueSetFactory();
            private static readonly IValueSetFactory<int> s_underlying = new NumericValueSetFactory<int>(IntTC.NonNegativeInstance);

            private NonNegativeIntValueSetFactory() { }

            public IValueSet AllValues => NumericValueSet<int>.AllValues(IntTC.NonNegativeInstance);

            public IValueSet NoValues => NumericValueSet<int>.NoValues(IntTC.NonNegativeInstance);

            public IValueSet<int> Related(BinaryOperatorKind relation, int value)
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

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) => s_underlying.Random(expectedSize, random);

            ConstantValue IValueSetFactory.RandomValue(Random random) => s_underlying.RandomValue(random);

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? AllValues : Related(relation, IntTC.NonNegativeInstance.FromConstantValue(value));

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right) => s_underlying.Related(relation, left, right);
        }
    }
}
