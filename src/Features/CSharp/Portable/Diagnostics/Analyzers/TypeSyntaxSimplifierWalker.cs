// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    internal class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly bool _preferPredefinedTypeInDecl;
        private readonly CancellationToken _cancellationToken;

        private readonly List<Dictionary<ITypeSymbol, string>> _aliasStack
            = new List<Dictionary<ITypeSymbol, string>>();

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

        private Dictionary<ITypeSymbol, string> GetAliases(
            SyntaxList<UsingDirectiveSyntax> usings)
        {
            var result = new Dictionary<ITypeSymbol, string>();

            foreach (var @using in usings)
            {
                if (@using.Alias != null)
                {
                    var aliasVal = _semanticModel.GetTypeInfo(@using.Name, _cancellationToken).Type;
                    if (aliasVal != null)
                    {
                        result[aliasVal] = @using.Alias.Name.Identifier.ValueText;
                    }
                }
            }

            return result;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            _aliasStack.Add(GetAliases(node.Usings));
            base.VisitCompilationUnit(node);
            _aliasStack.RemoveAt(_aliasStack.Count - 1);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _aliasStack.Add(GetAliases(node.Usings));
            base.VisitNamespaceDeclaration(node);
            _aliasStack.RemoveAt(_aliasStack.Count - 1);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node) &&
                TryReplaceWithPredefinedTypeOrAlias(node))
                return;

            base.VisitGenericName(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node) &&
                TryReplaceWithPredefinedTypeOrAlias(node))
                return;

            base.VisitIdentifierName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                if (TryReplaceWithPredefinedTypeOrAlias(node))
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
                if (TryReplaceWithPredefinedTypeOrAlias(node))
                    return;

                if (TryReplaceQualifiedNameWithRightSide(node, node.Left, node.Right))
                    return;
            }

            base.VisitQualifiedName(node);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this is a type-syntax that can be
        /// simplified. <see langword="false"/> otherwise.
        /// </summary>
        private bool TryReplaceWithPredefinedTypeOrAlias(TypeSyntax typeSyntax)
        {
            var typeSymbol = _semanticModel.GetTypeInfo(typeSyntax, _cancellationToken).Type;
            if (typeSymbol == null)
                return false;

            // First, see if we can replace this type with a builtin type.
            if (_preferPredefinedTypeInDecl)
            {
                var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                if (specialTypeKind != SyntaxKind.None)
                {
                    this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);
                    return true;
                }
            }

            // Next, see if there's an alias in scope we can bind to.
            if (TryGetAlias(typeSymbol, out var alias))
            {
                var symbols = _semanticModel.LookupNamespacesAndTypes(typeSyntax.SpanStart, name: alias);
                if (symbols.Contains(typeSymbol))
                {
                    this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                    return true;
                }
            }

            return false;
        }

        private bool TryReplaceQualifiedNameWithRightSide(
            NameSyntax qualifiedName, NameSyntax left, SimpleNameSyntax right)
        {
            var typeSymbol = _semanticModel.GetTypeInfo(qualifiedName, _cancellationToken).Type;
            if (typeSymbol != null)
            {
                typeSymbol = typeSymbol.OriginalDefinition;
                var symbols = _semanticModel.LookupSymbols(qualifiedName.SpanStart, name: right.Identifier.ValueText);
                foreach (var symbol in symbols)
                {
                    if (symbol.OriginalDefinition.Equals(typeSymbol))
                    {
                        AddDiagnostic(left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddDiagnostic(TextSpan issueSpan, string diagnosticId)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, issueSpan, diagnosticId, inDeclaration: true));
        }

        private bool TryGetAlias(ITypeSymbol typeSymbol, out string alias)
        {
            for (int i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var aliases = _aliasStack[i];
                if (aliases.TryGetValue(typeSymbol, out alias))
                {
                    return true;
                }
            }

            alias = null;
            return false;
        }
    }
}
