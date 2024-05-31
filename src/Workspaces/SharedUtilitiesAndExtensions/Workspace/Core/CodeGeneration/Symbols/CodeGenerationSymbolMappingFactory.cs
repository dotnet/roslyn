// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal class CodeGenerationSymbolMappingFactory : SymbolMappingFactory
{
    private static readonly FrozenDictionary<Type, Type> s_interfaceMapping = FrozenDictionary.ToFrozenDictionary(
        [
            (typeof(ICodeGenerationArrayTypeSymbol), typeof(IArrayTypeSymbol)),
            (typeof(ICodeGenerationEventSymbol), typeof(IEventSymbol)),
            (typeof(ICodeGenerationFieldSymbol), typeof(IFieldSymbol)),
            (typeof(ICodeGenerationMethodSymbol), typeof(IMethodSymbol)),
            (typeof(ICodeGenerationNamedTypeSymbol), typeof(INamedTypeSymbol)),
            (typeof(ICodeGenerationNamespaceOrTypeSymbol), typeof(INamespaceOrTypeSymbol)),
            (typeof(ICodeGenerationNamespaceSymbol), typeof(INamespaceSymbol)),
            (typeof(ICodeGenerationParameterSymbol), typeof(IParameterSymbol)),
            (typeof(ICodeGenerationPointerTypeSymbol), typeof(IPointerTypeSymbol)),
            (typeof(ICodeGenerationPropertySymbol), typeof(IPropertySymbol)),
            (typeof(ICodeGenerationSymbol), typeof(ISymbol)),
            (typeof(ICodeGenerationTypeParameterSymbol), typeof(ITypeParameterSymbol)),
            (typeof(ICodeGenerationTypeSymbol), typeof(ITypeSymbol)),
        ],
        pair => pair.Item1,
        pair => pair.Item2);

    public static readonly CodeGenerationSymbolMappingFactory Instance = new();

    private CodeGenerationSymbolMappingFactory()
        : base(s_interfaceMapping)
    {
    }

    public IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank, NullableAnnotation nullableAnnotation)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationArrayTypeSymbol));
        var constructor = (Func<ITypeSymbol, int, NullableAnnotation, CodeGenerationArrayTypeSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(ITypeSymbol),
                typeof(int),
                typeof(NullableAnnotation),
            ]);

        return (IArrayTypeSymbol)constructor(elementType, rank, nullableAnnotation);
    }

    public IMethodSymbol CreateConstructedMethodSymbol(
        CodeGenerationAbstractMethodSymbol constructedFrom,
        ImmutableArray<ITypeSymbol> typeArguments)
    {
        throw new NotImplementedException();
        ////var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationConstructedMethodSymbol));
        ////var constructor = (Func<CodeGenerationAbstractMethodSymbol, ImmutableArray<ITypeSymbol>, CodeGenerationConstructedMethodSymbol>)GetOrCreateConstructor(
        ////    implementationType,
        ////    [
        ////        typeof(CodeGenerationAbstractMethodSymbol),
        ////        typeof(ImmutableArray<ITypeSymbol>),
        ////    ]);

        ////return (IMethodSymbol)constructor(constructedFrom, typeArguments);
    }

    public INamedTypeSymbol CreateConstructedNamedTypeSymbol(
        CodeGenerationNamedTypeSymbol constructedFrom,
        ImmutableArray<ITypeSymbol> typeArguments,
        ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationConstructedNamedTypeSymbol));
        var constructor = (Func<CodeGenerationNamedTypeSymbol, ImmutableArray<ITypeSymbol>, ImmutableArray<CodeGenerationAbstractNamedTypeSymbol>, CodeGenerationConstructedNamedTypeSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(CodeGenerationNamedTypeSymbol),
                typeof(ImmutableArray<ITypeSymbol>),
                typeof(ImmutableArray<CodeGenerationAbstractNamedTypeSymbol>),
            ]);

        return (INamedTypeSymbol)constructor(constructedFrom, typeArguments, typeMembers);
    }

    public IMethodSymbol CreateConstructorSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationConstructorSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ImmutableArray<IParameterSymbol>, CodeGenerationConstructorSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ImmutableArray<IParameterSymbol>),
            ]);

        return (IMethodSymbol)constructor(containingType, attributes, accessibility, modifiers, parameters);
    }

    public IMethodSymbol CreateConversionSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol toType,
        IParameterSymbol fromType,
        bool isImplicit,
        ImmutableArray<AttributeData> toTypeAttributes,
        string? documentationCommentXml)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationConversionSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol, IParameterSymbol, bool, ImmutableArray<AttributeData>, string?, CodeGenerationConversionSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(IParameterSymbol),
                typeof(bool),
                typeof(ImmutableArray<AttributeData>),
                typeof(string),
            ]);

        return (IMethodSymbol)constructor(containingType, attributes, declaredAccessibility, modifiers, toType, fromType, isImplicit, toTypeAttributes, documentationCommentXml);
    }

    public IMethodSymbol CreateDestructorSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationDestructorSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, CodeGenerationDestructorSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
            ]);

        return (IMethodSymbol)constructor(containingType, attributes);
    }

    public IEventSymbol CreateEventSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol type,
        ImmutableArray<IEventSymbol> explicitInterfaceImplementations,
        string name,
        IMethodSymbol? addMethod,
        IMethodSymbol? removeMethod,
        IMethodSymbol? raiseMethod)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationEventSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol, ImmutableArray<IEventSymbol>, string, IMethodSymbol?, IMethodSymbol?, IMethodSymbol?, CodeGenerationEventSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(ImmutableArray<IEventSymbol>),
                typeof(string),
                typeof(IMethodSymbol),
                typeof(IMethodSymbol),
                typeof(IMethodSymbol),
            ]);

        return (IEventSymbol)constructor(containingType, attributes, declaredAccessibility, modifiers, type, explicitInterfaceImplementations, name, addMethod, removeMethod, raiseMethod);
    }

    public IFieldSymbol CreateFieldSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol type,
        string name,
        bool hasConstantValue,
        object? constantValue)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationFieldSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol, string, bool, object?, CodeGenerationFieldSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(string),
                typeof(bool),
                typeof(object),
            ]);

        return (IFieldSymbol)constructor(containingType, attributes, accessibility, modifiers, type, name, hasConstantValue, constantValue);
    }

    public IMethodSymbol CreateMethodSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol? returnType,
        RefKind refKind,
        ImmutableArray<IMethodSymbol> explicitInterfaceImplementations,
        string name,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<AttributeData> returnTypeAttributes,
        string? documentationCommentXml = null,
        MethodKind methodKind = MethodKind.Ordinary,
        bool isInitOnly = false)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationMethodSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol?, RefKind, ImmutableArray<IMethodSymbol>, string, ImmutableArray<ITypeParameterSymbol>, ImmutableArray<IParameterSymbol>, ImmutableArray<AttributeData>, string?, MethodKind, bool, CodeGenerationMethodSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(RefKind),
                typeof(ImmutableArray<IMethodSymbol>),
                typeof(string),
                typeof(ImmutableArray<ITypeParameterSymbol>),
                typeof(ImmutableArray<IParameterSymbol>),
                typeof(ImmutableArray<AttributeData>),
                typeof(string),
                typeof(MethodKind),
                typeof(bool),
            ]);

        return (IMethodSymbol)constructor(containingType, attributes, declaredAccessibility, modifiers, returnType, refKind, explicitInterfaceImplementations, name, typeParameters, parameters, returnTypeAttributes, documentationCommentXml, methodKind, isInitOnly);
    }

    public INamedTypeSymbol CreateNamedTypeSymbol(
        IAssemblySymbol? containingAssembly,
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        bool isRecord,
        TypeKind typeKind,
        string name,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        INamedTypeSymbol? baseType,
        ImmutableArray<INamedTypeSymbol> interfaces,
        SpecialType specialType,
        NullableAnnotation nullableAnnotation,
        ImmutableArray<ISymbol> members,
        ImmutableArray<CodeGenerationAbstractNamedTypeSymbol> typeMembers,
        INamedTypeSymbol? enumUnderlyingType)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationNamedTypeSymbol));
        var constructor = (Func<IAssemblySymbol?, INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, bool, TypeKind, string, ImmutableArray<ITypeParameterSymbol>, INamedTypeSymbol?, ImmutableArray<INamedTypeSymbol>, SpecialType, NullableAnnotation, ImmutableArray<ISymbol>, ImmutableArray<CodeGenerationAbstractNamedTypeSymbol>, INamedTypeSymbol?, CodeGenerationNamedTypeSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(IAssemblySymbol),
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(bool),
                typeof(TypeKind),
                typeof(string),
                typeof(ImmutableArray<ITypeParameterSymbol>),
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<INamedTypeSymbol>),
                typeof(SpecialType),
                typeof(NullableAnnotation),
                typeof(ImmutableArray<ISymbol>),
                typeof(ImmutableArray<CodeGenerationAbstractNamedTypeSymbol>),
                typeof(INamedTypeSymbol),
            ]);

        return (INamedTypeSymbol)constructor(containingAssembly, containingType, attributes, declaredAccessibility, modifiers, isRecord, typeKind, name, typeParameters, baseType, interfaces, specialType, nullableAnnotation, members, typeMembers, enumUnderlyingType);
    }

    public INamespaceSymbol CreateNamespaceSymbol(string name, IList<INamespaceOrTypeSymbol>? members)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationNamespaceSymbol));
        var constructor = (Func<string, IList<INamespaceOrTypeSymbol>?, CodeGenerationNamespaceSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(string),
                typeof(IList<INamespaceOrTypeSymbol>),
            ]);

        return (INamespaceSymbol)constructor(name, members);
    }

    public IMethodSymbol CreateOperatorSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility accessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol returnType,
        CodeGenerationOperatorKind operatorKind,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<AttributeData> returnTypeAttributes,
        string? documentationCommentXml)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationOperatorSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol, CodeGenerationOperatorKind, ImmutableArray<IParameterSymbol>, ImmutableArray<AttributeData>, string?, CodeGenerationOperatorSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(CodeGenerationOperatorKind),
                typeof(ImmutableArray<IParameterSymbol>),
                typeof(ImmutableArray<AttributeData>),
                typeof(string),
            ]);

        return (IMethodSymbol)constructor(containingType, attributes, accessibility, modifiers, returnType, operatorKind, parameters, returnTypeAttributes, documentationCommentXml);
    }

    public IParameterSymbol CreateParameterSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        RefKind refKind,
        bool isParams,
        ITypeSymbol type,
        string name,
        bool isOptional,
        bool hasDefaultValue,
        object? defaultValue)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationParameterSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, RefKind, bool, ITypeSymbol, string, bool, bool, object?, CodeGenerationParameterSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(RefKind),
                typeof(bool),
                typeof(ITypeSymbol),
                typeof(string),
                typeof(bool),
                typeof(bool),
                typeof(object),
            ]);

        return (IParameterSymbol)constructor(containingType, attributes, refKind, isParams, type, name, isOptional, hasDefaultValue, defaultValue);
    }

    public IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
        => throw new NotImplementedException();

    public IPropertySymbol CreatePropertySymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol type,
        RefKind refKind,
        ImmutableArray<IPropertySymbol> explicitInterfaceImplementations,
        string name,
        bool isIndexer,
        ImmutableArray<IParameterSymbol> parametersOpt,
        IMethodSymbol? getMethod,
        IMethodSymbol? setMethod)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationPropertySymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, Accessibility, DeclarationModifiers, ITypeSymbol, RefKind, ImmutableArray<IPropertySymbol>, string, bool, ImmutableArray<IParameterSymbol>, IMethodSymbol?, IMethodSymbol?, CodeGenerationPropertySymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(Accessibility),
                typeof(DeclarationModifiers),
                typeof(ITypeSymbol),
                typeof(RefKind),
                typeof(ImmutableArray<IPropertySymbol>),
                typeof(string),
                typeof(bool),
                typeof(ImmutableArray<IParameterSymbol>),
                typeof(IMethodSymbol),
                typeof(IMethodSymbol),
            ]);

        return (IPropertySymbol)constructor(containingType, attributes, declaredAccessibility, modifiers, type, refKind, explicitInterfaceImplementations, name, isIndexer, parametersOpt, getMethod, setMethod);
    }

    public ITypeParameterSymbol CreateTypeParameterSymbol(
        INamedTypeSymbol? containingType,
        ImmutableArray<AttributeData> attributes,
        VarianceKind varianceKind,
        string name,
        NullableAnnotation nullableAnnotation,
        ImmutableArray<ITypeSymbol> constraintTypes,
        bool hasConstructorConstraint,
        bool hasReferenceConstraint,
        bool hasValueConstraint,
        bool hasUnmanagedConstraint,
        bool hasNotNullConstraint,
        bool allowsRefLikeType,
        int ordinal)
    {
        var implementationType = GetOrCreateImplementationType(typeof(CodeGenerationTypeParameterSymbol));
        var constructor = (Func<INamedTypeSymbol?, ImmutableArray<AttributeData>, VarianceKind, string, NullableAnnotation, ImmutableArray<ITypeSymbol>, bool, bool, bool, bool, bool, bool, int, CodeGenerationTypeParameterSymbol>)GetOrCreateConstructor(
            implementationType,
            [
                typeof(INamedTypeSymbol),
                typeof(ImmutableArray<AttributeData>),
                typeof(VarianceKind),
                typeof(string),
                typeof(NullableAnnotation),
                typeof(ImmutableArray<ITypeSymbol>),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(int),
            ]);

        return (ITypeParameterSymbol)constructor(containingType, attributes, varianceKind, name, nullableAnnotation, constraintTypes, hasConstructorConstraint, hasReferenceConstraint, hasValueConstraint, hasUnmanagedConstraint, hasNotNullConstraint, allowsRefLikeType, ordinal);
    }
}
