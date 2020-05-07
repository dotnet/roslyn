// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IDynamicTypeSymbol DynamicType(
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new DynamicTypeSymbol(nullableAnnotation);
        }

        public static IDynamicTypeSymbol With(
            IDynamicTypeSymbol dynamicType,
            Optional<NullableAnnotation> nullableAnnotation = default)
        {
            return new DynamicTypeSymbol(
                nullableAnnotation.GetValueOr(dynamicType.NullableAnnotation));
        }

        private class DynamicTypeSymbol : TypeSymbol, IDynamicTypeSymbol
        {
            public DynamicTypeSymbol(NullableAnnotation nullableAnnotation)
            {
                NullableAnnotation = nullableAnnotation;
            }

            public override SymbolKind Kind => SymbolKind.DynamicType;
            public override NullableAnnotation NullableAnnotation { get; }
            public override TypeKind TypeKind => TypeKind.Dynamic;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitDynamicType(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitDynamicType(this);

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
