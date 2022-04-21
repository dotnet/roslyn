// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationPointerTypeSymbol : CodeGenerationTypeSymbol, IPointerTypeSymbol
    {
        public ITypeSymbol PointedAtType { get; }

        public CodeGenerationPointerTypeSymbol(ITypeSymbol pointedAtType)
            : base(null, null, default, Accessibility.NotApplicable, default, string.Empty, SpecialType.None, NullableAnnotation.None)
        {
            this.PointedAtType = pointedAtType;
        }

        protected override CodeGenerationTypeSymbol CloneWithNullableAnnotation(NullableAnnotation nullableAnnotation)
        {
            // We ignore the nullableAnnotation parameter because pointer types can't be nullable.
            return new CodeGenerationPointerTypeSymbol(this.PointedAtType);
        }

        public override TypeKind TypeKind => TypeKind.Pointer;

        public override SymbolKind Kind => SymbolKind.PointerType;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitPointerType(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitPointerType(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitPointerType(this, argument);

        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }
    }
}
