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

            private NonNegativeIntValueSetFactory() { }

            private readonly IValueSetFactory<int> _underlying = NumericValueSetFactory<int, NonNegativeIntTC>.Instance;

            public IValueSet AllValues => NumericValueSet<int, NonNegativeIntTC>.AllValues;

            public IValueSet NoValues => NumericValueSet<int, NonNegativeIntTC>.NoValues;

            public IValueSet<int> Related(BinaryOperatorKind relation, int value)
            {
                switch (relation)
                {
                    case LessThan:
                        if (value <= 0)
                            return NumericValueSet<int, NonNegativeIntTC>.NoValues;
                        return new NumericValueSet<int, NonNegativeIntTC>(0, value - 1);
                    case LessThanOrEqual:
                        if (value < 0)
                            return NumericValueSet<int, NonNegativeIntTC>.NoValues;
                        return new NumericValueSet<int, NonNegativeIntTC>(0, value);
                    case GreaterThan:
                        if (value == int.MaxValue)
                            return NumericValueSet<int, NonNegativeIntTC>.NoValues;
                        return new NumericValueSet<int, NonNegativeIntTC>(Math.Max(0, value + 1), int.MaxValue);
                    case GreaterThanOrEqual:
                        return new NumericValueSet<int, NonNegativeIntTC>(Math.Max(0, value), int.MaxValue);
                    case Equal:
                        if (value < 0)
                            return NumericValueSet<int, NonNegativeIntTC>.NoValues;
                        return new NumericValueSet<int, NonNegativeIntTC>(value, value);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }
            }

            IValueSet IValueSetFactory.Random(int expectedSize, Random random) => _underlying.Random(expectedSize, random);

            ConstantValue IValueSetFactory.RandomValue(Random random) => _underlying.RandomValue(random);

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? AllValues : Related(relation, default(NonNegativeIntTC).FromConstantValue(value));

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right) => _underlying.Related(relation, left, right);
        }
    }
}
