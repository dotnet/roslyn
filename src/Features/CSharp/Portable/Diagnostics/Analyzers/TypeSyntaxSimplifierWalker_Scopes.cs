// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal partial class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker
    {
        private readonly CSharpSimplifyTypeNamesDiagnosticAnalyzer _analyzer;
        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly bool _preferPredefinedTypeInDecl;
        private readonly bool _preferPredefinedTypeInMemberAccess;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Set of all the type names that this compilation knows about (both through source and
        /// metadata).  This is used so that when we see <c>Y.Z</c> we can know if <c>Y</c> could be
        /// a type and thus if we should try to simplify that to <c>X.Z</c> (if <c>X</c> is a base
        /// class of <c>Y</c>).
        /// </summary>
        private HashSet<string> _compilationTypeNames;

        /// <summary>
        /// 
        /// </summary>
        private HashSet<string> _aliasedSymbolNames;
        private HashSet<string> _declarationNamesInScope;
        private HashSet<string> _staticNamesInScope;

        private readonly Action<CompilationUnitSyntax> _visitBaseCompilationUnit;
        private readonly Action<NamespaceDeclarationSyntax> _visitBaseNamespaceDeclaration;
        private readonly Action<ClassDeclarationSyntax> _visitBaseClassDeclaration;
        private readonly Action<StructDeclarationSyntax> _visitBaseStructDeclaration;
        private readonly Action<InterfaceDeclarationSyntax> _visitBaseInterfaceDeclaration;
        private readonly Action<EnumDeclarationSyntax> _visitBaseEnumDeclaration;

        public readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();

        public TypeSyntaxSimplifierWalker(
            CSharpSimplifyTypeNamesDiagnosticAnalyzer analyzer, SemanticModel semanticModel,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            _analyzer = analyzer;
            _semanticModel = semanticModel;
            _optionSet = optionSet;
            _cancellationToken = cancellationToken;

            _preferPredefinedTypeInDecl = optionSet.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, semanticModel.Language).Value;
            _preferPredefinedTypeInMemberAccess = optionSet.GetOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, semanticModel.Language).Value;

            _compilationTypeNames = SharedPools.StringHashSet.Allocate();
            _compilationTypeNames.AddAll(semanticModel.Compilation.Assembly.TypeNames);

            _visitBaseCompilationUnit = n => base.VisitCompilationUnit(n);
            _visitBaseNamespaceDeclaration = n => base.VisitNamespaceDeclaration(n);
            _visitBaseClassDeclaration = n => base.VisitClassDeclaration(n);
            _visitBaseStructDeclaration = n => base.VisitStructDeclaration(n);
            _visitBaseInterfaceDeclaration = n => base.VisitInterfaceDeclaration(n);
            _visitBaseEnumDeclaration = n => base.VisitEnumDeclaration(n);
        }

        private void AddAliases(HashSet<string> savedAliasedSymbolNames, SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (savedAliasedSymbolNames != null)
            {
                // Include the members of the top of the stack in the new indices we're making.
                _aliasedSymbolNames.UnionWith(savedAliasedSymbolNames);
            }

            foreach (var @using in usings)
            {
                if (@using.Alias != null)
                {
                    var symbolInfo = _semanticModel.GetSymbolInfo(@using.Name, _cancellationToken);
                    if (symbolInfo.CandidateSymbols.Length > 0)
                        continue;

                    if (symbolInfo.Symbol is INamespaceOrTypeSymbol symbol)
                    {
                        _aliasedSymbolNames.Add(symbol.Name);
                    }
                }
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            // For any member-decl (which includes named-types), descend into any leading doc
            // comments so we can simplify types there as well.
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

        private void EnterNamespaceContext<TNode>(
            TNode node, SyntaxList<UsingDirectiveSyntax> usings,
            int position, Action<TNode> func) where TNode : SyntaxNode
        {
            var savedAliasedSymbolNames = _aliasedSymbolNames;
            var savedDeclarationNamesInScope = _declarationNamesInScope;
            var savedStaticNamesInScope = _staticNamesInScope;

            _aliasedSymbolNames = SharedPools.StringHashSet.GetPooledObject().Object;
            _declarationNamesInScope = SharedPools.StringHashSet.GetPooledObject().Object;
            _staticNamesInScope = SharedPools.StringHashSet.GetPooledObject().Object;

            AddAliases(savedAliasedSymbolNames, usings);
            AddNamesInScope(position);

            func(node);

            SharedPools.StringHashSet.ClearAndFree(_aliasedSymbolNames);
            SharedPools.StringHashSet.ClearAndFree(_declarationNamesInScope);
            SharedPools.StringHashSet.ClearAndFree(_staticNamesInScope);

            _aliasedSymbolNames = savedAliasedSymbolNames;
            _declarationNamesInScope = savedDeclarationNamesInScope;
            _staticNamesInScope = savedStaticNamesInScope;
        }

        private void AddNamesInScope(int position)
        {
            var declarationSymbols = _semanticModel.LookupNamespacesAndTypes(position);
            foreach (var symbol in declarationSymbols)
                _declarationNamesInScope.Add(symbol.Name);

            var staticSymbols = _semanticModel.LookupStaticMembers(position);
            foreach (var symbol in staticSymbols)
                _staticNamesInScope.Add(symbol.Name);
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
            => EnterNamespaceContext(node, node.Usings,
                node.AttributeLists.FirstOrDefault()?.SpanStart ??
                node.Usings.FirstOrDefault()?.SpanStart ??
                node.EndOfFileToken.SpanStart,
                _visitBaseCompilationUnit);

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            => EnterNamespaceContext(node, node.Usings, node.OpenBraceToken.Span.End, _visitBaseNamespaceDeclaration);

        private void EnterNamedTypeContext<TNode>(TNode node, Action<TNode> func) where TNode : BaseTypeDeclarationSyntax
        {
            var savedDeclarationNamesInScope = _declarationNamesInScope;
            var savedStaticNamesInScope = _staticNamesInScope;

            _declarationNamesInScope = SharedPools.StringHashSet.GetPooledObject().Object;
            _staticNamesInScope = SharedPools.StringHashSet.GetPooledObject().Object;

            AddNamesInScope(node.OpenBraceToken.Span.End);

            func(node);

            SharedPools.StringHashSet.ClearAndFree(_declarationNamesInScope);
            SharedPools.StringHashSet.ClearAndFree(_staticNamesInScope);

            _declarationNamesInScope = savedDeclarationNamesInScope;
            _staticNamesInScope = savedStaticNamesInScope;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            => EnterNamedTypeContext(node, _visitBaseClassDeclaration);

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
            => EnterNamedTypeContext(node, _visitBaseStructDeclaration);

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            => EnterNamedTypeContext(node, _visitBaseInterfaceDeclaration);

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            => EnterNamedTypeContext(node, _visitBaseEnumDeclaration);
    }
}
