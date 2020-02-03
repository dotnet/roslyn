// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private class FloatingValueSetFactory<TFloating, TFloatingTC> : IValueSetFactory<TFloating> where TFloatingTC : struct, FloatingTC<TFloating>
        {
            private FloatingValueSetFactory() { }
            public static readonly FloatingValueSetFactory<TFloating, TFloatingTC> Instance = new FloatingValueSetFactory<TFloating, TFloatingTC>();
            IValueSet<TFloating> IValueSetFactory<TFloating>.All => FloatingValueSet<TFloating, TFloatingTC>.AllValues;
            IValueSet IValueSetFactory.All => FloatingValueSet<TFloating, TFloatingTC>.AllValues;
            IValueSet<TFloating> IValueSetFactory<TFloating>.None => FloatingValueSet<TFloating, TFloatingTC>.None;
            IValueSet IValueSetFactory.None => FloatingValueSet<TFloating, TFloatingTC>.None;
            public IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value) =>
                FloatingValueSet<TFloating, TFloatingTC>.Related(relation, value);
            IValueSet IValueSetFactory.Related(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad ? FloatingValueSet<TFloating, TFloatingTC>.AllValues : FloatingValueSet<TFloating, TFloatingTC>.Related(relation, default(TFloatingTC).FromConstantValue(value));
        }
    }
}
