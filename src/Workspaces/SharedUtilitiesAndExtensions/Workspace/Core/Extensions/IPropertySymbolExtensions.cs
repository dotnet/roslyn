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
}
