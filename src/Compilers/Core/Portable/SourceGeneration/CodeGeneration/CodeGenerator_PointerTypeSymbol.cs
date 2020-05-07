// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IPointerTypeSymbol Pointer(ITypeSymbol pointedAtType)
            => new PointerTypeSymbol(pointedAtType);

        public static IPointerTypeSymbol With(
            this IPointerTypeSymbol pointer,
            Optional<ITypeSymbol> pointedAtType = default)
        {
            return new PointerTypeSymbol(
                pointedAtType.GetValueOr(pointer.PointedAtType));
        }

        private class PointerTypeSymbol : TypeSymbol, IPointerTypeSymbol
        {
            public PointerTypeSymbol(ITypeSymbol pointedAtType)
            {
                PointedAtType = pointedAtType;
            }

            public override SymbolKind Kind => SymbolKind.PointerType;
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
