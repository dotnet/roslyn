// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationArrayTypeSymbol : CodeGenerationTypeSymbol, IArrayTypeSymbol
    {
        public ITypeSymbol ElementType { get; }

        public int Rank { get; }

        public CodeGenerationArrayTypeSymbol(ITypeSymbol elementType, int rank)
            : base(null, null, Accessibility.NotApplicable, default(DeclarationModifiers), string.Empty, SpecialType.None)
        {
            this.ElementType = elementType;
            this.Rank = rank;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationArrayTypeSymbol(this.ElementType, this.Rank);
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Array;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.ArrayType;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitArrayType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitArrayType(this);
        }

        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return ImmutableArray.Create<CustomModifier>();
            }
        }

        public bool Equals(IArrayTypeSymbol other)
        {
            return SymbolEquivalenceComparer.Instance.Equals(this, other);
        }
    }
}
