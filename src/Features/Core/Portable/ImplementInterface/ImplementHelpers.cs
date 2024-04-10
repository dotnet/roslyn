// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        using var _1 = ArrayBuilder<ISymbol>.GetInstance(fields.Length + properties.Length + parameters.Length, out var result);
        result.AddRange(fields);
        result.AddRange(properties);
        result.AddRange(parameters);
        return result.ToImmutableAndClear();

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

            return result.ToImmutable();
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
}
