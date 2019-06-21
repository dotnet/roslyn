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

        public bool IsSZArray
        {
            get
            {
                return Rank == 1;
            }
        }

        public ImmutableArray<int> Sizes
        {
            get
            {
                return ImmutableArray<int>.Empty;
            }
        }

        public ImmutableArray<int> LowerBounds
        {
            get
            {
                return default;
            }
        }

        public CodeGenerationArrayTypeSymbol(ITypeSymbol elementType, int rank)
            : base(null, default, Accessibility.NotApplicable, default, string.Empty, SpecialType.None)
        {
            this.ElementType = elementType;
            this.Rank = rank;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationArrayTypeSymbol(this.ElementType, this.Rank);
        }

        public override TypeKind TypeKind => TypeKind.Array;

        public override SymbolKind Kind => SymbolKind.ArrayType;

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

        public NullableAnnotation ElementNullableAnnotation => ElementType.GetNullability();

        public bool Equals(IArrayTypeSymbol other)
        {
            return SymbolEquivalenceComparer.Instance.Equals(this, other);
        }
    }
}
