// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set factory that only supports equality and works by including or excluding specific values.
        /// </summary>
        private sealed class EnumeratedValueSetFactory<T, TTC> : IValueSetFactory<T> where TTC : struct, IEquatableValueTC<T> where T : notnull
        {
            public static readonly EnumeratedValueSetFactory<T, TTC> Instance = new EnumeratedValueSetFactory<T, TTC>();

            IValueSet IValueSetFactory.AllValues => EnumeratedValueSet<T, TTC>.AllValues;

            IValueSet IValueSetFactory.NoValues => EnumeratedValueSet<T, TTC>.NoValues;

            private EnumeratedValueSetFactory() { }

            public IValueSet<T> Related(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case Equal:
                        return EnumeratedValueSet<T, TTC>.Including(value);
                    default:
                        return EnumeratedValueSet<T, TTC>.AllValues; // supported for error recovery
                }
            }

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || value.IsNull ? EnumeratedValueSet<T, TTC>.AllValues : this.Related(relation, default(TTC).FromConstantValue(value));

            bool IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right)
            {
                Debug.Assert(relation == BinaryOperatorKind.Equal);
                TTC tc = default;
                return tc.FromConstantValue(left).Equals(tc.FromConstantValue(right));
            }

            public IValueSet Random(int expectedSize, Random random)
            {
                TTC tc = default;
                T[] values = tc.RandomValues(expectedSize, random, expectedSize * 2);
                IValueSet<T> result = EnumeratedValueSet<T, TTC>.NoValues;
                Debug.Assert(result.IsEmpty);
                foreach (T value in values)
                    result = result.Union(Related(Equal, value));

                return result;
            }

            ConstantValue IValueSetFactory.RandomValue(Random random)
            {
                TTC tc = default;
                return tc.ToConstantValue(tc.RandomValues(1, random, 100)[0]);
            }
        }
    }
}
