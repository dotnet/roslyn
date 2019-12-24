// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using Humanizer;
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
        private ImmutableArray<ISymbol> LookupName(SyntaxNode location, bool inDeclaration, string name)
            => inDeclaration
                ? _semanticModel.LookupNamespacesAndTypes(location.SpanStart, name: name)
                : _semanticModel.LookupSymbols(location.SpanStart, name: name);

        // For back-compat, we treat everything in a cref as if it's not a declaration context.
        private bool InDeclarationContext(SyntaxNode node)
            => !_inCref && node is ExpressionSyntax expr && SyntaxFacts.IsInNamespaceOrTypeContext(expr);

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Don't bother looking at the right side of A.B or A::B.  We will process those in
            // VisitQualifiedName, VisitAliasQualifiedName or VisitMemberAccessExpression.
            if (!node.IsRightSideOfDotOrArrowOrColonColon())
            {
                var inDeclaration = InDeclarationContext(node);

                // If we have an identifier, we would only ever replace it with an alias or a
                // predefined-type name.  Do a very quick syntactic check to even see if either of those
                // are possible.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithPredefinedType(node, identifier, inDeclaration, ref symbol))
                    return;

                if (TryReplaceWithAlias(node, identifier, inDeclaration, nameMustMatch: false, ref symbol))
                    return;
            }

            // No need to call `base.VisitIdentifierName()`.  identifier have no
            // children we need to process.
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            // Don't bother looking at the right side of A.G<> or A::G<>.  We will process those in
            // VisitQualifiedName, VisitAliasQualifiedName or VisitMemberAccessExpression.
            if (!node.IsRightSideOfDotOrColonColon())
            {
                var inDeclaration = InDeclarationContext(node);

                // A generic name is never a predefined type. So we don't need to check for that.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithAlias(node, identifier, inDeclaration, nameMustMatch: false, ref symbol))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier, inDeclaration, ref symbol))
                    return;
            }

            // Try to simplify the type arguments if we can't simplify anything else.
            this.Visit(node.TypeArgumentList);
        }

        private bool VisitAnyQualifiedName(SyntaxNode node)
        {
            var inDeclaration = InDeclarationContext(node);

            var (left, right) = TryGetPartsOfQualifiedName(node).Value;

            // We have a qualified name (like A.B).  Check and see if 'B' is the name of
            // predefined type, or if there's something aliased to the name B.
            var identifier = right.Identifier.ValueText;
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, inDeclaration, ref symbol))
                return true;

            if (TryReplaceWithAlias(node, identifier, inDeclaration, nameMustMatch: false, ref symbol))
                return true;

            if (TryReplaceWithNullable(node, identifier, inDeclaration, ref symbol))
                return true;

            // Wasn't predefined or an alias.  See if we can just reduce it to 'B'.
            if (TryReplaceExprWithRightSide(node, identifier, left, right, inDeclaration, ref symbol))
                return true;

            return false;
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (VisitAnyQualifiedName(node))
                return;

            // we could have something like `A.B.C<D.E>`.  We want to visit both A.B to see if that
            // can be simplified as well as D.E.
            base.VisitQualifiedName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (VisitAnyQualifiedName(node))
                return;

            // We still want to simplify the right side of this name.  We might have something
            // like `A::G<X.Y>` which could be simplified to `A::G<Y>`.
            this.Visit(node.Name);
        }

        public override void VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            if (VisitAnyQualifiedName(node))
                return;

            base.VisitQualifiedCref(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Look for one of the following:
            //
            //      A.B.C
            //      X::A.B.C
            //      A.B.C<X.Y>
            //
            // In these cases we want to see if we can simplify what's on the left of 'C'.
            // In case we're in a `nameof` we can simplify the entire expr.
            //
            //      nameof(A.B.C)

            // To be able to simplify, we have to only contain other member-accesses or
            // alias-qualified names.
            if (IsSimplifiableMemberAccess(node))
            {
                // If we have `nameof(A.B.C)` then we can potentially simplify this just to
                // 'C'.
                if (node.IsNameOfArgumentExpression() &&
                    SimplifyMemberAccessInNameofExpression(node))
                {
                    return;
                }

                if (SimplifyExpressionOfMemberAccessExpression(node.Expression))
                {
                    return;
                }
            }

            base.VisitMemberAccessExpression(node);
        }

        private bool SimplifyExpressionOfMemberAccessExpression(ExpressionSyntax node)
        {
            // Can be one of:
            //
            //  A.B         expr is identifier
            //  A.B.C       expr is member access
            //  A::B.C      expr is alias qualified name
            //  A<T>.B      expr is generic name.

            // We could end up simplifying to a predefined type or alias.  We can't simplify
            // to nullable as `A?.B` is not a legal member access for `Nullable<A>.B`
            var identifier = node.GetRightmostName().Identifier.ValueText;
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, inDeclaration: false, ref symbol))
                return true;

            if (TryReplaceWithAlias(node, identifier, inDeclaration: false, nameMustMatch: false, ref symbol))
                return true;

            var parts = TryGetPartsOfQualifiedName(node);
            if (parts != null &&
                TryReplaceExprWithRightSide(node, identifier,
                    parts.Value.left, parts.Value.right,
                    inDeclaration: false, ref symbol))
            {
                return true;
            }

            return false;
        }

        private bool SimplifyMemberAccessInNameofExpression(MemberAccessExpressionSyntax node)
        {
            // in a nameof(...) expr, we cannot simplify to predefined types, or nullable. We can
            // simplify to an alias if it has the same name as us.
            INamespaceOrTypeSymbol symbol = null;
            var memberName = node.Name.Identifier.ValueText;
            if (TryReplaceWithAlias(node, memberName,
                    inDeclaration: false, nameMustMatch: true, ref symbol))
            {
                return true;
            }

            if (TryReplaceExprWithRightSide(node, memberName,
                    node.Expression, node.Name, inDeclaration: false, ref symbol))
            {
                return true;
            }

            return false;
        }

        private bool IsSimplifiableMemberAccess(MemberAccessExpressionSyntax node)
        {
            var current = node.Expression;
            while (current.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                current = ((MemberAccessExpressionSyntax)current).Expression;
                continue;
            }

            return current.Kind() == SyntaxKind.AliasQualifiedName ||
                   current.Kind() == SyntaxKind.IdentifierName ||
                   current.Kind() == SyntaxKind.GenericName;
        }

        private bool IsNameOfUsingDirective(QualifiedNameSyntax node, out UsingDirectiveSyntax usingDirective)
        {
            while (node.Parent is QualifiedNameSyntax parent)
                node = parent;

            usingDirective = node.Parent as UsingDirectiveSyntax;
            return usingDirective != null;
        }

        private INamespaceOrTypeSymbol GetNamespaceOrTypeSymbol(SyntaxNode node)
        {
            var symbolInfo = _semanticModel.GetSymbolInfo(node, _cancellationToken);

            // Don't offer if we have ambiguity involved.
            if (symbolInfo.CandidateSymbols.Length > 0)
                return null;

            return symbolInfo.Symbol as INamespaceOrTypeSymbol;
        }

        private bool AddAliasDiagnostic(SyntaxNode node, string alias, bool inDeclaration)
        {
            if (node is IdentifierNameSyntax identifier &&
                alias == identifier.Identifier.ValueText)
            {
                // No point simplifying an identifier to the same alias name.
                return false;
            }

            // If we're replacing a qualified name with an alias that is the same as
            // the RHS, then don't mark the entire type-syntax as being simplified.
            // Only mark the LHS.
            var parts = TryGetPartsOfQualifiedName(node);
            if (parts != null &&
                parts.Value.right is IdentifierNameSyntax identifier2 &&
                alias == identifier2.Identifier.ValueText)
            {
                return this.AddDiagnostic(parts.Value.left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
            }

            return this.AddDiagnostic(node.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
        }

        private bool TryReplaceWithAlias(
            SyntaxNode node, string typeName,
            bool inDeclaration, bool nameMustMatch, ref INamespaceOrTypeSymbol symbol)
        {
            // See if we actually have an alias to something with our name.
            if (!Peek(_aliasedSymbolNamesStack).Contains(typeName))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(node);
            if (symbol == null)
                return false;

            // Next, see if there's an alias in scope we can bind to.
            for (var i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var symbolToAlias = _aliasStack[i];
                if (symbolToAlias.TryGetValue(symbol, out var alias))
                {
                    if (nameMustMatch && alias != typeName)
                        continue;

                    var foundSymbols = LookupName(node, inDeclaration, alias);
                    foreach (var found in foundSymbols)
                    {
                        if (found is IAliasSymbol aliasSymbol && aliasSymbol.Target.Equals(symbol))
                        {
                            return AddAliasDiagnostic(node, alias, inDeclaration);
                        }
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithPredefinedType(
            SyntaxNode node, string typeName,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            if (inDeclaration && !_preferPredefinedTypeInDecl)
                return false;

            if (!inDeclaration && !_preferPredefinedTypeInMemberAccess)
                return false;

            if (s_predefinedTypeNames.Contains(typeName) &&
                !node.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(node);
                if (symbol is ITypeSymbol typeSymbol)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                    {
                        return this.AddDiagnostic(
                            node.Span, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId, inDeclaration);
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithNullable(
            SyntaxNode node, string typeName,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            // `int?` can only be used in a type-decl context.  i.e. it can't be used like 
            // `int?.Equals()`
            if (inDeclaration &&
                typeName == nameof(Nullable) &&
                !node.IsParentKind(SyntaxKind.UsingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(node);
                if (symbol is ITypeSymbol typeSymbol &&
                    typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return AddDiagnostic(node.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
                }
            }

            return false;
        }

        private (ExpressionSyntax left, SimpleNameSyntax right)? TryGetPartsOfQualifiedName(SyntaxNode node)
            => node switch
            {
                QualifiedNameSyntax qualifiedName => (qualifiedName.Left, qualifiedName.Right),
                MemberAccessExpressionSyntax memberAccess => (memberAccess.Expression, memberAccess.Name),
                AliasQualifiedNameSyntax aliasName => (aliasName.Alias, aliasName.Name),
                QualifiedCrefSyntax { Member: NameMemberCrefSyntax { Name: SimpleNameSyntax name } nameMember } qualifiedCref
                    => (qualifiedCref.Container, name),
                _ => default((ExpressionSyntax, SimpleNameSyntax)?),
            };

        private bool TryReplaceExprWithRightSide(
            SyntaxNode rootExpression, string identifier,
            ExpressionSyntax left, SimpleNameSyntax right,
            bool inDeclaration, ref INamespaceOrTypeSymbol symbol)
        {
            // We have a name like A.B or A::B.

            // First see if we even have a type/namespace in scope called 'B'.  If not,
            // there's nothing we need to do further.
            if (!Peek(_namesInScopeStack).Contains(identifier))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(rootExpression);
            if (symbol == null)
                return false;

            if (rootExpression is QualifiedNameSyntax qualifiedName &&
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
            var foundSymbols = LookupName(rootExpression, inDeclaration, identifier);
            foreach (var found in foundSymbols)
            {
                if (symbol.OriginalDefinition.Equals(found.OriginalDefinition))
                {
                    return AddDiagnostic(
                        left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
                }
            }

            // See if we're in the `Color Color` case.  i.e user may have written
            // `X.Color.Red`.  We need to retry binding `Color` as a decl here to 
            // see if we can simplify to that.
            if (!inDeclaration && IsColorColorCase(foundSymbols))
            {
                foundSymbols = LookupName(rootExpression, inDeclaration: true, identifier);
                foreach (var found in foundSymbols)
                {
                    if (symbol.OriginalDefinition.Equals(found.OriginalDefinition))
                    {
                        return AddDiagnostic(
                            left.Span, IDEDiagnosticIds.SimplifyNamesDiagnosticId, inDeclaration);
                    }
                }
            }

            return false;
        }

        private bool IsColorColorCase(ImmutableArray<ISymbol> foundSymbols)
        {
            if (foundSymbols.Length == 1)
            {
                var found = foundSymbols[0];
                if (found is IPropertySymbol property && found.Name == property.Type.Name)
                    return true;

                if (found is IFieldSymbol field && found.Name == field.Type.Name)
                    return true;
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
