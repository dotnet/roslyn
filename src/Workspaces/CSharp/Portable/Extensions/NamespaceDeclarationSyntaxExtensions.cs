// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class NamespaceDeclarationSyntaxExtensions
    {
        public static NamespaceDeclarationSyntax AddUsingDirectives(
            this NamespaceDeclarationSyntax namespaceDeclaration,
            IList<UsingDirectiveSyntax> usingDirectives,
            bool placeSystemNamespaceFirst,
            params SyntaxAnnotation[] annotations)
        {
            if (!usingDirectives.Any())
            {
                return namespaceDeclaration;
            }

            var specialCaseSystem = placeSystemNamespaceFirst;
            var comparer = specialCaseSystem
                ? UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance
                : UsingsAndExternAliasesDirectiveComparer.NormalInstance;

            var usings = new List<UsingDirectiveSyntax>();
            usings.AddRange(namespaceDeclaration.Usings);
            usings.AddRange(usingDirectives);

            if (namespaceDeclaration.Usings.IsSorted(comparer))
            {
                usings.Sort(comparer);
            }

            usings = usings.Select(u => u.WithAdditionalAnnotations(annotations)).ToList();
            var newNamespace = namespaceDeclaration.WithUsings(usings.ToSyntaxList());

            return newNamespace;
        }
    }
}
