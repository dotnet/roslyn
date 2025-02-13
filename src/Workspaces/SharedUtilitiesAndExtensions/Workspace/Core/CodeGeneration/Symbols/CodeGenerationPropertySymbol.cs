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

internal sealed class CodeGenerationPropertySymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    Accessibility declaredAccessibility,
    DeclarationModifiers modifiers,
    ITypeSymbol type,
    RefKind refKind,
    ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
    string name,
    bool isIndexer,
    ImmutableArray<IParameterSymbol> parametersOpt,
    IMethodSymbol getMethod,
    IMethodSymbol setMethod) : CodeGenerationSymbol(containingType?.ContainingAssembly, containingType, attributes, declaredAccessibility, modifiers, name), IPropertySymbol
{
    public ITypeSymbol Type { get; } = type;
    public NullableAnnotation NullableAnnotation => Type.NullableAnnotation;
    public bool IsIndexer { get; } = isIndexer;

    public ImmutableArray<IParameterSymbol> Parameters { get; } = parametersOpt.NullToEmpty();
    public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; } = explicitInterfaceImplementations.NullToEmpty();

    public IMethodSymbol GetMethod { get; } = getMethod;
    public IMethodSymbol SetMethod { get; } = setMethod;

    protected override CodeGenerationSymbol Clone()
    {
        var result = new CodeGenerationPropertySymbol(
            this.ContainingType, this.GetAttributes(), this.DeclaredAccessibility,
            this.Modifiers, this.Type, this.RefKind, this.ExplicitInterfaceImplementations,
            this.Name, this.IsIndexer, this.Parameters,
            this.GetMethod, this.SetMethod);
        CodeGenerationPropertyInfo.Attach(result,
            CodeGenerationPropertyInfo.GetIsNew(this),
            CodeGenerationPropertyInfo.GetIsUnsafe(this),
            CodeGenerationPropertyInfo.GetInitializer(this));

        return result;
    }

    public override SymbolKind Kind => SymbolKind.Property;

    public override void Accept(SymbolVisitor visitor)
        => visitor.VisitProperty(this);

    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        => visitor.VisitProperty(this);

    public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitProperty(this, argument);

    public bool IsReadOnly => this.GetMethod != null && this.SetMethod == null;

    public bool IsWriteOnly => this.GetMethod == null && this.SetMethod != null;

    public bool IsRequired => Modifiers.IsRequired && !IsIndexer;

    public new IPropertySymbol OriginalDefinition => this;

    public RefKind RefKind => refKind;

    public bool ReturnsByRef => refKind == RefKind.Ref;

    public bool ReturnsByRefReadonly => refKind == RefKind.RefReadOnly;

    public IPropertySymbol OverriddenProperty => null;

    public bool IsWithEvents => false;

    public ImmutableArray<CustomModifier> RefCustomModifiers => [];

    public ImmutableArray<CustomModifier> TypeCustomModifiers => [];

    public IPropertySymbol PartialImplementationPart => null;

    public IPropertySymbol PartialDefinitionPart => null;

    public bool IsPartialDefinition => false;
}
