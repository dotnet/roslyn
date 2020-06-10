// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IPropertySymbolExtensions
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
            var someParameterHasAttribute = property.Parameters
                .Any(p => p.GetAttributes().Any(shouldRemoveAttribute));
            if (!someParameterHasAttribute)
            {
                return property;
            }

            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                property.ContainingType,
                property.GetAttributes(),
                property.DeclaredAccessibility,
                property.GetSymbolModifiers(),
                property.Type,
                property.RefKind,
                property.ExplicitInterfaceImplementations,
                property.Name,
                property.Parameters.SelectAsArray(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(
                        p.GetAttributes().WhereAsArray(a => !shouldRemoveAttribute(a)),
                        p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)),
                property.GetMethod,
                property.SetMethod,
                property.IsIndexer);

            bool shouldRemoveAttribute(AttributeData a) =>
                attributesToRemove.Any(attr => attr.Equals(a.AttributeClass)) ||
                a.AttributeClass?.IsAccessibleWithin(accessibleWithin) == false;
        }

        public static bool IsWritableInConstructor(this IPropertySymbol property)
            => (property.SetMethod != null || ContainsBackingField(property));

        public static IFieldSymbol? GetBackingFieldIfAny(this IPropertySymbol property)
            => property.ContainingType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => property.Equals(f.AssociatedSymbol));

        private static bool ContainsBackingField(IPropertySymbol property)
            => property.GetBackingFieldIfAny() != null;
    }
}
