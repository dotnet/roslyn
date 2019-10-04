// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        private sealed class PointerTypeSymbolWithNullableAnnotation : TypeSymbolWithNullableAnnotation, IPointerTypeSymbol
        {
            public PointerTypeSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability) : base(wrappedSymbol, nullability)
            {
            }

            private new IPointerTypeSymbol WrappedSymbol => (IPointerTypeSymbol)base.WrappedSymbol;

            public override void Accept(SymbolVisitor visitor)
            {
                visitor.VisitPointerType(this);
            }

            [return: MaybeNull]
            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
                return visitor.VisitPointerType(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            }

            #region IPointerTypeSymbol Implementation Forwards

            public ITypeSymbol PointedAtType => WrappedSymbol.PointedAtType;

            public ImmutableArray<CustomModifier> CustomModifiers => WrappedSymbol.CustomModifiers;

            #endregion
        }
    }
}
