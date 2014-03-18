// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Text;

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
    }
}