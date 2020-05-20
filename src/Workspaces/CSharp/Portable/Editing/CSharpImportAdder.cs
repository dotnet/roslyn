// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Editing
{
    [ExportLanguageService(typeof(ImportAdderService), LanguageNames.CSharp), Shared]
    internal class CSharpImportAdder : ImportAdderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

        protected override void AddPotentiallyConflictingImports(
            SyntaxNode root, IEnumerable<INamespaceOrTypeSymbol> namespaceMembers, IEnumerable<IMethodSymbol> extensionMethods, SemanticModel model, HashSet<INamespaceSymbol> conflicts, CancellationToken cancellationToken)
        {
            var rewriter = new ConflictWalker(namespaceMembers, extensionMethods, model, conflicts, cancellationToken);
            rewriter.Visit(root);
        }

        private INamespaceSymbol? GetExplicitNamespaceSymbol(ExpressionSyntax fullName, ExpressionSyntax namespacePart, SemanticModel model)
        {

            // name must refer to something that is not a namespace, but be qualified with a namespace.
            var symbol = model.GetSymbolInfo(fullName).Symbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace && model.GetSymbolInfo(namespacePart).Symbol is INamespaceSymbol)
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

        private class ConflictWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _model;
            private readonly CancellationToken _cancellationToken;

            /// <summary>
            /// A mapping containing the simple names and arity of all namespace members, mapped to the import that
            /// they're brought in by.
            /// </summary>
            private readonly MultiDictionary<(string name, int arity), INamespaceSymbol> _namespaceMembers
                = new MultiDictionary<(string name, int arity), INamespaceSymbol>();

            /// <summary>
            /// A mapping containing the simple names of all extension methods, mapped to the import that they're
            /// brought in by.  This doesn't keep track of arity because methods can be called with type arguments.
            /// </summary>
            private readonly MultiDictionary<string, INamespaceSymbol> _extensionMethods
                = new MultiDictionary<string, INamespaceSymbol>();

            private readonly HashSet<INamespaceSymbol> _conflictNamespaces;

            public ConflictWalker(
                IEnumerable<INamespaceOrTypeSymbol> namespaceMembers,
                IEnumerable<IMethodSymbol> extensionMethods,
                SemanticModel model,
                HashSet<INamespaceSymbol> conflictNamespaces,
                CancellationToken cancellationToken)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                _model = model;
                _cancellationToken = cancellationToken;
                _conflictNamespaces = conflictNamespaces;

                foreach (var member in namespaceMembers)
                    _namespaceMembers.Add((member.Name, member.GetArity()), member.ContainingNamespace);

                foreach (var method in extensionMethods)
                    _extensionMethods.Add(method.Name, method.ContainingNamespace);
            }

            private void CheckName(NameSyntax node)
            {
                // Check to see if we have an standalone identifer (or identifier on the left of a dot).
                // If so, if that identifier binds to a namespace or type, then we don't want to bring in
                // any imports that would bring in the same name and could then potentially conflict here.

                if (node.IsRightSideOfDotOrArrowOrColonColon())
                    return;

                var symbol = _model.GetSymbolInfo(node, _cancellationToken).GetAnySymbol();
                if (symbol == null)
                    return;

                if (symbol.Kind != SymbolKind.Namespace && symbol.Kind != SymbolKind.NamedType)
                    return;

                _conflictNamespaces.AddRange(_namespaceMembers[(symbol.Name, node.Arity)]);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                base.VisitIdentifierName(node);
                CheckName(node);
            }

            public override void VisitGenericName(GenericNameSyntax node)
            {
                base.VisitGenericName(node);
                CheckName(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                base.VisitMemberAccessExpression(node);

                // Check to see if we have a reference to an extension method.  If so, then pulling in an import could
                // bring in an extension that conflicts with that.

                var symbol = _model.GetSymbolInfo(node.Name, _cancellationToken).GetAnySymbol();
                if (!(symbol is IMethodSymbol method))
                    return;

                if (!method.OriginalDefinition.IsExtensionMethod)
                    return;

                _conflictNamespaces.AddRange(_extensionMethods[method.Name]);
            }
        }
    }
}
