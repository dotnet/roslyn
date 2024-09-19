// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IPropertySymbolExtensions
{
    public static IPropertySymbol RenameParameters(this IPropertySymbol property, ImmutableArray<string> parameterNames)
    {
        var parameterList = property.Parameters;
        if (parameterList.Select(p => p.Name).SequenceEqual(parameterNames))
        {
            return property;
        }

        var parameters = parameterList.RenameParameters(parameterNames);

        return CodeGenerationSymbolFactory.CreatePropertySymbol(
            property.ContainingType,
            property.GetAttributes(),
            property.DeclaredAccessibility,
            property.GetSymbolModifiers(),
            property.Type,
            property.RefKind,
            property.ExplicitInterfaceImplementations,
            property.Name,
            parameters,
            property.GetMethod,
            property.SetMethod,
            property.IsIndexer);
    }

    public static IPropertySymbol RemoveInaccessibleAttributesAndAttributesOfTypes(
        this IPropertySymbol property, ISymbol accessibleWithin, params INamedTypeSymbol[] attributesToRemove)
    {
        // Many static predicates use the same state argument in this method
        var arg = (attributesToRemove, accessibleWithin);

        var someParameterHasAttribute = property.Parameters
            .Any(static (p, arg) => p.GetAttributes().Any(ShouldRemoveAttribute, arg), arg);
        if (!someParameterHasAttribute)
            return property;

        return CodeGenerationSymbolFactory.CreatePropertySymbol(
            property.ContainingType,
            property.GetAttributes(),
            property.DeclaredAccessibility,
            property.GetSymbolModifiers(),
            property.Type,
            property.RefKind,
            property.ExplicitInterfaceImplementations,
            property.Name,
            property.Parameters.SelectAsArray(static (p, arg) =>
                CodeGenerationSymbolFactory.CreateParameterSymbol(
                    p.GetAttributes().WhereAsArray(static (a, arg) => !ShouldRemoveAttribute(a, arg), arg),
                    p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                    p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null), arg),
            property.GetMethod,
            property.SetMethod,
            property.IsIndexer);

        static bool ShouldRemoveAttribute(AttributeData a, (INamedTypeSymbol[] attributesToRemove, ISymbol accessibleWithin) arg)
            => arg.attributesToRemove.Any(attr => attr.Equals(a.AttributeClass)) ||
            a.AttributeClass?.IsAccessibleWithin(arg.accessibleWithin) == false;
    }

    public static bool IsWritableInConstructor(this IPropertySymbol property)
        => property.SetMethod != null || ContainsBackingField(property);

    private static bool ContainsBackingField(IPropertySymbol property)
        => property.GetBackingFieldIfAny() != null;
}
