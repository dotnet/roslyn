// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class BaseNamespaceDeclarationSyntaxExtensions
{
    extension<TNamespaceDeclarationSyntax>(TNamespaceDeclarationSyntax namespaceDeclaration) where TNamespaceDeclarationSyntax : BaseNamespaceDeclarationSyntax
    {
        public TNamespaceDeclarationSyntax AddUsingDirectives(
        IList<UsingDirectiveSyntax> usingDirectives,
        bool placeSystemNamespaceFirst,
        params SyntaxAnnotation[] annotations)
        {
            if (usingDirectives.Count == 0)
                return namespaceDeclaration;

            var newUsings = new List<UsingDirectiveSyntax>();
            newUsings.AddRange(namespaceDeclaration.Usings);
            newUsings.AddRange(usingDirectives);

            newUsings.SortUsingDirectives(namespaceDeclaration.Usings, placeSystemNamespaceFirst);
            newUsings = [.. newUsings.Select(u => u.WithAdditionalAnnotations(annotations))];

            var newNamespace = namespaceDeclaration.WithUsings([.. newUsings]);
            return (TNamespaceDeclarationSyntax)newNamespace;
        }
    }
}
