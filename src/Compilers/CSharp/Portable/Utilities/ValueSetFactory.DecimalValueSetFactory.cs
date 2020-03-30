// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class DecimalValueSetFactory : NumericValueSetFactory<decimal, DecimalTC>, IValueSetFactory<decimal>, IValueSetFactory
        {
            public static new readonly DecimalValueSetFactory Instance = new DecimalValueSetFactory();

            public new IValueSet<decimal> Related(BinaryOperatorKind relation, decimal value)
            {
                return base.Related(relation, DecimalTC.Normalize(value));
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? NumericValueSet<decimal, DecimalTC>.AllValues : Related(relation, default(DecimalTC).FromConstantValue(value));
        }
    }
}
