// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker, IDisposable
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
            if (_aliasStack.Count > 0)
            {
                // Include the members of the top of the stack in the new indices we're making.
                aliasMap.AddRange(Peek(_aliasStack));
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

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Don't bother looking at the right side of A.B or A::B.  We will process those in
            // VisitQualifiedName and VisitAliasQualifiedName.
            if (node.IsRightSideOfDotOrColonColon())
                return;

            if (!SyntaxFacts.IsInNamespaceOrTypeContext(node))
                return;

            // If we have an identifier, we would only ever replace it with an alias or a
            // predefined-type name.  Do a very quick syntactic check to even see if either of those
            // are possible.
            var identifier = node.Identifier.ValueText;
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                return;

            if (TryReplaceWithAlias(node, identifier, ref symbol))
                return;

            // No need to call `base.VisitIdentifierName()`.  identifier have no
            // children we need to process.
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            // Don't bother looking at the right side of A.G<...> or A::G<...>.  We will process
            // those in VisitQualifiedName and VisitAliasQualifiedName.
            if (!node.IsRightSideOfDotOrColonColon())
            {
                if (!SyntaxFacts.IsInNamespaceOrTypeContext(node))
                    return;

                // A generic name is never a predefined type. So we don't need to check for that.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithAlias(node, identifier, ref symbol))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier, ref symbol))
                    return;
            }

            // Try to simplify the type arguments if we can't simplify anything else.
            this.Visit(node.TypeArgumentList);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                // We have a qualified name (like A.B).  Check and see if 'B' is the name of
                // predefined type, or if there's something aliased to the name B.
                var identifier = node.Right.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                    return;

                if (TryReplaceWithAlias(node, identifier, ref symbol))
                    return;

                if (TryReplaceWithNullable(node, identifier, ref symbol))
                    return;

                // Wasn't predefined or an alias.  See if we can just reduce it to 'B'.
                if (TryReplaceQualifiedNameWithRightSide(node, identifier, node.Left, node.Right, ref symbol))
                    return;
            }

            // we could have something like `A.B.C<D.E>`.  We want to visit both A.B to see if that
            // can be simplified as well as D.E.
            base.VisitQualifiedName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
            {
                var identifier = node.Name.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                    return;

                if (TryReplaceWithAlias(node, identifier, ref symbol))
                    return;

                if (TryReplaceQualifiedNameWithRightSide(node, identifier, node.Alias, node.Name, ref symbol))
                    return;
            }

            // We still want to simplify the right side of this name.  We might have something
            // like `A::G<X.Y>` which could be simplified to `A::G<Y>`.
            this.Visit(node.Name);
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

        private bool TryReplaceWithAlias(
            TypeSyntax typeSyntax, string typeName, ref INamespaceOrTypeSymbol symbol)
        {
            // See if we actually have an alias to something with our name.
            if (!Peek(_aliasedSymbolNamesStack).Contains(typeName))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
            if (symbol == null)
                return false;

            // Next, see if there's an alias in scope we can bind to.
            var symbolToAlias = Peek(_aliasStack);
            if (symbolToAlias.TryGetValue(symbol, out var alias))
            {
                var foundSymbols = _semanticModel.LookupNamespacesAndTypes(typeSyntax.SpanStart, name: alias);
                foreach (var found in foundSymbols)
                {
                    if (found is IAliasSymbol aliasSymbol && aliasSymbol.Target.Equals(symbol))
                    {
                        return AddAliasDiagnostic(typeSyntax, alias);
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithPredefinedType(
            TypeSyntax typeSyntax, string typeName, ref INamespaceOrTypeSymbol symbol)
        {
            if (_preferPredefinedTypeInDecl &&
                s_predefinedTypeNames.Contains(typeName) &&
                !typeSyntax.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
                if (symbol is ITypeSymbol typeSymbol)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                    {
                        return this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithNullable(
            TypeSyntax typeSyntax, string typeName, ref INamespaceOrTypeSymbol symbol)
        {
            if (typeName == nameof(Nullable) &&
                !typeSyntax.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
                if (symbol is ITypeSymbol typeSymbol &&
                    typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                }
            }

            return false;
        }

        private bool TryReplaceQualifiedNameWithRightSide(
            NameSyntax aliasedOrQualifiedName, string identifier,
            NameSyntax left, SimpleNameSyntax right,
            ref INamespaceOrTypeSymbol symbol)
        {
            // We have a name like A.B or A::B.

            // First see if we even have a type/namespace in scope called 'B'.  If not,
            // there's nothing we need to do further.
            if (!Peek(_namesInScopeStack).Contains(identifier))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(aliasedOrQualifiedName);
            if (symbol == null)
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
                if (symbol is INamespaceSymbol)
                    return false;

                // Do not replace `using static NS1.C1` with anything shorter if it binds to a type.
                // In a using declaration we've found that people prefer to see the full name for
                // clarity. Note: this does not apply to stripping the 'global' alias off of
                // something like `using static global::NS1.C1`.
                if (usingDirective.StaticKeyword != default)
                    return false;
            }

            // Now try to bind just 'B' in our current location.  If it binds to 'A.B' then we can
            // reduce to just that name.
            var foundSymbols = _semanticModel.LookupSymbols(aliasedOrQualifiedName.SpanStart, name: right.Identifier.ValueText);
            foreach (var found in foundSymbols)
            {
                if (symbol.OriginalDefinition.Equals(found.OriginalDefinition))
                {
                    return AddDiagnostic(left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
                }
            }

            return false;
        }

        private bool AddDiagnostic(TextSpan issueSpan, string diagnosticId)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, issueSpan, diagnosticId, inDeclaration: true));
            return true;
        }

        private static readonly HashSet<string> s_predefinedTypeNames = new HashSet<string>
        {
            nameof(Boolean),
            nameof(Byte),
            nameof(SByte),
            nameof(Int32),
            nameof(UInt32),
            nameof(Int16),
            nameof(UInt16),
            nameof(Int64),
            nameof(UInt64),
            nameof(Single),
            nameof(Double),
            nameof(Decimal),
            nameof(String),
            nameof(Char),
            nameof(Object),
        };
    }
}
