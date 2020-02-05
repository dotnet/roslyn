// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private EnumeratedValueSetFactory() { }
            public static EnumeratedValueSetFactory<T, TTC> Instance = new EnumeratedValueSetFactory<T, TTC>();
            IValueSet<T> IValueSetFactory<T>.All => EnumeratedValueSet<T, TTC>.AllValues;
            IValueSet IValueSetFactory.All => EnumeratedValueSet<T, TTC>.AllValues;
            IValueSet<T> IValueSetFactory<T>.None => EnumeratedValueSet<T, TTC>.None;
            IValueSet IValueSetFactory.None => EnumeratedValueSet<T, TTC>.None;
            public IValueSet<T> Related(BinaryOperatorKind relation, T value) => relation switch
            {
                Equal => EnumeratedValueSet<T, TTC>.Including(value),
                _ => EnumeratedValueSet<T, TTC>.AllValues, // supported for error recovery
            };
            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) => value.IsBad ? EnumeratedValueSet<T, TTC>.AllValues : this.Related(relation, default(TTC).FromConstantValue(value));
        }
    }
}
