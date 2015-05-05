// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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
        public static IEventSymbol CreateEventSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, IEventSymbol explicitInterfaceSymbol, string name, IMethodSymbol addMethod = null, IMethodSymbol removeMethod = null, IMethodSymbol raiseMethod = null, IList<IParameterSymbol> parameterList = null)
        {
            var result = new CodeGenerationEventSymbol(null, attributes, accessibility, modifiers, type, explicitInterfaceSymbol, name, addMethod, removeMethod, raiseMethod, parameterList);
            CodeGenerationEventInfo.Attach(result, modifiers.IsUnsafe);
            return result;
        }

        internal static IPropertySymbol CreatePropertySymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol type,
            IPropertySymbol explicitInterfaceSymbol,
            string name,
            IList<IParameterSymbol> parameters,
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
        public static IPropertySymbol CreatePropertySymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, IPropertySymbol explicitInterfaceSymbol, string name, IList<IParameterSymbol> parameters, IMethodSymbol getMethod, IMethodSymbol setMethod, bool isIndexer = false)
        {
            return CreatePropertySymbol(
                containingType: null,
                attributes: attributes,
                accessibility: accessibility,
                modifiers: modifiers,
                type: type,
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
        public static IFieldSymbol CreateFieldSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, string name, bool hasConstantValue = false, object constantValue = null, SyntaxNode initializer = null)
        {
            var result = new CodeGenerationFieldSymbol(null, attributes, accessibility, modifiers, type, name, hasConstantValue, constantValue);
            CodeGenerationFieldInfo.Attach(result, modifiers.IsUnsafe, modifiers.IsWithEvents, initializer);
            return result;
        }

        /// <summary>
        /// Creates a constructor symbol that can be used to describe a constructor declaration.
        /// </summary>
        public static IMethodSymbol CreateConstructorSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, string typeName, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> baseConstructorArguments = null, IList<SyntaxNode> thisConstructorArguments = null)
        {
            var result = new CodeGenerationConstructorSymbol(null, attributes, accessibility, modifiers, parameters);
            CodeGenerationConstructorInfo.Attach(result, typeName, statements, baseConstructorArguments, thisConstructorArguments);
            return result;
        }

        /// <summary>
        /// Creates a destructor symbol that can be used to describe a destructor declaration.
        /// </summary>
        public static IMethodSymbol CreateDestructorSymbol(IList<AttributeData> attributes, string typeName, IList<SyntaxNode> statements = null)
        {
            var result = new CodeGenerationDestructorSymbol(null, attributes);
            CodeGenerationDestructorInfo.Attach(result, typeName, statements);
            return result;
        }

        internal static IMethodSymbol CreateMethodSymbol(INamedTypeSymbol containingType, IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, IMethodSymbol explicitInterfaceSymbol, string name, IList<ITypeParameterSymbol> typeParameters, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> handlesExpressions = null, IList<AttributeData> returnTypeAttributes = null, MethodKind methodKind = MethodKind.Ordinary, bool returnsByRef = false)
        {
            var result = new CodeGenerationMethodSymbol(containingType, attributes, accessibility, modifiers, returnType, returnsByRef, explicitInterfaceSymbol, name, typeParameters, parameters, returnTypeAttributes, methodKind);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions);
            return result;
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe a method declaration.
        /// </summary>
        public static IMethodSymbol CreateMethodSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, IMethodSymbol explicitInterfaceSymbol, string name, IList<ITypeParameterSymbol> typeParameters, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> handlesExpressions = null, IList<AttributeData> returnTypeAttributes = null, MethodKind methodKind = MethodKind.Ordinary)
        {
            return CreateMethodSymbol(null, attributes, accessibility, modifiers, returnType, explicitInterfaceSymbol, name, typeParameters, parameters, statements, handlesExpressions, returnTypeAttributes, methodKind);
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe an operator declaration.
        /// </summary>
        public static IMethodSymbol CreateOperatorSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, CodeGenerationOperatorKind operatorKind, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<AttributeData> returnTypeAttributes = null)
        {
            int expectedParameterCount = CodeGenerationOperatorSymbol.GetParameterCount(operatorKind);
            if (parameters.Count != expectedParameterCount)
            {
                var message = expectedParameterCount == 1 ?
                    WorkspacesResources.InvalidParameterCountForUnaryOperator :
                    WorkspacesResources.InvalidParameterCountForBinaryOperator;
                throw new ArgumentException(message, "parameters");
            }

            var result = new CodeGenerationOperatorSymbol(null, attributes, accessibility, modifiers, returnType, operatorKind, parameters, returnTypeAttributes);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions: null);
            return result;
        }

        /// <summary>
        /// Creates a method symbol that can be used to describe a conversion declaration.
        /// </summary>
        public static IMethodSymbol CreateConversionSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol toType, IParameterSymbol fromType, bool isImplicit = false, IList<SyntaxNode> statements = null, IList<AttributeData> toTypeAttributes = null)
        {
            var result = new CodeGenerationConversionSymbol(null, attributes, accessibility, modifiers, toType, fromType, isImplicit, toTypeAttributes);
            CodeGenerationMethodInfo.Attach(result, modifiers.IsNew, modifiers.IsUnsafe, modifiers.IsPartial, modifiers.IsAsync, statements, handlesExpressions: null);
            return result;
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static IParameterSymbol CreateParameterSymbol(ITypeSymbol type, string name)
        {
            return CreateParameterSymbol(attributes: null, refKind: RefKind.None, isParams: false, type: type, name: name, isOptional: false);
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static IParameterSymbol CreateParameterSymbol(IList<AttributeData> attributes, RefKind refKind, bool isParams, ITypeSymbol type, string name, bool isOptional = false, bool hasDefaultValue = false, object defaultValue = null)
        {
            return new CodeGenerationParameterSymbol(null, attributes, refKind, isParams, type, name, isOptional, hasDefaultValue, defaultValue);
        }

        /// <summary>
        /// Creates a parameter symbol that can be used to describe a parameter declaration.
        /// </summary>
        public static ITypeParameterSymbol CreateTypeParameterSymbol(string name, int ordinal = 0)
        {
            return CreateTypeParameter(attributes: null, varianceKind: VarianceKind.None,
            name: name, constraintTypes: ImmutableArray.Create<ITypeSymbol>(),
            hasConstructorConstraint: false, hasReferenceConstraint: false, hasValueConstraint: false,
            ordinal: ordinal);
        }

        /// <summary>
        /// Creates a type parameter symbol that can be used to describe a type parameter declaration.
        /// </summary>
        public static ITypeParameterSymbol CreateTypeParameter(IList<AttributeData> attributes, VarianceKind varianceKind, string name, ImmutableArray<ITypeSymbol> constraintTypes, bool hasConstructorConstraint = false, bool hasReferenceConstraint = false, bool hasValueConstraint = false, int ordinal = 0)
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
            IList<AttributeData> attributes = null,
            Accessibility? accessibility = null,
            IMethodSymbol explicitInterfaceSymbol = null,
            IList<SyntaxNode> statements = null)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility ?? accessor.DeclaredAccessibility,
                accessor.GetSymbolModifiers().WithIsAbstract(statements == null),
                accessor.ReturnType,
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
            IList<AttributeData> attributes,
            Accessibility accessibility,
            IList<SyntaxNode> statements)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility,
                new DeclarationModifiers(isAbstract: statements == null),
                null, null,
                string.Empty,
                null, null,
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
        public static INamedTypeSymbol CreateNamedTypeSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, TypeKind typeKind, string name, IList<ITypeParameterSymbol> typeParameters = null, INamedTypeSymbol baseType = null, IList<INamedTypeSymbol> interfaces = null, SpecialType specialType = SpecialType.None, IList<ISymbol> members = null)
        {
            members = members ?? SpecializedCollections.EmptyList<ISymbol>();

            return new CodeGenerationNamedTypeSymbol(
                null, attributes, accessibility, modifiers, typeKind, name,
                typeParameters, baseType, interfaces, specialType,
                members.Where(m => !(m is INamedTypeSymbol)).ToList(),
                members.OfType<INamedTypeSymbol>().Select(n => n.ToCodeGenerationSymbol()).ToList(),
                enumUnderlyingType: null);
        }

        /// <summary>
        /// Creates a method type symbol that can be used to describe a delegate type declaration.
        /// </summary>
        public static INamedTypeSymbol CreateDelegateTypeSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, string name, IList<ITypeParameterSymbol> typeParameters = null, IList<IParameterSymbol> parameters = null)
        {
            var invokeMethod = CreateMethodSymbol(
                attributes: null,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                returnType: returnType,
                explicitInterfaceSymbol: null,
                name: "Invoke",
                typeParameters: null,
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
                interfaces: null,
                specialType: SpecialType.None,
                members: new[] { invokeMethod },
                typeMembers: SpecializedCollections.EmptyList<CodeGenerationAbstractNamedTypeSymbol>(),
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
            IList<AttributeData> attributes = null,
            Accessibility? accessibility = null,
            DeclarationModifiers? modifiers = null,
            IMethodSymbol explicitInterfaceSymbol = null,
            string name = null,
            IList<SyntaxNode> statements = null)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes,
                accessibility ?? method.DeclaredAccessibility,
                modifiers ?? method.GetSymbolModifiers(),
                method.ReturnType,
                explicitInterfaceSymbol,
                name ?? method.Name,
                method.TypeParameters,
                method.Parameters,
                statements,
                returnTypeAttributes: method.GetReturnTypeAttributes());
        }

        internal static IPropertySymbol CreatePropertySymbol(
            IPropertySymbol property,
            IList<AttributeData> attributes = null,
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
                explicitInterfaceSymbol,
                name ?? property.Name,
                property.Parameters,
                getMethod,
                setMethod,
                isIndexer ?? property.IsIndexer);
        }

        internal static IEventSymbol CreateEventSymbol(
            IEventSymbol @event,
            IList<AttributeData> attributes = null,
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
