// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set factory that only supports equality and works by including or excluding specific values.
        /// </summary>
        private class EnumeratedValueSetFactory<T, TTC> : IValueSetFactory<T> where TTC : struct, EqualableValueTC<T>
        {
            public static EnumeratedValueSetFactory<T, TTC> Instance = new EnumeratedValueSetFactory<T, TTC>();

            private EnumeratedValueSetFactory() { }

            IValueSet<T> IValueSetFactory<T>.All => EnumeratedValueSet<T, TTC>.AllValues;

            IValueSet IValueSetFactory.All => EnumeratedValueSet<T, TTC>.AllValues;

            IValueSet<T> IValueSetFactory<T>.None => EnumeratedValueSet<T, TTC>.None;

            IValueSet IValueSetFactory.None => EnumeratedValueSet<T, TTC>.None;

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
                value.IsBad ? EnumeratedValueSet<T, TTC>.AllValues : this.Related(relation, default(TTC).FromConstantValue(value));

            IValueSet<T> IValueSetFactory<T>.Random(int expectedSize, Random random)
            {
                throw new NotImplementedException($"IValueSetFactory<{typeof(T).ToString()}>.Random");
            }
        }
    }
}
