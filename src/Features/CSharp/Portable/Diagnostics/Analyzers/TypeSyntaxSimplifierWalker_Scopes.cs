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
        /// Set of type and namespace names that have an alias associated with them.  i.e. if the
        /// user has <c>using X = System.DateTime</c>, then <c>DateTime</c> will be in this set.
        /// This is used so we can easily tell if we should try to simplify some identifier to an
        /// alias when we encounter it.
        /// </summary>
        private HashSet<string> _aliasedSymbolNames;

        /// <summary>
        /// Set of types and namespace names currently in scope based on the usings/namespaces we're
        /// inside of.  This can be used to tell if we can simplify <c>X.Y</c> (in a types-only
        /// context) to just<c>Y</c>.  If there is no declaration in scope called <c>Y</c> we don't
        /// have to bother checking.
        /// </summary>
        private HashSet<string> _declarationNamesInScope;

        /// <summary>
        /// Similar to <see cref="_declarationNamesInScope"/> except this also contains static
        /// members. Used in expression contexts to tell if <c>X.Y</c> can be simplified to just
        /// <c>Y</c>.
        /// </summary>
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

        /// <summary>
        /// Common helper that we call through with all nodes that can add type, namespace and
        /// static names into the current scope.
        /// </summary>
        private void EnterScope<TNode>(
            TNode node, SyntaxList<UsingDirectiveSyntax> usings,
            int position, Action<TNode> func) where TNode : SyntaxNode
        {
            var savedAliasedSymbolNames = _aliasedSymbolNames;
            var savedDeclarationNamesInScope = _declarationNamesInScope;
            var savedStaticNamesInScope = _staticNamesInScope;

            _aliasedSymbolNames = SharedPools.StringHashSet.Allocate();
            _declarationNamesInScope = SharedPools.StringHashSet.Allocate();
            _staticNamesInScope = SharedPools.StringHashSet.Allocate();

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
            => EnterScope(node, node.Usings,
                node.AttributeLists.FirstOrDefault()?.SpanStart ??
                node.Usings.FirstOrDefault()?.SpanStart ??
                node.EndOfFileToken.SpanStart,
                _visitBaseCompilationUnit);

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            => EnterScope(node, node.Usings, node.OpenBraceToken.Span.End, _visitBaseNamespaceDeclaration);

        private void EnterNamedTypeScope<TNode>(TNode node, Action<TNode> func) where TNode : BaseTypeDeclarationSyntax
            => EnterScope(node, usings: default, node.OpenBraceToken.Span.End, func);

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            => EnterNamedTypeScope(node, _visitBaseClassDeclaration);

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
            => EnterNamedTypeScope(node, _visitBaseStructDeclaration);

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            => EnterNamedTypeScope(node, _visitBaseInterfaceDeclaration);

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            => EnterNamedTypeScope(node, _visitBaseEnumDeclaration);
    }
}
