// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Generates symbols that describe declarations to be generated.
    /// </summary>
    internal static class CodeGenerationSymbolFactory
    {
        /// <summary>
        /// Determines if the symbol is purely a code generation symbol.
        /// </summary>
        public static bool IsCodeGenerationSymbol(this ISymbol symbol)
        {
            return symbol is CodeGenerationSymbol;
        }

        /// <summary>
        /// Creates an event symbol that can be used to describe an event declaration.
        /// </summary>
        public static IEventSymbol CreateEventSymbol(
            ImmutableArray<AttributeData> attributes, Accessibility accessibility,
            DeclarationModifiers modifiers, ITypeSymbol type,
            IEventSymbol explicitInterfaceSymbol, string name,
            IMethodSymbol addMethod = null, 
            IMethodSymbol removeMethod = null,
            IMethodSymbol raiseMethod = null)
        {
            var result = new CodeGenerationEventSymbol(null, attributes, accessibility, modifiers, type, explicitInterfaceSymbol, name, addMethod, removeMethod, raiseMethod);
            CodeGenerationEventInfo.Attach(result, modifiers.IsUnsafe);
            return result;
        }

        internal static IPropertySymbol CreatePropertySymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            bool returnsByRef,
            IPropertySymbol explicitInterfaceSymbol,
            string name,
            ImmutableArray<IParameterSymbol> parameters,
            IMethodSymbol getMethod,
            IMethodSymbol setMethod,
            bool isIndexer = false,
            SyntaxNode initializer = null)
        {
            var result = new CodeGenerationPropertySymbol(
                containingType,
                attributes,
                accessibility,
                modifiers,
                type,
                returnsByRef,
                explicitInterfaceSymbol,
                name,
                isIndexer,
                parameters,
                getMethod,
                setMethod);
            CodeGenerationPropertyInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, initializer);
            return result;
        }

        /// <summary>
        /// Creates a property symbol that can be used to describe a property declaration.
        /// </summary>
        public static IPropertySymbol CreatePropertySymbol(
            ImmutableArray<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers,
            ITypeSymbol type, bool returnsByRef, IPropertySymbol explicitInterfaceSymbol, string name,
            ImmutableArray<IParameterSymbol> parameters, IMethodSymbol getMethod, IMethodSymbol setMethod,
            bool isIndexer = false)
        {
            return CreatePropertySymbol(
                containingType: null,
                attributes: attributes,
                accessibility: accessibility,
                modifiers: modifiers,
                type: type,
                returnsByRef: returnsByRef,
                explicitInterfaceSymbol: explicitInterfaceSymbol,
                name: name,
                parameters: parameters,
                getMethod: getMethod,
                setMethod: setMethod,
                isIndexer: isIndexer);
        }

        /// <summary>
        /// Creates a field symbol that can be used to describe a field declaration.
        /// </summary>
        public static IFieldSymbol CreateFieldSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type, string name,
            bool hasConstantValue = false,
            object constantValue = null,
            SyntaxNode initializer = null)
        {
            var result = new CodeGenerationFieldSymbol(null, attributes, accessibility, modifiers, type, name, hasConstantValue, constantValue);
            CodeGenerationFieldInfo.Attach(result, modifiers.IsUnsafe, modifiers.IsWithEvents, initializer);
            return result;
        }

        /// <summary>
        /// Creates a constructor symbol that can be used to describe a constructor declaration.
        /// </summary>
        public static IMethodSymbol CreateConstructorSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            string typeName,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<SyntaxNode> baseConstructorArguments = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<SyntaxNode> thisConstructorArguments = default(ImmutableArray<SyntaxNode>))
        {
            var result = new CodeGenerationConstructorSymbol(null, attributes, accessibility, modifiers, parameters);
            CodeGenerationConstructorInfo.Attach(result, typeName, statements, baseConstructorArguments, thisConstructorArguments);
            return result;
        }

        /// <summary>
        /// Creates a destructor symbol that can be used to describe a destructor declaration.
        /// </summary>
        public static IMethodSymbol CreateDestructorSymbol(
            ImmutableArray<AttributeData> attributes, string typeName,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>))
        {
            var result = new CodeGenerationDestructorSymbol(null, attributes);
            CodeGenerationDestructorInfo.Attach(result, typeName, statements);
            return result;
        }

        internal static IMethodSymbol CreateMethodSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility, DeclarationModifiers modifiers,
            ITypeSymbol returnType, bool returnsByRef,
            IMethodSymbol explicitInterfaceSymbol, string name,
            ImmutableArray<ITypeParameterSymbol> typeParameters,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<SyntaxNode> handlesExpressions = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<AttributeData> returnTypeAttributes = default(ImmutableArray<AttributeData>),
            MethodKind methodKind = MethodKind.Ordinary)
        {
            var result = new CodeGenerationMethodSymbol(containingType, attributes, accessibility, modifiers, returnType, returnsByRef, explicitInterfaceSymbol, name, typeParameters, parameters, returnTypeAttributes, methodKind);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions);
            return result;
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe a method declaration.
        /// </summary>
        public static IMethodSymbol CreateMethodSymbol(
            ImmutableArray<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers,
            ITypeSymbol returnType, bool returnsByRef, IMethodSymbol explicitInterfaceSymbol, string name,
            ImmutableArray<ITypeParameterSymbol> typeParameters, ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<SyntaxNode> handlesExpressions = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<AttributeData> returnTypeAttributes = default(ImmutableArray<AttributeData>),
            MethodKind methodKind = MethodKind.Ordinary)
        {
            return CreateMethodSymbol(null, attributes, accessibility, modifiers, returnType, returnsByRef, explicitInterfaceSymbol, name, typeParameters, parameters, statements, handlesExpressions, returnTypeAttributes, methodKind);
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe an operator declaration.
        /// </summary>
        public static IMethodSymbol CreateOperatorSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol returnType,
            CodeGenerationOperatorKind operatorKind,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<AttributeData> returnTypeAttributes = default(ImmutableArray<AttributeData>))
        {
            int expectedParameterCount = CodeGenerationOperatorSymbol.GetParameterCount(operatorKind);
            if (parameters.Length != expectedParameterCount)
            {
                var message = expectedParameterCount == 1 ?
                    WorkspacesResources.Invalid_number_of_parameters_for_unary_operator :
                    WorkspacesResources.Invalid_number_of_parameters_for_binary_operator;
                throw new ArgumentException(message, nameof(parameters));
            }

            var result = new CodeGenerationOperatorSymbol(null, attributes, accessibility, modifiers, returnType, operatorKind, parameters, returnTypeAttributes);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions: default(ImmutableArray<SyntaxNode>));
            return result;
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe a conversion declaration.
        /// </summary>
        public static IMethodSymbol CreateConversionSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol toType,
            IParameterSymbol fromType,
            bool isImplicit = false,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>),
            ImmutableArray<AttributeData> toTypeAttributes = default(ImmutableArray<AttributeData>))
        {
            var result = new CodeGenerationConversionSymbol(null, attributes, accessibility, modifiers, toType, fromType, isImplicit, toTypeAttributes);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions: default(ImmutableArray<SyntaxNode>));
            return result;
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static IParameterSymbol CreateParameterSymbol(ITypeSymbol type, string name)
        {
            return CreateParameterSymbol(
                attributes: default(ImmutableArray<AttributeData>), refKind: RefKind.None, isParams: false, type: type, name: name, isOptional: false);
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static IParameterSymbol CreateParameterSymbol(
            ImmutableArray<AttributeData> attributes, RefKind refKind, bool isParams, ITypeSymbol type, string name, bool isOptional = false, bool hasDefaultValue = false, object defaultValue = null)
        {
            return new CodeGenerationParameterSymbol(null, attributes, refKind, isParams, type, name, isOptional, hasDefaultValue, defaultValue);
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static ITypeParameterSymbol CreateTypeParameterSymbol(string name, int ordinal = 0)
        {
            return CreateTypeParameter(
                attributes: default(ImmutableArray<AttributeData>), varianceKind: VarianceKind.None,
                name: name, constraintTypes: ImmutableArray.Create<ITypeSymbol>(),
                hasConstructorConstraint: false, hasReferenceConstraint: false, hasValueConstraint: false,
                ordinal: ordinal);
        }

        /// <summary>
        /// Creates a type parameter symbol that can be used to describe a type parameter declaration.
        /// </summary>
        public static ITypeParameterSymbol CreateTypeParameter(
            ImmutableArray<AttributeData> attributes,
            VarianceKind varianceKind, string name,
            ImmutableArray<ITypeSymbol> constraintTypes,
            bool hasConstructorConstraint = false,
            bool hasReferenceConstraint = false,
            bool hasValueConstraint = false, int ordinal = 0)
        {
            return new CodeGenerationTypeParameterSymbol(null, attributes, varianceKind, name, constraintTypes, hasConstructorConstraint, hasReferenceConstraint, hasValueConstraint, ordinal);
        }

        /// <summary>
        /// Creates a pointer type symbol that can be used to describe a pointer type reference.
        /// </summary>
        public static IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
        {
            return new CodeGenerationPointerTypeSymbol(pointedAtType);
        }

        /// <summary>
        /// Creates an array type symbol that can be used to describe an array type reference.
        /// </summary>
        public static IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank = 1)
        {
            return new CodeGenerationArrayTypeSymbol(elementType, rank);
        }

        internal static IMethodSymbol CreateAccessorSymbol(
            IMethodSymbol accessor,
            ImmutableArray<AttributeData> attributes = default(ImmutableArray<AttributeData>),
            Accessibility? accessibility = null,
            IMethodSymbol explicitInterfaceSymbol = null,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>))
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility ?? accessor.DeclaredAccessibility,
                accessor.GetSymbolModifiers().WithIsAbstract(statements == null),
                accessor.ReturnType,
                accessor.ReturnsByRef,
                explicitInterfaceSymbol ?? accessor.ExplicitInterfaceImplementations.FirstOrDefault(),
                accessor.Name,
                accessor.TypeParameters,
                accessor.Parameters,
                statements,
                returnTypeAttributes: accessor.GetReturnTypeAttributes());
        }

        /// <summary>
        /// Creates an method type symbol that can be used to describe an accessor method declaration.
        /// </summary>
        public static IMethodSymbol CreateAccessorSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            ImmutableArray<SyntaxNode> statements)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility,
                new DeclarationModifiers(isAbstract: statements == null),
                returnType: null, 
                returnsByRef: false, 
                explicitInterfaceSymbol: null,
                name: string.Empty,
                typeParameters: default(ImmutableArray<ITypeParameterSymbol>), 
                parameters: default(ImmutableArray<IParameterSymbol>),
                statements: statements);
        }

        /// <summary>
        /// Create attribute data that can be used in describing an attribute declaration.
        /// </summary>
        public static AttributeData CreateAttributeData(
            INamedTypeSymbol attributeClass,
            ImmutableArray<TypedConstant> constructorArguments = default(ImmutableArray<TypedConstant>),
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<string, TypedConstant>>))
        {
            return new CodeGenerationAttributeData(attributeClass, constructorArguments, namedArguments);
        }

        /// <summary>
        /// Creates a named type symbol that can be used to describe a named type declaration.
        /// </summary>
        public static INamedTypeSymbol CreateNamedTypeSymbol(
            ImmutableArray<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            TypeKind typeKind, string name,
            ImmutableArray<ITypeParameterSymbol> typeParameters = default(ImmutableArray<ITypeParameterSymbol>),
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> interfaces = default(ImmutableArray<INamedTypeSymbol>),
            SpecialType specialType = SpecialType.None,
            ImmutableArray<ISymbol> members = default(ImmutableArray<ISymbol>))
        {
            members = members.NullToEmpty();

            return new CodeGenerationNamedTypeSymbol(
                null, attributes, accessibility, modifiers, typeKind, name,
                typeParameters, baseType, interfaces, specialType,
                members.WhereAsArray(m => !(m is INamedTypeSymbol)),
                members.OfType<INamedTypeSymbol>().Select(n => n.ToCodeGenerationSymbol()).ToImmutableArray(),
                enumUnderlyingType: null);
        }

        /// <summary>
        /// Creates a method type symbol that can be used to describe a delegate type declaration.
        /// </summary>
        public static CodeGenerationNamedTypeSymbol CreateDelegateTypeSymbol(
            ImmutableArray<AttributeData> attributes, 
            Accessibility accessibility, 
            DeclarationModifiers modifiers, 
            ITypeSymbol returnType, 
            bool returnsByRef,
            string name,
            ImmutableArray<ITypeParameterSymbol> typeParameters = default(ImmutableArray<ITypeParameterSymbol>),
            ImmutableArray<IParameterSymbol> parameters = default(ImmutableArray<IParameterSymbol>))
        {
            var invokeMethod = CreateMethodSymbol(
                attributes: default(ImmutableArray<AttributeData>),
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                returnType: returnType,
                returnsByRef: returnsByRef,
                explicitInterfaceSymbol: null,
                name: "Invoke",
                typeParameters: default(ImmutableArray<ITypeParameterSymbol>),
                parameters: parameters);

            return new CodeGenerationNamedTypeSymbol(
                containingType: null,
                attributes: attributes,
                declaredAccessibility: accessibility,
                modifiers: modifiers,
                typeKind: TypeKind.Delegate,
                name: name,
                typeParameters: typeParameters,
                baseType: null,
                interfaces: default(ImmutableArray<INamedTypeSymbol>),
                specialType: SpecialType.None,
                members: ImmutableArray.Create<ISymbol>(invokeMethod),
                typeMembers: ImmutableArray<CodeGenerationAbstractNamedTypeSymbol>.Empty,
                enumUnderlyingType: null);
        }

        /// <summary>
        /// Creates a namespace symbol that can be used to describe a namespace declaration.
        /// </summary>
        public static INamespaceSymbol CreateNamespaceSymbol(string name, IList<ISymbol> imports = null, IList<INamespaceOrTypeSymbol> members = null)
        {
            var @namespace = new CodeGenerationNamespaceSymbol(name, members);
            CodeGenerationNamespaceInfo.Attach(@namespace, imports);
            return @namespace;
        }

        internal static IMethodSymbol CreateMethodSymbol(
            IMethodSymbol method,
            ImmutableArray<AttributeData> attributes = default(ImmutableArray<AttributeData>),
            Accessibility? accessibility = null,
            DeclarationModifiers? modifiers = null,
            IMethodSymbol explicitInterfaceSymbol = null,
            string name = null,
            ImmutableArray<SyntaxNode> statements = default(ImmutableArray<SyntaxNode>))
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility ?? method.DeclaredAccessibility,
                modifiers ?? method.GetSymbolModifiers(),
                method.ReturnType,
                method.ReturnsByRef,
                explicitInterfaceSymbol,
                name ?? method.Name,
                method.TypeParameters,
                method.Parameters,
                statements,
                returnTypeAttributes: method.GetReturnTypeAttributes());
        }

        internal static IPropertySymbol CreatePropertySymbol(
            IPropertySymbol property,
            ImmutableArray<AttributeData> attributes = default(ImmutableArray<AttributeData>),
            Accessibility? accessibility = null,
            DeclarationModifiers? modifiers = null,
            IPropertySymbol explicitInterfaceSymbol = null,
            string name = null,
            bool? isIndexer = null,
            IMethodSymbol getMethod = null,
            IMethodSymbol setMethod = null)
        {
            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes,
                accessibility ?? property.DeclaredAccessibility,
                modifiers ?? property.GetSymbolModifiers(),
                property.Type,
                property.ReturnsByRef,
                explicitInterfaceSymbol,
                name ?? property.Name,
                property.Parameters,
                getMethod,
                setMethod,
                isIndexer ?? property.IsIndexer);
        }

        internal static IEventSymbol CreateEventSymbol(
            IEventSymbol @event,
            ImmutableArray<AttributeData> attributes = default(ImmutableArray<AttributeData>),
            Accessibility? accessibility = null,
            DeclarationModifiers? modifiers = null,
            IEventSymbol explicitInterfaceSymbol = null,
            string name = null,
            IMethodSymbol addMethod = null,
            IMethodSymbol removeMethod = null)
        {
            return CodeGenerationSymbolFactory.CreateEventSymbol(
                attributes,
                accessibility ?? @event.DeclaredAccessibility,
                modifiers ?? @event.GetSymbolModifiers(),
                @event.Type,
                explicitInterfaceSymbol,
                name ?? @event.Name,
                addMethod,
                removeMethod);
        }
    }
}
