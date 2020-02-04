// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        public static NameSyntax GenerateNameSyntax(
            this INamespaceOrTypeSymbol symbol, bool allowVar = true)
        {
            return (NameSyntax)GenerateTypeSyntax(symbol, nameSyntax: true, allowVar: allowVar);
        }

        public static TypeSyntax GenerateTypeSyntax(
            this INamespaceOrTypeSymbol symbol, bool allowVar = true)
        {
            return GenerateTypeSyntax(symbol, nameSyntax: false, allowVar: allowVar);
        }

        private static TypeSyntax GenerateTypeSyntax(
            INamespaceOrTypeSymbol symbol, bool nameSyntax, bool allowVar = true)
        {
            if (symbol is ITypeSymbol type && type.ContainsAnonymousType())
            {
                // something with an anonymous type can only be represented with 'var', regardless
                // of what the user's preferences might be.
                return SyntaxFactory.IdentifierName("var");
            }

            var syntax = symbol.Accept(TypeSyntaxGeneratorVisitor.Create(nameSyntax))
                               .WithAdditionalAnnotations(Simplifier.Annotation);

            if (!allowVar)
            {
                syntax = syntax.WithAdditionalAnnotations(DoNotAllowVarAnnotation.Annotation);
            }

            return syntax;
        }
    }
}
