// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationArrayTypeSymbol(ITypeSymbol elementType, int rank, NullableAnnotation nullableAnnotation) : CodeGenerationTypeSymbol(null, null, default, Accessibility.NotApplicable, default, string.Empty, SpecialType.None, nullableAnnotation), IArrayTypeSymbol
{
    public ITypeSymbol ElementType { get; } = elementType;

    public int Rank { get; } = rank;

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
            return [];
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
        => new CodeGenerationArrayTypeSymbol(this.ElementType, this.Rank, nullableAnnotation);

    public override TypeKind TypeKind => TypeKind.Array;

    public override SymbolKind Kind => SymbolKind.ArrayType;

    public override void Accept(SymbolVisitor visitor)
        => visitor.VisitArrayType(this);

    public override TResult? Accept<TResult>(SymbolVisitor<TResult> visitor)
        where TResult : default
        => visitor.VisitArrayType(this);

    public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitArrayType(this, argument);

    public ImmutableArray<CustomModifier> CustomModifiers
    {
        get
        {
            return [];
        }
    }

    public NullableAnnotation ElementNullableAnnotation => ElementType.NullableAnnotation;

    public bool Equals(IArrayTypeSymbol? other)
        => SymbolEquivalenceComparer.Instance.Equals(this, other);
}
