// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamedTypeSymbolExtensions
    {
        public static INamespaceOrTypeSymbol GenerateRootNamespaceOrType(this INamedTypeSymbol namedType, string[] containers)
        {
            INamespaceOrTypeSymbol currentSymbol = namedType;

            for (var i = containers.Length - 1; i >= 0; i--)
            {
                currentSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol(containers[i], members: new[] { currentSymbol });
            }

            return currentSymbol;
        }

        public static bool IsNotNullAttribute([NotNullWhen(true)] this INamedTypeSymbol? namedType)
        {
            return namedType?.Name == nameof(NotNullAttribute) &&
                namedType.ContainingNamespace.IsSystemDiagnosticsCodeAnalysis();
        }

        public static bool IsDisallowNullAttribute([NotNullWhen(true)] this INamedTypeSymbol? namedType)
        {
            return namedType?.Name == nameof(DisallowNullAttribute) &&
                namedType.ContainingNamespace.IsSystemDiagnosticsCodeAnalysis();
        }

        public static bool IsAllowNullAttribute([NotNullWhen(true)] this INamedTypeSymbol? namedType)
        {
            return namedType?.Name == nameof(AllowNullAttribute) &&
                namedType.ContainingNamespace.IsSystemDiagnosticsCodeAnalysis();
        }

        public static bool IsMaybeNullAttribute([NotNullWhen(true)] this INamedTypeSymbol? namedType)
        {
            return namedType?.Name == nameof(MaybeNullAttribute) &&
                namedType.ContainingNamespace.IsSystemDiagnosticsCodeAnalysis();
        }
    }
}
