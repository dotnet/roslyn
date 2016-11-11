// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImports
{
    internal abstract class AbstractAddImportsService<TCompilationUnitSyntax, TNamespaceDeclarationSyntax, TUsingOrAliasSyntax, TExternSyntax>
        : IAddImportsService
        where TCompilationUnitSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TUsingOrAliasSyntax : SyntaxNode
        where TExternSyntax : SyntaxNode
    {
        protected AbstractAddImportsService()
        {
        }

        protected abstract SyntaxNode GetAlias(TUsingOrAliasSyntax usingOrAlias);

        private bool IsUsing(TUsingOrAliasSyntax usingOrAlias) => GetAlias(usingOrAlias) == null;
        private bool IsAlias(TUsingOrAliasSyntax usingOrAlias) => GetAlias(usingOrAlias) != null;
        private bool HasAliases(SyntaxNode node) => GetUsingsAndAliases(node).Any(IsAlias);
        private bool HasUsings(SyntaxNode node) => GetUsingsAndAliases(node).Any(IsUsing);
        private bool HasExterns(SyntaxNode node) => GetExterns(node).Any();
        private bool HasAnyImports(SyntaxNode node) => GetUsingsAndAliases(node).Any() || GetExterns(node).Any();

        public bool HasExistingImport(SyntaxNode root, SyntaxNode contextLocation, SyntaxNode import)
        {
            contextLocation = contextLocation ?? root;

            var applicableContainer = GetFirstApplicableContainer(contextLocation);
            var containers = applicableContainer.GetAncestorsOrThis<SyntaxNode>().ToArray();

            foreach (var node in containers)
            {
                if (GetUsingsAndAliases(node).Any(u => u.IsEquivalentTo(import, topLevel: false)))
                {
                    return true;
                }

                if (GetExterns(node).Any(u => u.IsEquivalentTo(import, topLevel: false)))
                {
                    return true;
                }
            }

            return false;
        }

        public SyntaxNode GetImportContainer(SyntaxNode root, SyntaxNode contextLocation, SyntaxNode import)
        {
            contextLocation = contextLocation ?? root;
            GetContainers(root, contextLocation,
                out var externContainer, out var usingContainer, out var aliasContainer);

            switch (import)
            {
                case TExternSyntax e: return externContainer;
                case TUsingOrAliasSyntax u: return IsAlias(u) ? aliasContainer : usingContainer;
            }

            throw new InvalidOperationException();
        }

        public SyntaxNode AddImports(
            SyntaxNode root,
            SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports,
            bool placeSystemNamespaceFirst)
        {
            contextLocation = contextLocation ?? root;
            GetContainers(root, contextLocation,
                out var externContainer, out var usingContainer, out var aliasContainer);

            var filteredImports = newImports.Where(i => !HasExistingImport(root, contextLocation, i)).ToArray();

            var externAliases = filteredImports.OfType<TExternSyntax>().ToArray();
            var usingDirectives = filteredImports.OfType<TUsingOrAliasSyntax>().Where(IsUsing).ToArray();
            var aliasDirectives = filteredImports.OfType<TUsingOrAliasSyntax>().Where(IsAlias).ToArray();

            var newRoot = Rewrite(
                externAliases, usingDirectives, aliasDirectives,
                externContainer, usingContainer, aliasContainer,
                placeSystemNamespaceFirst, root);

            return newRoot;
        }

        protected abstract SyntaxNode Rewrite(
            TExternSyntax[] externAliases, TUsingOrAliasSyntax[] usingDirectives, TUsingOrAliasSyntax[] aliasDirectives, 
            SyntaxNode externContainer, SyntaxNode usingContainer, SyntaxNode aliasContainer, 
            bool placeSystemNamespaceFirst, SyntaxNode root);

        private void GetContainers(SyntaxNode root, SyntaxNode contextLocation, out SyntaxNode externContainer, out SyntaxNode usingContainer, out SyntaxNode aliasContainer)
        {
            var applicableContainer = GetFirstApplicableContainer(contextLocation);
            var contextSpine = applicableContainer.GetAncestorsOrThis<SyntaxNode>().ToImmutableArray();

            // The node we'll add to if we can't find a specific namespace with imports of 
            // the type we're trying to add.  This will be the closest namespace with any
            // imports in it, or the root if there are no such namespaces.
            var fallbackNode = contextSpine.FirstOrDefault(HasAnyImports) ?? root;

            // The specific container to add each type of import to.  We look for a container
            // that already has an import of the same type as the node we want to add to.
            // If we can find one, we add to that container.  If not, we call back to the 
            // innermost node with any imports.
            externContainer = contextSpine.FirstOrDefault(HasExterns) ?? fallbackNode;
            usingContainer = contextSpine.FirstOrDefault(HasUsings) ?? fallbackNode;
            aliasContainer = contextSpine.FirstOrDefault(HasAliases) ?? fallbackNode;
        }

        protected abstract SyntaxList<TUsingOrAliasSyntax> GetUsingsAndAliases(SyntaxNode node);

        protected abstract SyntaxList<TExternSyntax> GetExterns(SyntaxNode node);

        private static SyntaxNode GetFirstApplicableContainer(SyntaxNode contextNode)
        {
            var usingDirective = contextNode.GetAncestor<TUsingOrAliasSyntax>();
            if (usingDirective != null)
            {
                contextNode = usingDirective.Parent;
            }

            return contextNode.GetAncestor<TNamespaceDeclarationSyntax>() ??
                   (SyntaxNode)contextNode.GetAncestor<TCompilationUnitSyntax>();
        }
    }
}