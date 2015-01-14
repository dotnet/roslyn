// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            this IPropertySymbol property, INamedTypeSymbol attributeType)
        {
            if (attributeType == null)
            {
                return property;
            }

            var someParameterHasAttribute = property.Parameters
                .Any(p => p.GetAttributes().Any(a => a.AttributeClass.Equals(attributeType)));
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
                        p.GetAttributes().Where(a => !a.AttributeClass.Equals(attributeType)).ToList(),
                        p.RefKind, p.IsParams, p.Type, p.Name, p.IsOptional,
                        p.HasExplicitDefaultValue, p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null)).ToList(),
                property.GetMethod,
                property.SetMethod,
                property.IsIndexer);
        }
    }
}
