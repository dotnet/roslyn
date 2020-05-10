// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IPointerTypeSymbol Pointer(
            ITypeSymbol pointedAtType,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new PointerTypeSymbol(
                pointedAtType,
                nullableAnnotation);
        }

        private static IPointerTypeSymbol WithPointedAtType(this IPointerTypeSymbol arrayType, ITypeSymbol pointedAtType)
            => With(arrayType, pointedAtType: ToOptional(pointedAtType));

        private static IPointerTypeSymbol WithNullableAnnotation(this IPointerTypeSymbol arrayType, NullableAnnotation nullableAnnotation)
            => With(arrayType, nullableAnnotation: ToOptional(nullableAnnotation));

        private static IPointerTypeSymbol With(
            this IPointerTypeSymbol pointer,
            Optional<ITypeSymbol> pointedAtType = default,
            Optional<NullableAnnotation> nullableAnnotation = default)
        {
            return new PointerTypeSymbol(
                pointedAtType.GetValueOr(pointer.PointedAtType),
                nullableAnnotation.GetValueOr(pointer.NullableAnnotation));
        }

        private class PointerTypeSymbol : TypeSymbol, IPointerTypeSymbol
        {
            public PointerTypeSymbol(
                ITypeSymbol pointedAtType,
                NullableAnnotation nullableAnnotation)
            {
                PointedAtType = pointedAtType;
                NullableAnnotation = nullableAnnotation;
            }

            public override SymbolKind Kind => SymbolKind.PointerType;
            public override NullableAnnotation NullableAnnotation { get; }
            public override TypeKind TypeKind => TypeKind.Pointer;

            public ITypeSymbol PointedAtType { get; }

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitPointerType(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitPointerType(this);

            #region default implementation

            public ImmutableArray<CustomModifier> CustomModifiers => throw new NotImplementedException();

            #endregion
        }
    }
}
