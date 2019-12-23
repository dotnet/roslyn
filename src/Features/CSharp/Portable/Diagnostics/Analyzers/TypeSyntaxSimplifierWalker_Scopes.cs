// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal partial class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker, IDisposable
    {
        private readonly SemanticModel _semanticModel;
        private readonly OptionSet _optionSet;
        private readonly bool _preferPredefinedTypeInDecl;
        private readonly CancellationToken _cancellationToken;

        private readonly List<Dictionary<INamespaceOrTypeSymbol, string>> _aliasStack;
        private readonly List<HashSet<string>> _aliasedSymbolNamesStack;
        private readonly List<HashSet<string>> _namesInScopeStack;

        public readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();

        public TypeSyntaxSimplifierWalker(
            SemanticModel semanticModel, OptionSet optionSet,
            bool preferPredefinedTypeInDecl, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _optionSet = optionSet;
            _preferPredefinedTypeInDecl = preferPredefinedTypeInDecl;
            _cancellationToken = cancellationToken;

            _aliasStack = SharedPools.Default<List<Dictionary<INamespaceOrTypeSymbol, string>>>().Allocate();
            _aliasedSymbolNamesStack = SharedPools.Default<List<HashSet<string>>>().Allocate();
            _namesInScopeStack = SharedPools.Default<List<HashSet<string>>>().Allocate();
        }

        public void Dispose()
        {
            SharedPools.Default<List<Dictionary<INamespaceOrTypeSymbol, string>>>().ClearAndFree(_aliasStack);
            SharedPools.Default<List<HashSet<string>>>().ClearAndFree(_aliasedSymbolNamesStack);
            SharedPools.Default<List<HashSet<string>>>().ClearAndFree(_namesInScopeStack);
        }

        private static T Peek<T>(List<T> stack)
            => stack[stack.Count - 1];

        private static void Pop<T>(List<T> stack)
            => stack.RemoveAt(stack.Count - 1);

        private void AddAliases(
            Dictionary<INamespaceOrTypeSymbol, string> aliasMap,
            HashSet<string> aliasedSymbolNames,
            SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (_aliasedSymbolNamesStack.Count > 0)
            {
                // Include the members of the top of the stack in the new indices we're making.
                aliasedSymbolNames.UnionWith(Peek(_aliasedSymbolNamesStack));
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
                        aliasMap[symbol] = @using.Alias.Name.Identifier.ValueText;
                        aliasedSymbolNames.Add(symbol.Name);
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
                VisitMemberDeclaration(memberDeclaration);
            }

            base.DefaultVisit(node);
        }

        private void VisitMemberDeclaration(MemberDeclarationSyntax memberDeclaration)
        {
            foreach (var trivia in memberDeclaration.GetLeadingTrivia())
            {
                if (trivia.HasStructure)
                    this.Visit(trivia.GetStructure());
            }
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            using var aliases = SharedPools.Default<Dictionary<INamespaceOrTypeSymbol, string>>().GetPooledObject();
            using var aliasedSymbolNames = SharedPools.StringHashSet.GetPooledObject();
            using var namesInScope = SharedPools.StringHashSet.GetPooledObject();

            AddAliases(aliases.Object, aliasedSymbolNames.Object, node.Usings);
            AddNamesInScope(namesInScope.Object,
                node.AttributeLists.FirstOrDefault()?.SpanStart,
                node.Usings.FirstOrDefault()?.SpanStart,
                node.EndOfFileToken.SpanStart);

            _aliasStack.Add(aliases.Object);
            _aliasedSymbolNamesStack.Add(aliasedSymbolNames.Object);
            _namesInScopeStack.Add(namesInScope.Object);

            base.VisitCompilationUnit(node);

            Pop(_aliasStack);
            Pop(_aliasedSymbolNamesStack);
            Pop(_namesInScopeStack);
        }

        private void AddNamesInScope(
            HashSet<string> names, int? positionOpt1, int? positionOpt2 = null, int? positionOpt3 = null)
        {
            var position = positionOpt1 ?? positionOpt2 ?? positionOpt3 ?? 0;
            var symbols = _semanticModel.LookupNamespacesAndTypes(position);
            foreach (var symbol in symbols)
            {
                names.Add(symbol.Name);
            }
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            using var aliases = SharedPools.Default<Dictionary<INamespaceOrTypeSymbol, string>>().GetPooledObject();
            using var aliasedSymbolNames = SharedPools.StringHashSet.GetPooledObject();
            using var namesInScope = SharedPools.StringHashSet.GetPooledObject();

            AddAliases(aliases.Object, aliasedSymbolNames.Object, node.Usings);
            AddNamesInScope(namesInScope.Object, node.OpenBraceToken.Span.End);

            _aliasStack.Add(aliases.Object);
            _aliasedSymbolNamesStack.Add(aliasedSymbolNames.Object);
            _namesInScopeStack.Add(namesInScope.Object);

            base.VisitNamespaceDeclaration(node);

            Pop(_aliasStack);
            Pop(_aliasedSymbolNamesStack);
            Pop(_namesInScopeStack);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            using var namesInScope = SharedPools.StringHashSet.GetPooledObject();

            AddNamesInScope(namesInScope.Object, node.OpenBraceToken.Span.End);
            _namesInScopeStack.Add(namesInScope.Object);

            base.VisitClassDeclaration(node);

            Pop(_namesInScopeStack);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            using var namesInScope = SharedPools.StringHashSet.GetPooledObject();

            AddNamesInScope(namesInScope.Object, node.OpenBraceToken.Span.End);
            _namesInScopeStack.Add(namesInScope.Object);

            base.VisitStructDeclaration(node);

            Pop(_namesInScopeStack);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            using var namesInScope = SharedPools.StringHashSet.GetPooledObject();

            AddNamesInScope(namesInScope.Object, node.OpenBraceToken.Span.End);
            _namesInScopeStack.Add(namesInScope.Object);

            base.VisitInterfaceDeclaration(node);

            Pop(_namesInScopeStack);
        }
    }
}
