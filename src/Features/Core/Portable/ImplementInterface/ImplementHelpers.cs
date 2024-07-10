// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal static class ImplementHelpers
{
    public static ImmutableArray<ISymbol> GetDelegatableMembers(
        Document document,
        INamedTypeSymbol namedType,
        Func<ITypeSymbol, bool> includeMemberType,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var fields = namedType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsImplicitlyDeclared)
            .Where(f => includeMemberType(f.Type))
            .ToImmutableArray();

        var properties = namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsImplicitlyDeclared && p.Parameters.Length == 0 && p.GetMethod != null)
            .Where(p => includeMemberType(p.Type))
            .ToImmutableArray();

        var parameters = GetNonCapturedPrimaryConstructorParameters(fields, properties);

        return [.. fields, .. properties, .. parameters];

        ImmutableArray<IParameterSymbol> GetNonCapturedPrimaryConstructorParameters(
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<IPropertySymbol> properties)
        {
            using var _2 = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

            var primaryConstructor = namedType.InstanceConstructors
                .FirstOrDefault(c => c.Parameters.Length > 0 && c.Parameters[0].IsPrimaryConstructor(cancellationToken));
            if (primaryConstructor != null)
            {
                foreach (var parameter in primaryConstructor.Parameters)
                {
                    if (includeMemberType(parameter.Type) &&
                        !IsAssignedToFieldOrProperty(fields, properties, parameter))
                    {
                        result.Add(parameter);
                    }
                }
            }

            return result.ToImmutableAndClear();
        }

        bool IsAssignedToFieldOrProperty(ImmutableArray<IFieldSymbol> fields, ImmutableArray<IPropertySymbol> properties, IParameterSymbol parameter)
            => fields.Any(f => IsAssignedToField(f, parameter)) || properties.Any(p => IsAssignedToProperty(p, parameter));

        bool IsAssignedToField(IFieldSymbol field, IParameterSymbol parameter)
        {
            if (field.DeclaringSyntaxReferences is [var syntaxRef, ..])
            {
                var declarator = syntaxRef.GetSyntax(cancellationToken);
                if (syntaxFacts.IsVariableDeclarator(declarator))
                {
                    var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declarator);
                    if (InitializerReferencesParameter(initializer, parameter))
                        return true;
                }
            }

            return false;
        }

        bool IsAssignedToProperty(IPropertySymbol property, IParameterSymbol parameter)
        {
            if (property.DeclaringSyntaxReferences is [var syntaxRef, ..])
            {
                var declarator = syntaxRef.GetSyntax(cancellationToken);
                if (syntaxFacts.IsPropertyDeclaration(declarator))
                {
                    var initializer = syntaxFacts.GetInitializerOfPropertyDeclaration(declarator);
                    if (InitializerReferencesParameter(initializer, parameter))
                        return true;
                }
            }

            return false;
        }

        bool InitializerReferencesParameter(SyntaxNode? initializer, IParameterSymbol parameter)
        {
            if (initializer != null)
            {
                var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                value = syntaxFacts.WalkDownParentheses(value);

                if (syntaxFacts.IsIdentifierName(value))
                {
                    syntaxFacts.GetNameAndArityOfSimpleName(value, out var name, out _);
                    if (name == parameter.Name)
                        return true;
                }
            }

            return false;
        }
    }

    public static bool IsLessAccessibleThan(ISymbol? first, INamedTypeSymbol second)
    {
        if (first is null)
        {
            return false;
        }

        if (first.DeclaredAccessibility <= Accessibility.NotApplicable ||
            second.DeclaredAccessibility <= Accessibility.NotApplicable)
        {
            return false;
        }

        if (first.DeclaredAccessibility < second.DeclaredAccessibility)
        {
            return true;
        }

        switch (first)
        {
            case IPropertySymbol propertySymbol:
                if (IsTypeLessAccessibleThanOtherType(propertySymbol.Type, second, []))
                {
                    return true;
                }

                if (IsLessAccessibleThan(propertySymbol.GetMethod, second))
                {
                    return true;
                }

                if (IsLessAccessibleThan(propertySymbol.SetMethod, second))
                {
                    return true;
                }

                return false;

            case IMethodSymbol methodSymbol:
                if (IsTypeLessAccessibleThanOtherType(methodSymbol.ReturnType, second, []))
                {
                    return true;
                }

                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (IsTypeLessAccessibleThanOtherType(parameter.Type, second, []))
                    {
                        return true;
                    }
                }

                foreach (var typeArg in methodSymbol.TypeArguments)
                {
                    if (IsTypeLessAccessibleThanOtherType(typeArg, second, []))
                    {
                        return true;
                    }
                }

                return false;

            case IEventSymbol eventSymbol:
                return IsTypeLessAccessibleThanOtherType(eventSymbol.Type, second, []);

            default:
                return false;
        }
    }

    private static bool IsTypeLessAccessibleThanOtherType(ITypeSymbol? first, INamedTypeSymbol second, HashSet<ITypeSymbol> alreadyCheckingTypes)
    {
        if (first is null)
        {
            return false;
        }

        alreadyCheckingTypes.Add(first);

        if (first is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraint in typeParameter.ConstraintTypes)
            {
                if (alreadyCheckingTypes.Contains(constraint))
                {
                    continue;
                }

                if (IsTypeLessAccessibleThanOtherType(constraint, second, alreadyCheckingTypes))
                {
                    return true;
                }
            }
        }

        if (first.DeclaredAccessibility <= Accessibility.NotApplicable ||
            second.DeclaredAccessibility <= Accessibility.NotApplicable)
        {
            return false;
        }

        if (first.DeclaredAccessibility < second.DeclaredAccessibility)
        {
            return true;
        }

        if (first is INamedTypeSymbol namedType)
        {
            foreach (var genericParam in namedType.TypeArguments)
            {
                if (alreadyCheckingTypes.Contains(genericParam))
                {
                    continue;
                }

                if (IsTypeLessAccessibleThanOtherType(genericParam, second, alreadyCheckingTypes))
                {
                    return true;
                }
            }
        }

        if (IsTypeLessAccessibleThanOtherType(first.ContainingType, second, alreadyCheckingTypes))
        {
            return true;
        }

        return false;
    }
}
