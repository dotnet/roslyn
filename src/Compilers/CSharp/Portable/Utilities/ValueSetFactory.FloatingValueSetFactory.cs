// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class FloatingValueSetFactory<TFloating, TFloatingTC> : IValueSetFactory<TFloating> where TFloatingTC : struct, FloatingTC<TFloating>
        {
            public static readonly FloatingValueSetFactory<TFloating, TFloatingTC> Instance = new FloatingValueSetFactory<TFloating, TFloatingTC>();

            private FloatingValueSetFactory() { }

            public IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value) =>
                FloatingValueSet<TFloating, TFloatingTC>.Related(relation, value);

            IValueSet<TFloating> IValueSetFactory<TFloating>.Random(int expectedSize, Random random) =>
                FloatingValueSet<TFloating, TFloatingTC>.Random(expectedSize, random);

            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? FloatingValueSet<TFloating, TFloatingTC>.AllValues : FloatingValueSet<TFloating, TFloatingTC>.Related(relation, default(TFloatingTC).FromConstantValue(value));
        }
    }
}
