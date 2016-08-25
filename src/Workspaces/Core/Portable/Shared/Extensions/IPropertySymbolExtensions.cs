// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IPropertySymbolExtensions
    {
        public static IPropertySymbol RenameParameters(this IPropertySymbol property, IList<string> parameterNames)
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
                property.ExplicitInterfaceImplementations.FirstOrDefault(),
                property.Name,
                parameters,
                property.GetMethod,
                property.SetMethod,
                property.IsIndexer);
        }

        public static IPropertySymbol RemoveAttributeFromParameters(
            this IPropertySymbol property, INamedTypeSymbol[] attributesToRemove)
        {
            if (attributesToRemove == null)
            {
                return property;
            }

            Func<AttributeData, bool> shouldRemoveAttribute = a =>
                attributesToRemove.Where(attr => attr != null).Any(attr => attr.Equals(a.AttributeClass));

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
                property.ExplicitInterfaceImplementations.FirstOrDefault(),
                property.Name,
                property.Parameters.Select(p =>
                    CodeGenerationSymbolFactory.CreateParameterSymbol(
                        p.GetAttributes().Where(a => !shouldRemoveAttribute(a)).ToList(),
                        p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)).ToList(),
                property.GetMethod,
                property.SetMethod,
                property.IsIndexer);
        }
    }
}
