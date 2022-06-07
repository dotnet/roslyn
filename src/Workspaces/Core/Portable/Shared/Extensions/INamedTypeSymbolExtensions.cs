// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
