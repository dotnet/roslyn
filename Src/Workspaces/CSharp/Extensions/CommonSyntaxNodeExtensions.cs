// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class CommonSyntaxNodeExtensions
    {
        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node != null && node.Parent.IsKind(kind);
        }

        public static bool MatchesKind(this SyntaxNode node, SyntaxKind kind)
        {
            if (node == null)
            {
                return false;
            }

            return node.IsKind(kind);
        }

        public static bool MatchesKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2)
        {
            if (node == null)
            {
                return false;
            }

            return node.IsKind(kind1) || node.IsKind(kind2);
        }

        public static bool MatchesKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            if (node == null)
            {
                return false;
            }

            return kinds.Contains(node.CSharpKind());
        }

        /// <summary>
        /// Returns the list of using directives that affect 'node'.  The list will be returned in
        /// top down order.  
        /// </summary>
        public static IEnumerable<UsingDirectiveSyntax> GetEnclosingUsingDirectives(this SyntaxNode node)
        {
            return node.GetAncestorOrThis<CompilationUnitSyntax>().Usings
                       .Concat(node.GetAncestorsOrThis<NamespaceDeclarationSyntax>()
                                   .Reverse()
                                   .SelectMany(n => n.Usings));
        }

        public static bool IsUnsafeContext(this SyntaxNode node)
        {
            if (node.GetAncestor<UnsafeStatementSyntax>() != null)
            {
                return true;
            }

            return node.GetAncestors<MemberDeclarationSyntax>().Any(
                m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
        }

        public static bool IsInStaticContext(this SyntaxNode node)
        {
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null)
            {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null)
            {
                return false;
            }

            switch (memberDeclaration.CSharpKind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.FieldDeclaration:
                    // Inside a field one can only access static members of a type.
                    return true;

                case SyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null)
            {
                return false;
            }

            // any other location is considered static
            return true;
        }

        public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
        {
            return contextNode.GetAncestorsOrThis<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
        }
    }
}