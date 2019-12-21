// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker
    {
        private static readonly ObjectPool<Dictionary<INamespaceOrTypeSymbol, string>> s_aliasMapPool
            = new ObjectPool<Dictionary<INamespaceOrTypeSymbol, string>>(() => new Dictionary<INamespaceOrTypeSymbol, string>());

        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly bool _preferPredefinedTypeInDecl;
        private readonly CancellationToken _cancellationToken;

        private readonly List<Dictionary<INamespaceOrTypeSymbol, string>> _aliasStack
            = new List<Dictionary<INamespaceOrTypeSymbol, string>>();

        public readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();

        public TypeSyntaxSimplifierWalker(
            SemanticModel semanticModel, OptionSet optionSet,
            bool preferPredefinedTypeInDecl, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _optionSet = optionSet;
            _preferPredefinedTypeInDecl = preferPredefinedTypeInDecl;
            _cancellationToken = cancellationToken;
        }

        private void AddAliases(
            Dictionary<INamespaceOrTypeSymbol, string> aliasMap,
            SyntaxList<UsingDirectiveSyntax> usings)
        {
            foreach (var @using in usings)
            {
                if (@using.Alias != null)
                {
                    var symbolInfo = _semanticModel.GetSymbolInfo(@using.Name, _cancellationToken);
                    if (symbolInfo.CandidateSymbols.Length > 0)
                        continue;

                    if (symbolInfo.Symbol is INamespaceOrTypeSymbol symbol)
                        aliasMap[symbol] = @using.Alias.Name.Identifier.ValueText;
                }
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            // For any member-decl (which includes named-types), descend into any leading doc
            // comments so we can fixup types there as well.
            if (node is MemberDeclarationSyntax memberDeclaration)
            {
                foreach (var trivia in memberDeclaration.GetLeadingTrivia())
                {
                    if (trivia.HasStructure)
                        this.Visit(trivia.GetStructure());
                }
            }

            base.DefaultVisit(node);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            using var aliases = s_aliasMapPool.GetPooledObject();
            AddAliases(aliases.Object, node.Usings);

            _aliasStack.Add(aliases.Object);
            base.VisitCompilationUnit(node);
            _aliasStack.RemoveAt(_aliasStack.Count - 1);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            using var aliases = s_aliasMapPool.GetPooledObject();
            AddAliases(aliases.Object, node.Usings);

            _aliasStack.Add(aliases.Object);
            base.VisitNamespaceDeclaration(node);
            _aliasStack.RemoveAt(_aliasStack.Count - 1);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node) &&
                TryReplaceWithPredefinedTypeOrAliasOrNullable(node))
            {
                return;
            }

            base.VisitGenericName(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node) &&
                TryReplaceWithPredefinedTypeOrAliasOrNullable(node))
            {
                return;
            }

            base.VisitIdentifierName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                if (TryReplaceWithPredefinedTypeOrAliasOrNullable(node))
                    return;

                if (TryReplaceQualifiedNameWithRightSide(node, node.Alias, node.Name))
                    return;
            }

            base.VisitAliasQualifiedName(node);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                if (TryReplaceWithPredefinedTypeOrAliasOrNullable(node))
                    return;

                if (TryReplaceQualifiedNameWithRightSide(node, node.Left, node.Right))
                    return;
            }

            base.VisitQualifiedName(node);
        }

        private bool IsNameOfUsingDirective(QualifiedNameSyntax node, out UsingDirectiveSyntax usingDirective)
        {
            while (node.Parent is QualifiedNameSyntax parent)
                node = parent;

            usingDirective = node.Parent as UsingDirectiveSyntax;
            return usingDirective != null;
        }

        private INamespaceOrTypeSymbol GetNamespaceOrTypeSymbol(TypeSyntax typeSyntax)
        {
            var symbolInfo = _semanticModel.GetSymbolInfo(typeSyntax, _cancellationToken);

            // Don't offer if we have ambiguity involved.
            if (symbolInfo.CandidateSymbols.Length > 0)
                return null;

            return symbolInfo.Symbol as INamespaceOrTypeSymbol;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this is a type-syntax that can be
        /// simplified. <see langword="false"/> otherwise.
        /// </summary>
        private bool TryReplaceWithPredefinedTypeOrAliasOrNullable(TypeSyntax typeSyntax)
        {
            var symbol = GetNamespaceOrTypeSymbol(typeSyntax);
            if (symbol == null)
                return false;

            if (TryReplaceWithPredefinedTypeOrNullable(typeSyntax, symbol))
                return true;

            if (TryReplaceWithAlias(typeSyntax, symbol))
                return true;

            return false;
        }

        private bool TryReplaceWithAlias(TypeSyntax typeSyntax, INamespaceOrTypeSymbol symbol)
        {
            // Next, see if there's an alias in scope we can bind to.
            for (var i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var symbolToAlias = _aliasStack[i];
                if (symbolToAlias.TryGetValue(symbol, out var alias))
                {
                    var foundSymbols = _semanticModel.LookupNamespacesAndTypes(typeSyntax.SpanStart, name: alias);
                    foreach (var found in foundSymbols)
                    {
                        if (found is IAliasSymbol aliasSymbol && aliasSymbol.Target.Equals(symbol))
                            return AddAliasDiagnostic(typeSyntax, alias);
                    }
                }
            }

            return false;
        }

        private bool AddAliasDiagnostic(TypeSyntax typeSyntax, string alias)
        {
            if (typeSyntax is IdentifierNameSyntax identifier &&
                alias == identifier.Identifier.ValueText)
            {
                // No point simplifying an identifier to the same alias name.
                return false;
            }

            // If we're replacing a qualified name with an alias that is the same as
            // the RHS, then don't mark the entire type-syntax as being simplified.
            // Only mark the LHS.
            if (typeSyntax is QualifiedNameSyntax { Right: IdentifierNameSyntax qualifiedRight, Left: var qualifiedLeft } &&
                alias == qualifiedRight.Identifier.ValueText)
            {
                return this.AddDiagnostic(qualifiedLeft.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
            }

            if (typeSyntax is AliasQualifiedNameSyntax { Name: IdentifierNameSyntax aliasName, Alias: var aliasAlias } &&
                alias == aliasName.Identifier.ValueText)
            {
                return this.AddDiagnostic(aliasAlias.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
            }

            return this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
        }

        private bool TryReplaceWithPredefinedTypeOrNullable(TypeSyntax typeSyntax, INamespaceOrTypeSymbol symbol)
        {
            // First, see if we can replace this type with a built-in type.
            if (!typeSyntax.IsParentKind(SyntaxKind.UsingDirective) &&
                symbol is ITypeSymbol typeSymbol)
            {
                if (_preferPredefinedTypeInDecl)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                        return this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);
                }

                if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    return this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
            }

            return false;
        }

        private bool TryReplaceQualifiedNameWithRightSide(
            NameSyntax aliasedOrQualifiedName, NameSyntax left, SimpleNameSyntax right)
        {
            var namespaceOrTypeSymbol = GetNamespaceOrTypeSymbol(aliasedOrQualifiedName);
            if (namespaceOrTypeSymbol == null)
                return false;

            if (aliasedOrQualifiedName is QualifiedNameSyntax qualifiedName &&
                IsNameOfUsingDirective(qualifiedName, out var usingDirective))
            {
                // Check for a couple of cases where it is legal to simplify, but where users prefer
                // that we not do that.

                // Do not replace `using NS1.NS2` with anything shorter if it binds to a namespace.
                // In a using declaration we've found that people prefer to see the full name for
                // clarity. Note: this does not apply to stripping the 'global' alias off of
                // something like `using global::NS1.NS2`.
                if (namespaceOrTypeSymbol is INamespaceSymbol)
                    return false;

                // Do not replace `using static NS1.C1` with anything shorter if it binds to a type.
                // In a using declaration we've found that people prefer to see the full name for
                // clarity. Note: this does not apply to stripping the 'global' alias off of
                // something like `using static global::NS1.C1`.
                if (usingDirective.StaticKeyword != default)
                    return false;
            }

            var symbols = _semanticModel.LookupSymbols(aliasedOrQualifiedName.SpanStart, name: right.Identifier.ValueText);
            foreach (var symbol in symbols)
            {
                if (symbol.OriginalDefinition.Equals(namespaceOrTypeSymbol.OriginalDefinition))
                    return AddDiagnostic(left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
            }

            return false;
        }

        private bool AddDiagnostic(TextSpan issueSpan, string diagnosticId)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, issueSpan, diagnosticId, inDeclaration: true));
            return true;
        }
    }
}
