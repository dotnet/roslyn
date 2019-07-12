// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Editing
{
    [ExportLanguageService(typeof(ImportAdderService), LanguageNames.CSharp), Shared]
    internal class CSharpImportAdder : ImportAdderService
    {
        [ImportingConstructor]
        public CSharpImportAdder()
        {
        }

        protected override INamespaceSymbol GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model)
        {
            switch (node)
            {
                case QualifiedNameSyntax name:
                    return GetExplicitNamespaceSymbol(name, name.Left, model);
                case MemberAccessExpressionSyntax memberAccess:
                    return GetExplicitNamespaceSymbol(memberAccess, memberAccess.Expression, model);
            }

            return null;
        }

        protected override INamespaceSymbol GetContainedNamespace(SyntaxNode node, SemanticModel model)
        {
            var namespaceSyntax = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            if (namespaceSyntax is null)
                return null;

            return model.GetDeclaredSymbol(namespaceSyntax);
        }

        protected override SyntaxNode MakeSafeToAddNamespaces(SyntaxNode root, IEnumerable<INamespaceOrTypeSymbol> namespaceMembers, IEnumerable<IMethodSymbol> extensionMethods, SemanticModel model, Workspace workspace, CancellationToken cancellationToken)
        {
            var rewriter = new Rewriter(namespaceMembers, extensionMethods, model, workspace, cancellationToken);

            return rewriter.Visit(root);
        }

        private INamespaceSymbol GetExplicitNamespaceSymbol(ExpressionSyntax fullName, ExpressionSyntax namespacePart, SemanticModel model)
        {
            // name must refer to something that is not a namespace, but be qualified with a namespace.
            var symbol = model.GetSymbolInfo(fullName).Symbol;
            var nsSymbol = model.GetSymbolInfo(namespacePart).Symbol as INamespaceSymbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace && nsSymbol != null)
            {
                // use the symbols containing namespace, and not the potentially less than fully qualified namespace in the full name expression.
                var ns = symbol.ContainingNamespace;
                if (ns != null)
                {
                    return model.Compilation.GetCompilationNamespace(ns);
                }
            }

            return null;
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private Workspace _workspace;
            private CancellationToken _cancellationToken;
            private readonly SemanticModel _model;
            private readonly HashSet<string> _namespaceMembers;
            private readonly HashSet<string> _extensionMethods;

            public Rewriter(
                IEnumerable<INamespaceOrTypeSymbol> namespaceMembers,
                IEnumerable<IMethodSymbol> extensionMethods,
                SemanticModel model,
                Workspace workspace,
                CancellationToken cancellationToken)
            {
                _model = model;
                _workspace = workspace;
                _cancellationToken = cancellationToken;
                _namespaceMembers = new HashSet<string>(namespaceMembers.Select(x => x.Name));
                _extensionMethods = new HashSet<string>(extensionMethods.Select(x => x.Name));
            }

            public override bool VisitIntoStructuredTrivia => true;

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                //Only visit leading trivia, as we only care about xml doc comments
                var leadingTrivia = VisitList(node.GetLeadingTrivia());

                if (_namespaceMembers.Contains(node.Identifier.Text))
                {
                    var expanded = Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    return expanded.WithLeadingTrivia(leadingTrivia);
                }

                return node.WithLeadingTrivia(leadingTrivia);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                //Only visit leading trivia, as we only care about xml doc comments
                var leadingTrivia = VisitList(node.GetLeadingTrivia());

                if (_namespaceMembers.Contains(node.Identifier.Text))
                {
                    // no need to visit type argument list as simplifier will expand everything
                    var expanded = Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    return expanded.WithLeadingTrivia(leadingTrivia);
                }

                var typeArgumentList = (TypeArgumentListSyntax)base.Visit(node.TypeArgumentList);
                return node.Update(node.Identifier.WithLeadingTrivia(leadingTrivia), typeArgumentList);
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                var left = (NameSyntax)base.Visit(node.Left);
                var right = node.Right;
                if (right is GenericNameSyntax genericName)
                {
                    var typeArgumentList = (TypeArgumentListSyntax)base.Visit(genericName.TypeArgumentList);
                    right = genericName.Update(genericName.Identifier, typeArgumentList);
                }
                return node.Update(left, node.DotToken, right);
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                // No need to visit trivia, as we only care about xml doc comments
                if (node.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (_extensionMethods.Contains(memberAccess.Name.Identifier.Text))
                    {
                        // no need to visit this as simplifier will expand everything
                        return Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    }
                }

                return base.VisitInvocationExpression(node);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node);

                if (_extensionMethods.Contains(node.Name.Identifier.Text))
                {
                    // we will not visit this if the parent node is expanded, since we just expand the entire parent node.
                    // therefore, since this is visited, we haven't expanded, and so we should warn
                    node = node.WithAdditionalAnnotations(WarningAnnotation.Create(
                        "Adding imports will bring an extension method into scope with the same name as " + node.Name.Identifier.Text));
                }

                return node;
            }
        }
    }
}
