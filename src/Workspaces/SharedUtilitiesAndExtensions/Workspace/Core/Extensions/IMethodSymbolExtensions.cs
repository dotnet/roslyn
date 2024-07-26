// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IMethodSymbolExtensions
{
    public static IMethodSymbol EnsureNonConflictingNames(
        this IMethodSymbol method, INamedTypeSymbol containingType, ISyntaxFactsService syntaxFacts)
    {
        // The method's type parameters may conflict with the type parameters in the type
        // we're generating into.  In that case, rename them.
        var parameterNames = NameGenerator.EnsureUniqueness(
            method.Parameters.SelectAsArray(p => p.Name), isCaseSensitive: syntaxFacts.IsCaseSensitive);

        var outerTypeParameterNames =
            containingType.GetAllTypeParameters()
                          .Select(tp => tp.Name)
                          .Concat(method.Name)
                          .Concat(containingType.Name);

        var unusableNames = parameterNames.Concat(outerTypeParameterNames).ToSet(
            syntaxFacts.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        var newTypeParameterNames = NameGenerator.EnsureUniqueness(
            method.TypeParameters.SelectAsArray(tp => tp.Name),
            n => !unusableNames.Contains(n));

        var updatedMethod = method.RenameTypeParameters(newTypeParameterNames);
        return updatedMethod.RenameParameters(parameterNames);
    }

    public static IMethodSymbol RenameTypeParameters(this IMethodSymbol method, ImmutableArray<string> newNames)
    {
        if (method.TypeParameters.Select(t => t.Name).SequenceEqual(newNames))
        {
            return method;
        }

        var typeGenerator = new TypeGenerator();
        var updatedTypeParameters = RenameTypeParameters(
            method.TypeParameters, newNames, typeGenerator);

        var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        for (var i = 0; i < method.TypeParameters.Length; i++)
        {
            mapping[method.TypeParameters[i]] = updatedTypeParameters[i];
        }

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            method.ContainingType,
            method.GetAttributes(),
            method.DeclaredAccessibility,
            method.GetSymbolModifiers(),
            method.ReturnType.SubstituteTypes(mapping, typeGenerator),
            method.RefKind,
            method.ExplicitInterfaceImplementations,
            method.Name,
            updatedTypeParameters,
            method.Parameters.SelectAsArray(p =>
                CodeGenerationSymbolFactory.CreateParameterSymbol(p.GetAttributes(), p.RefKind, p.IsParams, p.Type.SubstituteTypes(mapping, typeGenerator), p.Name, p.IsOptional,
                    p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)));
    }

    public static IMethodSymbol RenameParameters(
        this IMethodSymbol method, ImmutableArray<string> parameterNames)
    {
        var parameterList = method.Parameters;
        if (parameterList.Select(p => p.Name).SequenceEqual(parameterNames))
        {
            return method;
        }

        var parameters = parameterList.RenameParameters(parameterNames);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            method.ContainingType,
            method.GetAttributes(),
            method.DeclaredAccessibility,
            method.GetSymbolModifiers(),
            method.ReturnType,
            method.RefKind,
            method.ExplicitInterfaceImplementations,
            method.Name,
            method.TypeParameters,
            parameters);
    }

    private static ImmutableArray<ITypeParameterSymbol> RenameTypeParameters(
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        ImmutableArray<string> newNames,
        ITypeGenerator typeGenerator)
    {
        // We generate the type parameter in two passes.  The first creates the new type
        // parameter.  The second updates the constraints to point at this new type parameter.
        var newTypeParameters = new List<CodeGenerationTypeParameterSymbol>();

        var mapping = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        for (var i = 0; i < typeParameters.Length; i++)
        {
            var typeParameter = typeParameters[i];

            var newTypeParameter = new CodeGenerationTypeParameterSymbol(
                typeParameter.ContainingType,
                typeParameter.GetAttributes(),
                typeParameter.Variance,
                newNames[i],
                typeParameter.NullableAnnotation,
                typeParameter.ConstraintTypes,
                typeParameter.HasConstructorConstraint,
                typeParameter.HasReferenceTypeConstraint,
                typeParameter.HasValueTypeConstraint,
                typeParameter.HasUnmanagedTypeConstraint,
                typeParameter.HasNotNullConstraint,
                typeParameter.AllowsRefLikeType,
                typeParameter.Ordinal);

            newTypeParameters.Add(newTypeParameter);
            mapping[typeParameter] = newTypeParameter;
        }

        // Now we update the constraints.
        foreach (var newTypeParameter in newTypeParameters)
        {
            newTypeParameter.ConstraintTypes = ImmutableArray.CreateRange(newTypeParameter.ConstraintTypes, t => t.SubstituteTypes(mapping, typeGenerator));
        }

        return newTypeParameters.Cast<ITypeParameterSymbol>().ToImmutableArray();
    }

    public static IMethodSymbol RemoveInaccessibleAttributesAndAttributesOfTypes(
        this IMethodSymbol method, ISymbol accessibleWithin,
        params INamedTypeSymbol[] removeAttributeTypes)
    {
        // Many static predicates use the same state argument in this method
        var arg = (removeAttributeTypes, accessibleWithin);

        var methodHasAttribute = method.GetAttributes().Any(shouldRemoveAttribute, arg);

        var someParameterHasAttribute = method.Parameters
            .Any(static (m, arg) => m.GetAttributes().Any(shouldRemoveAttribute, arg), arg);

        var returnTypeHasAttribute = method.GetReturnTypeAttributes().Any(shouldRemoveAttribute, arg);

        if (!methodHasAttribute && !someParameterHasAttribute && !returnTypeHasAttribute)
        {
            return method;
        }

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            method,
            containingType: method.ContainingType,
            explicitInterfaceImplementations: method.ExplicitInterfaceImplementations,
            attributes: method.GetAttributes().WhereAsArray(static (a, arg) => !shouldRemoveAttribute(a, arg), arg),
            parameters: method.Parameters.SelectAsArray(static (p, arg) =>
                CodeGenerationSymbolFactory.CreateParameterSymbol(
                    p.GetAttributes().WhereAsArray(static (a, arg) => !shouldRemoveAttribute(a, arg), arg),
                    p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                    p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null), arg),
            returnTypeAttributes: method.GetReturnTypeAttributes().WhereAsArray(static (a, arg) => !shouldRemoveAttribute(a, arg), arg));

        static bool shouldRemoveAttribute(AttributeData a, (INamedTypeSymbol[] removeAttributeTypes, ISymbol accessibleWithin) arg)
            => arg.removeAttributeTypes.Any(attr => attr.Equals(a.AttributeClass)) ||
            a.AttributeClass?.IsAccessibleWithin(arg.accessibleWithin) == false;
    }
}
