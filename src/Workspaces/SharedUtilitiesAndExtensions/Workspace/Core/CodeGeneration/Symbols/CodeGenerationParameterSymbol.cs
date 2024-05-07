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

internal abstract class CodeGenerationParameterSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    RefKind refKind,
    bool isParams,
    ITypeSymbol type,
    string name,
    bool isOptional,
    bool hasDefaultValue,
    object defaultValue) : CodeGenerationSymbol(containingType?.ContainingAssembly, containingType, attributes, Accessibility.NotApplicable, new DeclarationModifiers(), name), ICodeGenerationParameterSymbol
{
    public RefKind RefKind { get; } = refKind;
    public bool IsParams { get; } = isParams;
    bool ICodeGenerationParameterSymbol.IsParamsArray => IsParams;
    bool ICodeGenerationParameterSymbol.IsParamsCollection => false;
    public ITypeSymbol Type { get; } = type;
    public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
    public bool IsOptional { get; } = isOptional;
    public int Ordinal { get; }

    public bool HasExplicitDefaultValue { get; } = hasDefaultValue;
    public object ExplicitDefaultValue { get; } = defaultValue;

    protected override CodeGenerationSymbol Clone()
    {
        return (CodeGenerationSymbol)CodeGenerationSymbolMappingFactory.Instance.CreateParameterSymbol(
            this.ContainingType, this.GetAttributes(), this.RefKind,
            this.IsParams, this.Type, this.Name, this.IsOptional, this.HasExplicitDefaultValue,
            this.ExplicitDefaultValue);
    }

    public new IParameterSymbol OriginalDefinition => (IParameterSymbol)this;

    public override SymbolKind Kind => SymbolKind.Parameter;

    public override void Accept(SymbolVisitor visitor)
        => visitor.VisitParameter((IParameterSymbol)this);

    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        => visitor.VisitParameter((IParameterSymbol)this);

    public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitParameter((IParameterSymbol)this, argument);

    public bool IsThis => false;

    public ImmutableArray<CustomModifier> RefCustomModifiers => [];

    public ImmutableArray<CustomModifier> CustomModifiers => [];

    public ScopedKind ScopedKind => ScopedKind.None;

    public bool IsDiscard => false;
}
