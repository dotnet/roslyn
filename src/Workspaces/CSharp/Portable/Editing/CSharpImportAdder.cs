// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Editing
{
    [ExportLanguageService(typeof(ImportAdderService), LanguageNames.CSharp), Shared]
    internal class CSharpImportAdder : ImportAdderService
    {
        [ImportingConstructor]
        public CSharpImportAdder()
        {
        }

        protected override INamespaceSymbol? GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model)
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

        protected override SyntaxNode MakeSafeToAddNamespaces(SyntaxNode root, IEnumerable<INamespaceOrTypeSymbol> namespaceMembers, IEnumerable<IMethodSymbol> extensionMethods, SemanticModel model, Workspace workspace, CancellationToken cancellationToken)
        {
            var rewriter = new Rewriter(namespaceMembers, extensionMethods, model, workspace, cancellationToken);

            return rewriter.Visit(root);
        }

        private INamespaceSymbol? GetExplicitNamespaceSymbol(ExpressionSyntax fullName, ExpressionSyntax namespacePart, SemanticModel model)
        {

            // name must refer to something that is not a namespace, but be qualified with a namespace.
            var symbol = model.GetSymbolInfo(fullName).Symbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace && model.GetSymbolInfo(namespacePart).Symbol is INamespaceSymbol nsSymbol)
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
            private readonly SemanticModel _model;
            private readonly Workspace _workspace;
            private readonly CancellationToken _cancellationToken;

            /// <summary>
            /// A hashset containing the short names of all namespace members 
            /// </summary>
            private readonly HashSet<string> _namespaceMembers;

            /// <summary>
            /// A hashset containing the short names of all extension methods
            /// </summary>
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
                // We only care about xml doc comments
                var leadingTrivia = CanHaveDocComments(node) ? VisitList(node.GetLeadingTrivia()) : node.GetLeadingTrivia();

                if (_namespaceMembers.Contains(node.Identifier.Text))
                {
                    var expanded = Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    return expanded.WithLeadingTrivia(leadingTrivia);
                }

                return node.WithLeadingTrivia(leadingTrivia);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                // We only care about xml doc comments
                var leadingTrivia = CanHaveDocComments(node) ? VisitList(node.GetLeadingTrivia()) : node.GetLeadingTrivia();

                if (_namespaceMembers.Contains(node.Identifier.Text))
                {
                    // No need to visit type argument list as simplifier will expand everything
                    var expanded = Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    return expanded.WithLeadingTrivia(leadingTrivia);
                }

                var typeArgumentList = (TypeArgumentListSyntax)base.Visit(node.TypeArgumentList);
                return node.Update(node.Identifier.WithLeadingTrivia(leadingTrivia), typeArgumentList);
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                var left = (NameSyntax)base.Visit(node.Left);
                // We don't recurse on the right, as if B is a member of the imported namespace, A.B is still not ambiguous
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
                        // No need to visit this as simplifier will expand everything
                        return Simplifier.Expand<SyntaxNode>(node, _model, _workspace, cancellationToken: _cancellationToken);
                    }
                }

                return base.VisitInvocationExpression(node) ?? throw ExceptionUtilities.Unreachable;
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                node = (MemberAccessExpressionSyntax)(base.VisitMemberAccessExpression(node) ?? throw ExceptionUtilities.Unreachable);

                if (_extensionMethods.Contains(node.Name.Identifier.Text))
                {
                    // If an extension method is used as a delegate rather than invoked directly,
                    // there is no semantically valid transformation that will fully qualify the extension method. 
                    // For example `Func<int> f = x.M;` is not the same as Func<int> f = () => Extensions.M(x);`
                    // since one captures x by value, and the other by reference.
                    //
                    // We will not visit this node if the parent node was an InvocationExpression, 
                    // since we would have expanded the parent node entirely, rather than visiting it.
                    // Therefore it's possible that this is an extension method being used as a delegate so we warn.
                    node = node.WithAdditionalAnnotations(WarningAnnotation.Create(string.Format(
                        WorkspacesResources.Warning_adding_imports_will_bring_an_extension_method_into_scope_with_the_same_name_as_member_access,
                        node.Name.Identifier.Text)));
                }

                return node;
            }

            private bool CanHaveDocComments(NameSyntax node)
            {
                // a node can only have doc comments in its leading trivia if it's the first node in a member declaration syntax.

                SyntaxNode current = node;
                while (current.Parent != null)
                {
                    var parent = current.Parent;
                    if (parent is NameSyntax && parent.ChildNodes().First() == current)
                    {
                        current = parent;
                        continue;
                    }

                    return parent is MemberDeclarationSyntax && parent.ChildNodes().First() == current;
                }
                return false;
            }
        }
    }
}
