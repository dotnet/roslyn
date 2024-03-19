// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal class CodeGenerationFieldSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    Accessibility accessibility,
    DeclarationModifiers modifiers,
    ITypeSymbol type,
    string name,
    bool hasConstantValue,
    object constantValue) : CodeGenerationSymbol(containingType?.ContainingAssembly, containingType, attributes, accessibility, modifiers, name), IFieldSymbol
{
    public ITypeSymbol Type { get; } = type;
    public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
    public object ConstantValue { get; } = constantValue;
    public bool HasConstantValue { get; } = hasConstantValue;

    protected override CodeGenerationSymbol Clone()
    {
        return new CodeGenerationFieldSymbol(
            this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
            this.Modifiers, this.Type, this.Name, this.HasConstantValue, this.ConstantValue);
    }

    public new IFieldSymbol OriginalDefinition
    {
        get
        {
            return this;
        }
    }

    public IFieldSymbol CorrespondingTupleField => null;

    public override SymbolKind Kind => SymbolKind.Field;

    public override void Accept(SymbolVisitor visitor)
        => visitor.VisitField(this);

    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        => visitor.VisitField(this);

    public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitField(this, argument);

    public bool IsConst
    {
        get
        {
            return this.Modifiers.IsConst;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            return this.Modifiers.IsReadOnly;
        }
    }

    public bool IsVolatile => false;

    public bool IsRequired => Modifiers.IsRequired;

    public bool IsFixedSizeBuffer => false;

    public int FixedSize => 0;

    public RefKind RefKind => RefKind.None;

    public ImmutableArray<CustomModifier> RefCustomModifiers => [];

    public ImmutableArray<CustomModifier> CustomModifiers
    {
        get
        {
            return [];
        }
    }

    public ISymbol AssociatedSymbol => null;

    public bool IsExplicitlyNamedTupleElement => false;
}
