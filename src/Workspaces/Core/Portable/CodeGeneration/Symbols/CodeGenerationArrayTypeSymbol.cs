﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationArrayTypeSymbol : CodeGenerationTypeSymbol, IArrayTypeSymbol
    {
        public CodeGenerationArrayTypeSymbol(ITypeSymbol elementType, int rank, NullableAnnotation nullableAnnotation)
            : base(null, default, Accessibility.NotApplicable, default, string.Empty, SpecialType.None, nullableAnnotation)
        {
            this.ElementType = elementType;
            this.Rank = rank;
        }

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

        protected override CodeGenerationTypeSymbol CloneWithNullableAnnotation(NullableAnnotation nullableAnnotation)
        {
            return new CodeGenerationArrayTypeSymbol(this.ElementType, this.Rank, nullableAnnotation);
        }

        public override TypeKind TypeKind => TypeKind.Array;

        public override SymbolKind Kind => SymbolKind.ArrayType;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitArrayType(this);
        }

        [return: MaybeNull]
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

        public NullableAnnotation ElementNullableAnnotation => ElementType.NullableAnnotation;

        public bool Equals(IArrayTypeSymbol? other)
        {
            return SymbolEquivalenceComparer.Instance.Equals(this, other);
        }
    }
}
