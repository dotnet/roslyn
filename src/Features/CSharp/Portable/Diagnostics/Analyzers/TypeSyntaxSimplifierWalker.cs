// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var typeSymbol = GetNamespaceOrTypeSymbol(typeSyntax) as ITypeSymbol;
            if (typeSymbol == null)
                return false;

            // First, see if we can replace this type with a built-in type.
            if (!typeSyntax.IsParentKind(SyntaxKind.UsingDirective))
            {
                if (_preferPredefinedTypeInDecl)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                    {
                        this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);
                        return true;
                    }
                }

                if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                    return true;
                }
            }

            // Next, see if there's an alias in scope we can bind to.
            using (var pooledArray = ArrayBuilder<string>.GetInstance(out var aliases))
            {
                AddAliases(typeSymbol, aliases);
                foreach (var alias in aliases)
                {
                    var symbols = _semanticModel.LookupNamespacesAndTypes(typeSyntax.SpanStart, name: alias);
                    foreach (var symbol in symbols)
                    {
                        if (symbol is IAliasSymbol aliasSymbol &&
                            aliasSymbol.Target.Equals(typeSymbol))
                        {
                            this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool TryReplaceQualifiedNameWithRightSide(
            NameSyntax qualifiedName, NameSyntax left, SimpleNameSyntax right)
        {
            var namespaceOrTypeSymbol = GetNamespaceOrTypeSymbol(qualifiedName);
            if (namespaceOrTypeSymbol == null)
                return false;

            var symbols = _semanticModel.LookupSymbols(qualifiedName.SpanStart, name: right.Identifier.ValueText);
            foreach (var symbol in symbols)
            {
                if (symbol.OriginalDefinition.Equals(namespaceOrTypeSymbol.OriginalDefinition))
                {
                    AddDiagnostic(left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                    return true;
                }
            }

            return false;
        }

        private void AddDiagnostic(TextSpan issueSpan, string diagnosticId)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, issueSpan, diagnosticId, inDeclaration: true));
        }

        private void AddAliases(ITypeSymbol typeSymbol, ArrayBuilder<string> aliases)
        {
            for (var i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var typeToAlias = _aliasStack[i];
                if (typeToAlias.TryGetValue(typeSymbol, out var alias))
                {
                    aliases.Add(alias);
                }
            }
        }
    }
}
