// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IArrayTypeSymbol ArrayType(
            ITypeSymbol elementType,
            int rank = 1,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new ArrayTypeSymbol(
                elementType,
                rank,
                nullableAnnotation);
        }

        public static IArrayTypeSymbol With(
            this IArrayTypeSymbol arrayType,
            Optional<ITypeSymbol> elementType = default,
            Optional<int> rank = default,
            Optional<NullableAnnotation> nullableAnnotation = default)
        {
            return new ArrayTypeSymbol(
                elementType.GetValueOr(arrayType.ElementType),
                rank.GetValueOr(arrayType.Rank),
                nullableAnnotation.GetValueOr(arrayType.NullableAnnotation));
        }

        private class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
        {
            public ITypeSymbol ElementType { get; }
            public int Rank { get; }

            public ArrayTypeSymbol(
                ITypeSymbol elementType,
                int rank,
                NullableAnnotation nullableAnnotation)
            {
                ElementType = elementType;
                Rank = rank;
                NullableAnnotation = nullableAnnotation;
            }

            public override SymbolKind Kind => SymbolKind.ArrayType;
            public override NullableAnnotation NullableAnnotation { get; }
            public override TypeKind TypeKind => TypeKind.Array;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitArrayType(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitArrayType(this);

            #region default implementation

            public bool IsSZArray => throw new NotImplementedException();

            public ImmutableArray<int> LowerBounds => throw new NotImplementedException();

            public ImmutableArray<int> Sizes => throw new NotImplementedException();

            public NullableAnnotation ElementNullableAnnotation => throw new NotImplementedException();

            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();

            public bool Equals(IArrayTypeSymbol other)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
