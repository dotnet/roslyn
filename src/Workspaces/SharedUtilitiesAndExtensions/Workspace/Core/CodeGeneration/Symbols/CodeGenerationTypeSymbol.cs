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

internal abstract class CodeGenerationTypeSymbol : CodeGenerationNamespaceOrTypeSymbol, ICodeGenerationTypeSymbol
{
    public SpecialType SpecialType { get; protected set; }

    protected CodeGenerationTypeSymbol(
        IAssemblySymbol containingAssembly,
        INamedTypeSymbol containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        string name,
        SpecialType specialType,
        NullableAnnotation nullableAnnotation)
        : base(containingAssembly, containingType, attributes, declaredAccessibility, modifiers, name)
    {
        this.SpecialType = specialType;
        this.NullableAnnotation = nullableAnnotation;
    }

    public abstract TypeKind TypeKind { get; }

    public virtual INamedTypeSymbol BaseType => null;

    public virtual ImmutableArray<INamedTypeSymbol> Interfaces
        => [];

    public ImmutableArray<INamedTypeSymbol> AllInterfaces
        => [];

    public bool IsReferenceType => false;

    public bool IsValueType => TypeKind is TypeKind.Struct or TypeKind.Enum;

    public bool IsAnonymousType => false;

    public bool IsTupleType => false;

    public bool IsNativeIntegerType => false;

    public static ImmutableArray<ITypeSymbol> TupleElementTypes => default;

    public static ImmutableArray<string> TupleElementNames => default;

    public new ITypeSymbol OriginalDefinition => (ITypeSymbol)this;

    public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember) => null;

    public string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        => throw new System.NotImplementedException();

    public ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        => throw new System.NotImplementedException();

    public string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
        => throw new System.NotImplementedException();

    public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
        => throw new System.NotImplementedException();

    public override bool IsNamespace => false;

    public override bool IsType => true;

    bool ICodeGenerationTypeSymbol.IsRefLikeType => throw new System.NotImplementedException();

    bool ICodeGenerationTypeSymbol.IsUnmanagedType => throw new System.NotImplementedException();

    bool ICodeGenerationTypeSymbol.IsReadOnly => Modifiers.IsReadOnly;

    public virtual bool IsRecord => false;

    public NullableAnnotation NullableAnnotation { get; }

    public ITypeSymbol WithNullableAnnotation(NullableAnnotation nullableAnnotation)
    {
        if (this.NullableAnnotation == nullableAnnotation)
        {
            return (ITypeSymbol)this;
        }

        return (ITypeSymbol)CloneWithNullableAnnotation(nullableAnnotation);
    }

    protected sealed override CodeGenerationSymbol Clone()
        => CloneWithNullableAnnotation(this.NullableAnnotation);

    protected abstract CodeGenerationTypeSymbol CloneWithNullableAnnotation(NullableAnnotation nullableAnnotation);
}
