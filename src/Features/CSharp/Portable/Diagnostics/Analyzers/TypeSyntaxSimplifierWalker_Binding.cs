// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    //class A
    //{
    //    public static int X;
    //}

    //class B : A
    //{
    //    void M()
    //    {
    //        var v = A.X;
    //    }
    //}

    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal partial class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker, IDisposable
    {
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Don't bother looking at the right side of A.B or A::B.  We will process those in
            // VisitQualifiedName and VisitAliasQualifiedName.
            if (!node.IsRightSideOfDotOrArrowOrColonColon())
            {
                var inDeclaration = SyntaxFacts.IsInNamespaceOrTypeContext(node);

                // If we have an identifier, we would only ever replace it with an alias or a
                // predefined-type name.  Do a very quick syntactic check to even see if either of those
                // are possible.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithPredefinedType(node, identifier, inDeclaration, ref symbol))
                    return;

                if (TryReplaceWithAlias(node, identifier, inDeclaration, ref symbol))
                    return;
            }

            // No need to call `base.VisitIdentifierName()`.  identifier have no
            // children we need to process.
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            // Don't bother looking at the right side of A.G<...> or A::G<...>.  We will process
            // those in VisitQualifiedName and VisitAliasQualifiedName.
            if (!node.IsRightSideOfDotOrColonColon())
            {
                var inDeclaration = SyntaxFacts.IsInNamespaceOrTypeContext(node);

                // A generic name is never a predefined type. So we don't need to check for that.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithAlias(node, identifier, inDeclaration, ref symbol))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier, inDeclaration, ref symbol))
                    return;
            }

            // Try to simplify the type arguments if we can't simplify anything else.
            this.Visit(node.TypeArgumentList);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            var inDeclaration = SyntaxFacts.IsInNamespaceOrTypeContext(node);

            // We have a qualified name (like A.B).  Check and see if 'B' is the name of
            // predefined type, or if there's something aliased to the name B.
            var identifier = node.Right.Identifier.ValueText;
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, inDeclaration, ref symbol))
                return;

            if (TryReplaceWithAlias(node, identifier, inDeclaration, ref symbol))
                return;

            if (TryReplaceWithNullable(node, identifier, inDeclaration, ref symbol))
                return;

            // Wasn't predefined or an alias.  See if we can just reduce it to 'B'.
            if (TryReplaceQualifiedNameWithRightSide(node, identifier, node.Left, node.Right, inDeclaration, ref symbol))
                return;

            // we could have something like `A.B.C<D.E>`.  We want to visit both A.B to see if that
            // can be simplified as well as D.E.
            base.VisitQualifiedName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            var inDeclaration = SyntaxFacts.IsInNamespaceOrTypeContext(node);

            var identifier = node.Name.Identifier.ValueText;
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, inDeclaration, ref symbol))
                return;

            if (TryReplaceWithAlias(node, identifier, inDeclaration, ref symbol))
                return;

            if (TryReplaceQualifiedNameWithRightSide(node, identifier, node.Alias, node.Name, inDeclaration, ref symbol))
                return;

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

        private bool AddAliasDiagnostic(TypeSyntax typeSyntax, string alias, bool inDeclaration)
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
                return this.AddDiagnostic(qualifiedLeft.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
            }

            if (typeSyntax is AliasQualifiedNameSyntax { Name: IdentifierNameSyntax aliasName, Alias: var aliasAlias } &&
                alias == aliasName.Identifier.ValueText)
            {
                return this.AddDiagnostic(aliasAlias.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
            }

            return this.AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
        }

        private bool TryReplaceWithAlias(
            TypeSyntax typeSyntax, string typeName,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            // See if we actually have an alias to something with our name.
            if (!Peek(_aliasedSymbolNamesStack).Contains(typeName))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
            if (symbol == null)
                return false;

            // Next, see if there's an alias in scope we can bind to.
            for (var i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var symbolToAlias = _aliasStack[i];
                if (symbolToAlias.TryGetValue(symbol, out var alias))
                {
                    var foundSymbols = LookupName(typeSyntax, inDeclaration, alias);
                    foreach (var found in foundSymbols)
                    {
                        if (found is IAliasSymbol aliasSymbol && aliasSymbol.Target.Equals(symbol))
                        {
                            return AddAliasDiagnostic(typeSyntax, alias, inDeclaration);
                        }
                    }
                }
            }

            return false;
        }

        private ImmutableArray<ISymbol> LookupName(TypeSyntax typeSyntax, bool inDeclaration, string name)
            => inDeclaration
                ? _semanticModel.LookupNamespacesAndTypes(typeSyntax.SpanStart, name: name)
                : _semanticModel.LookupSymbols(typeSyntax.SpanStart, name: name);

        private bool TryReplaceWithPredefinedType(
            TypeSyntax typeSyntax, string typeName,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            if (inDeclaration && !_preferPredefinedTypeInDecl)
                return false;

            if (!inDeclaration && !_preferPredefinedTypeInMemberAccess)
                return false;

            if (s_predefinedTypeNames.Contains(typeName) &&
                !typeSyntax.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
                if (symbol is ITypeSymbol typeSymbol)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                    {
                        return this.AddDiagnostic(
                            typeSyntax.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId, inDeclaration);
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithNullable(
            TypeSyntax typeSyntax, string typeName,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            // `int?` can only be used in a type-decl context.  i.e. it can't be used like 
            // `int?.Equals()`
            if (inDeclaration &&
                typeName == nameof(Nullable) &&
                !typeSyntax.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(typeSyntax);
                if (symbol is ITypeSymbol typeSymbol &&
                    typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return AddDiagnostic(typeSyntax.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
                }
            }

            return false;
        }

        private bool TryReplaceQualifiedNameWithRightSide(
            NameSyntax aliasedOrQualifiedName, string identifier,
            NameSyntax left, SimpleNameSyntax right,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
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
            var foundSymbols = LookupName(aliasedOrQualifiedName, inDeclaration, right.Identifier.ValueText);
            foreach (var found in foundSymbols)
            {
                if (symbol.OriginalDefinition.Equals(found.OriginalDefinition))
                {
                    return AddDiagnostic(
                        left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
                }
            }

            return false;
        }

        private bool AddDiagnostic(TextSpan issueSpan, string diagnosticId, bool inDeclaration)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, issueSpan, diagnosticId, inDeclaration));
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
