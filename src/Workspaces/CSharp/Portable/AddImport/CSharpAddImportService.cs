// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AddImport
{
    [ExportLanguageService(typeof(IAddImportService), LanguageNames.CSharp), Shared]
    internal class CSharpAddImportService : IAddImportService
    {
        private static Func<UsingDirectiveSyntax, bool> s_isUsing = u => u.Alias == null;
        private static Func<UsingDirectiveSyntax, bool> s_isAlias = u => u.Alias != null;

        public SyntaxNode AddImports(
            SyntaxNode root,
            SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports,
            bool placeSystemNamespaceFirst)
        {
            contextLocation = contextLocation ?? root;

            var applicableContainer = GetFirstApplicableContainer(contextLocation);
            var contextSpine = applicableContainer.GetAncestorsOrThis<SyntaxNode>().ToImmutableArray();

            // The node we'll add to if we can't find a specific namespace with imports of 
            // the type we're trying to add.  This will be the closest namespace with any
            // imports in it, or the root if there are no such namespaces.
            var fallbackNode = contextSpine.FirstOrDefault(s_hasAnyImports) ?? root;

            // The specific container to add each type of import to.  We look for a container
            // that already has an import of the same type as the node we want to add to.
            // If we can find one, we add to that container.  If not, we call back to the 
            // innermost node with any imports.
            var externContainer = contextSpine.FirstOrDefault(s_hasExterns) ?? fallbackNode;
            var usingContainer = contextSpine.FirstOrDefault(s_hasUsings) ?? fallbackNode;
            var aliasContainer = contextSpine.FirstOrDefault(s_hasAliases) ?? fallbackNode;

            var externAliases = newImports.OfType<ExternAliasDirectiveSyntax>().ToArray();
            var usingDirectives = newImports.OfType<UsingDirectiveSyntax>().Where(u => u.Alias == null).ToArray();
            var aliasDirectives = newImports.OfType<UsingDirectiveSyntax>().Where(u => u.Alias != null).ToArray();

            var rewriter = new Rewriter(
                externAliases, usingDirectives, aliasDirectives,
                externContainer, usingContainer, aliasContainer,
                placeSystemNamespaceFirst);
            var newRoot = rewriter.Visit(root);

            return newRoot;
        }

        private Func<SyntaxNode, bool> s_hasAnyImports =
            n => GetUsings(n).Any() || GetExterns(n).Any();

        private static SyntaxList<UsingDirectiveSyntax> GetUsings(SyntaxNode node)
        {
            switch (node)
            {
                case CompilationUnitSyntax c: return c.Usings;
                case NamespaceDeclarationSyntax n: return n.Usings;
                default: return default(SyntaxList<UsingDirectiveSyntax>);
            }
        }

        private static SyntaxList<ExternAliasDirectiveSyntax> GetExterns(SyntaxNode node)
        {
            switch (node)
            {
                case CompilationUnitSyntax c: return c.Externs;
                case NamespaceDeclarationSyntax n: return n.Externs;
                default: return default(SyntaxList<ExternAliasDirectiveSyntax>);
            }
        }

        private static Func<SyntaxNode, bool> s_hasAliases =
            n => GetUsings(n).Any(s_isAlias);

        private static Func<SyntaxNode, bool> s_hasUsings =
            n => GetUsings(n).Any(s_isUsing);

        private static Func<SyntaxNode, bool> s_hasExterns =
            n => GetExterns(n).Any();

        private static SyntaxNode GetFirstApplicableContainer(SyntaxNode contextNode)
        {
            var usingDirective = contextNode.GetAncestor<UsingDirectiveSyntax>();
            if (usingDirective != null)
            {
                contextNode = usingDirective.Parent;
            }

            return contextNode.GetAncestor<NamespaceDeclarationSyntax>() ??
                   (SyntaxNode)contextNode.GetAncestor<CompilationUnitSyntax>();
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly bool _placeSystemNamespaceFirst;
            private readonly SyntaxNode _externContainer;
            private readonly SyntaxNode _usingContainer;
            private readonly SyntaxNode _aliasContainer;

            private readonly UsingDirectiveSyntax[] _aliasDirectives;
            private readonly ExternAliasDirectiveSyntax[] _externAliases;
            private readonly UsingDirectiveSyntax[] _usingDirectives;

            public Rewriter(
                ExternAliasDirectiveSyntax[] externAliases, 
                UsingDirectiveSyntax[] usingDirectives, 
                UsingDirectiveSyntax[] aliasDirectives,
                SyntaxNode externContainer,
                SyntaxNode usingContainer,
                SyntaxNode aliasContainer,
                bool placeSystemNamespaceFirst)
            {
                _externAliases = externAliases;
                _usingDirectives = usingDirectives;
                _aliasDirectives = aliasDirectives;
                _externContainer = externContainer;
                _usingContainer = usingContainer;
                _aliasContainer = aliasContainer;
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst;
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

                if (node == _aliasContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _usingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _externContainer)
                {
                    rewritten = rewritten.AddExterns(_externAliases);
                }

                return rewritten;
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

                if (node == _aliasContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _usingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _externContainer)
                {
                    rewritten = rewritten.AddExterns(_externAliases);
                }

                return rewritten;
            }
        }
    }
}