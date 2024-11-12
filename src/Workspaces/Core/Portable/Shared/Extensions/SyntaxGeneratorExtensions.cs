// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SyntaxGeneratorExtensions
{
    public static IMethodSymbol CreateBaseDelegatingConstructor(
        this SyntaxGenerator factory,
        IMethodSymbol constructor,
        string typeName)
    {
        // Create a constructor that calls the base constructor.  Note: if there are no
        // parameters then don't bother writing out "base()" it's automatically implied.
        return CodeGenerationSymbolFactory.CreateConstructorSymbol(
            attributes: default,
            accessibility: Accessibility.Public,
            modifiers: new DeclarationModifiers(),
            typeName: typeName,
            parameters: constructor.Parameters,
            statements: default,
            baseConstructorArguments: constructor.Parameters.Length == 0
                ? default
                : factory.CreateArguments(constructor.Parameters));
    }

    public static async Task<IPropertySymbol> OverridePropertyAsync(
        this SyntaxGenerator codeFactory,
        IPropertySymbol overriddenProperty,
        DeclarationModifiers modifiers,
        INamedTypeSymbol containingType,
        Document document,
        CancellationToken cancellationToken)
    {
        var getAccessibility = overriddenProperty.GetMethod.ComputeResultantAccessibility(containingType);
        var setAccessibility = overriddenProperty.SetMethod.ComputeResultantAccessibility(containingType);

        SyntaxNode? getBody;
        SyntaxNode? setBody;
        // Implement an abstract property by throwing not implemented in accessors.
        if (overriddenProperty.IsAbstract)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var statement = codeFactory.CreateThrowNotImplementedStatement(compilation);

            getBody = statement;
            setBody = statement;
        }
        else if (overriddenProperty.IsIndexer() && document.Project.Language == LanguageNames.CSharp)
        {
            // Indexer: return or set base[]. Only in C#, since VB must refer to these by name.

            getBody = codeFactory.ReturnStatement(
                WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                    codeFactory.ElementAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.CreateArguments(overriddenProperty.Parameters))));

            setBody = codeFactory.ExpressionStatement(
                codeFactory.AssignmentStatement(
                codeFactory.ElementAccessExpression(
                    codeFactory.BaseExpression(),
                    codeFactory.CreateArguments(overriddenProperty.Parameters)),
                codeFactory.IdentifierName("value")));
        }
        else if (overriddenProperty.GetParameters().Any())
        {
            // Call accessors directly if C# overriding VB
            if (document.Project.Language == LanguageNames.CSharp
                && await SymbolFinder.FindSourceDefinitionAsync(overriddenProperty, document.Project.Solution, cancellationToken).ConfigureAwait(false) is { Language: LanguageNames.VisualBasic })
            {
                var getName = overriddenProperty.GetMethod?.Name;
                var setName = overriddenProperty.SetMethod?.Name;

                getBody = getName == null
                    ? null
                    : codeFactory.ReturnStatement(
                codeFactory.InvocationExpression(
                    codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.IdentifierName(getName)),
                    codeFactory.CreateArguments(overriddenProperty.Parameters)));

                setBody = setName == null
                    ? null
                    : codeFactory.ExpressionStatement(
                    codeFactory.InvocationExpression(
                        codeFactory.MemberAccessExpression(
                            codeFactory.BaseExpression(),
                            codeFactory.IdentifierName(setName)),
                        codeFactory.CreateArguments(overriddenProperty.SetMethod.GetParameters())));
            }
            else
            {
                getBody = codeFactory.ReturnStatement(
                    WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                        codeFactory.InvocationExpression(
                            codeFactory.MemberAccessExpression(
                                codeFactory.BaseExpression(),
                                codeFactory.IdentifierName(overriddenProperty.Name)), codeFactory.CreateArguments(overriddenProperty.Parameters))));

                setBody = codeFactory.ExpressionStatement(
                    codeFactory.AssignmentStatement(
                        codeFactory.InvocationExpression(
                        codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                    codeFactory.IdentifierName(overriddenProperty.Name)), codeFactory.CreateArguments(overriddenProperty.Parameters)),
                    codeFactory.IdentifierName("value")));
            }
        }
        else
        {
            // Regular property: return or set the base property

            getBody = codeFactory.ReturnStatement(
                WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                    codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.IdentifierName(overriddenProperty.Name))));

            setBody = codeFactory.ExpressionStatement(
                codeFactory.AssignmentStatement(
                    codeFactory.MemberAccessExpression(
                    codeFactory.BaseExpression(),
                codeFactory.IdentifierName(overriddenProperty.Name)),
                codeFactory.IdentifierName("value")));
        }

        // Only generate a getter if the base getter is accessible.
        IMethodSymbol? accessorGet = null;
        if (overriddenProperty.GetMethod != null && overriddenProperty.GetMethod.IsAccessibleWithin(containingType))
        {
            accessorGet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenProperty.GetMethod,
                accessibility: getAccessibility,
                statements: getBody != null ? [getBody] : [],
                modifiers: modifiers);
        }

        // Only generate a setter if the base setter is accessible.
        IMethodSymbol? accessorSet = null;
        if (overriddenProperty.SetMethod is { DeclaredAccessibility: not Accessibility.Private } &&
            overriddenProperty.SetMethod.IsAccessibleWithin(containingType))
        {
            accessorSet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenProperty.SetMethod,
                accessibility: setAccessibility,
                statements: setBody != null ? [setBody] : [],
                modifiers: modifiers);
        }

        return CodeGenerationSymbolFactory.CreatePropertySymbol(
            overriddenProperty,
            accessibility: overriddenProperty.ComputeResultantAccessibility(containingType),
            modifiers: modifiers,
            name: overriddenProperty.Name,
            parameters: overriddenProperty.RemoveInaccessibleAttributesAndAttributesOfTypes(containingType).Parameters,
            isIndexer: overriddenProperty.IsIndexer(),
            getMethod: accessorGet,
            setMethod: accessorSet);
    }

    private static SyntaxNode WrapWithRefIfNecessary(SyntaxGenerator codeFactory, IPropertySymbol overriddenProperty, SyntaxNode body)
        => overriddenProperty.ReturnsByRef
            ? codeFactory.RefExpression(body)
            : body;

    public static IEventSymbol OverrideEvent(
        IEventSymbol overriddenEvent,
        DeclarationModifiers modifiers,
        INamedTypeSymbol newContainingType)
    {
        return CodeGenerationSymbolFactory.CreateEventSymbol(
            overriddenEvent,
            attributes: default,
            accessibility: overriddenEvent.ComputeResultantAccessibility(newContainingType),
            modifiers: modifiers,
            explicitInterfaceImplementations: default,
            name: overriddenEvent.Name);
    }

    public static async Task<ISymbol> OverrideAsync(
        this SyntaxGenerator generator,
        ISymbol symbol,
        INamedTypeSymbol containingType,
        Document document,
        DeclarationModifiers extraDeclarationModifiers = default,
        CancellationToken cancellationToken = default)
    {
        var modifiers = GetOverrideModifiers(symbol) + extraDeclarationModifiers;

        if (symbol is IMethodSymbol method)
        {
            return await generator.OverrideMethodAsync(method,
                modifiers, containingType, document, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is IPropertySymbol property)
        {
            return await generator.OverridePropertyAsync(property,
                modifiers, containingType, document, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is IEventSymbol ev)
        {
            return OverrideEvent(ev, modifiers, containingType);
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static DeclarationModifiers GetOverrideModifiers(ISymbol symbol)
        => symbol.GetSymbolModifiers()
                 .WithIsOverride(true)
                 .WithIsAbstract(false)
                 .WithIsVirtual(false);

    private static async Task<IMethodSymbol> OverrideMethodAsync(
        this SyntaxGenerator codeFactory,
        IMethodSymbol overriddenMethod,
        DeclarationModifiers modifiers,
        INamedTypeSymbol newContainingType,
        Document newDocument,
        CancellationToken cancellationToken)
    {
        // Required is not a valid modifier for methods, so clear it if the user typed it
        modifiers = modifiers.WithIsRequired(false);

        // Abstract: Throw not implemented
        if (overriddenMethod.IsAbstract)
        {
            var compilation = await newDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var statement = codeFactory.CreateThrowNotImplementedStatement(compilation);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenMethod,
                accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                modifiers: modifiers,
                statements: [statement]);
        }
        else
        {
            // Otherwise, call the base method with the same parameters
            var typeParams = overriddenMethod.GetTypeArguments();
            var body = codeFactory.InvocationExpression(
                codeFactory.MemberAccessExpression(codeFactory.BaseExpression(),
                typeParams.IsDefaultOrEmpty
                    ? codeFactory.IdentifierName(overriddenMethod.Name)
                    : codeFactory.GenericName(overriddenMethod.Name, typeParams)),
                codeFactory.CreateArguments(overriddenMethod.GetParameters()));

            if (overriddenMethod.ReturnsByRef)
            {
                body = codeFactory.RefExpression(body);
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method: overriddenMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(newContainingType),
                accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                modifiers: modifiers,
                statements: overriddenMethod.ReturnsVoid
                    ? [codeFactory.ExpressionStatement(body)]
                    : [codeFactory.ReturnStatement(body)]);
        }
    }
}
