// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters.
    /// Importantly, it only checks types/namespaces in contexts that are known to
    /// only allows types/namespaces only (i.e. declarations, casts, etc.).  It does
    /// not check general expression contexts.
    /// </summary>
    internal partial class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker, IDisposable
    {
        private bool TrySimplify(SyntaxNode node)
        {
            if (!_analyzer.TrySimplify(_semanticModel, node, out var diagnostic, _optionSet, _cancellationToken))
                return false;

            this.Diagnostics.Add(diagnostic);
            return true;
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Don't bother looking at the right side of A.B or A::B.  We will process those in
            // VisitQualifiedName, VisitAliasQualifiedName or VisitMemberAccessExpression.
            if (!node.IsRightSideOfDotOrArrowOrColonColon())
            {
                // If we have an identifier, we would only ever replace it with an alias or a
                // predefined-type name.  Do a very quick syntactic check to even see if either of those
                // are possible.
                var identifier = node.Identifier.ValueText;
                if (TryReplaceWithPredefinedType(node, identifier))
                    return;

                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithAlias(node, identifier, ref symbol))
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
                // A generic name is never a predefined type. So we don't need to check for that.
                var identifier = node.Identifier.ValueText;
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithAlias(node, identifier, ref symbol))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier))
                    return;
            }

            if (TrySimplifyTypeArgumentList(node))
                return;

            // Try to simplify the type arguments if we can't simplify anything else.
            this.Visit(node.TypeArgumentList);
        }

        private bool TrySimplifyTypeArgumentList(GenericNameSyntax node)
        {
            // If we have a generic method call (like `Goo<int>(...)`), see if we can replace this
            // with a call it like so `Goo(...)`.
            if (!IsNameOfInvocation(node))
                return false;

            return TrySimplify(node);
        }

        private static bool IsNameOfInvocation(GenericNameSyntax name)
        {
            if (name.IsExpressionOfInvocation())
                return true;

            if (name.IsAnyMemberAccessExpressionName())
            {
                var memberAccess = name.Parent as ExpressionSyntax;
                return memberAccess.IsExpressionOfInvocation();
            }

            return false;
        }

        private bool SimplifyQualifiedReferenceToNamespaceOrType(SyntaxNode node)
        {
            var (left, right) = TryGetPartsOfQualifiedName(node).Value;

            // We have a qualified name (like A.B).  Check and see if 'B' is the name of
            // predefined type, or if there's something aliased to the name B.
            var identifier = right.Identifier.ValueText;
            if (TryReplaceWithPredefinedType(node, identifier))
                return true;

            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithAlias(node, identifier, ref symbol))
                return true;

            if (TryReplaceWithNullable(node, identifier))
                return true;

            // Wasn't predefined or an alias.  See if we can just reduce it to 'B'.
            if (TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, right, ref symbol))
                return true;

            return false;
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (SimplifyQualifiedReferenceToNamespaceOrType(node))
                return;

            // we could have something like `A.B.C<D.E>`.  We want to visit both A.B to see if that
            // can be simplified as well as D.E.
            base.VisitQualifiedName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (SimplifyQualifiedReferenceToNamespaceOrType(node))
                return;

            // We still want to simplify the right side of this name.  We might have something
            // like `A::G<X.Y>` which could be simplified to `A::G<Y>`.
            this.Visit(node.Name);
        }

        public override void VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            // A qualified cref could be referencing a namespace, type or member.  Try simplifying
            // all of those possibilities.

            if (SimplifyQualifiedReferenceToNamespaceOrType(node))
                return;

            if (SimplifyStaticMemberAccess(node))
                return;

            base.VisitQualifiedCref(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (TrySimplifyBaseAccessExpression(node))
                return;

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
                if (SimplifyMemberAccessInNameofExpression(node))
                    return;

                if (SimplifyExpressionOfMemberAccessExpression(node.Expression))
                    return;

                if (SimplifyStaticMemberAccess(node))
                    return;
            }

            base.VisitMemberAccessExpression(node);
        }

        private bool SimplifyStaticMemberAccess(SyntaxNode node)
        {
            var parts = TryGetPartsOfQualifiedName(node);
            if (parts == null)
                return false;

            var (left, right) = parts.Value;

            if (SimplifyStaticMemberAccessInScope(node, right))
                return true;

            if (SimplifyStaticMemberAccessThroughDerivedType(node, left, right))
                return true;

            return false;
        }

        private bool TrySimplifyBaseAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression.Kind() != SyntaxKind.BaseExpression)
                return false;

            // We have `base.SomeMember(...)`.  This is potentially simplifiable to `SomeMember` if
            // certain conditions hold. First, `SomeMember` has to at least bind to the exact same
            // member as before.  However, that's still not sufficient as the runtime behavior might
            // be different (since base.SomeMember is a non-virtual call, and .SomeMember might not
            // be).  To ensure we'll have the same runtime behavior either the member must be
            // non-virtual, or we must be in a sealed type.  In the latter case, there can't be any
            // derivations of us that are overriding the virtual member.

            // Also, because we are making an instance call (and not a static), we have to use the
            // more complex validation system that ensures no changed semantics.  That's because
            // looking up through an instance may involve far more complex overload resolution (i.e.
            // because of different instance members in scope, or extension methods).
            return TrySimplify(node);
        }

        private bool SimplifyStaticMemberAccessInScope(
            SyntaxNode node, SimpleNameSyntax right)
        {
            // see if we can just access this member using it's name alone here.
            var memberName = right.Identifier.ValueText;
            if (!Peek(_staticNamesInScopeStack).Contains(memberName))
                return false;

            return TrySimplify(node);
        }

        private bool SimplifyStaticMemberAccessThroughDerivedType(
            SyntaxNode node, ExpressionSyntax left, SimpleNameSyntax right)
        {
            // Member on the right of the dot needs to be a static member or another named type.
            var nameSymbol = _semanticModel.GetSymbolInfo(right).Symbol;
            if (!IsNamedTypeOrStaticSymbol(nameSymbol))
                return false;

            if (nameSymbol.ContainingType == null)
                return false;

            // Don't simplify a container into a generic type.  Users do not consider it to actually
            // be simpler to go from D.P to B<X>.P
            if (nameSymbol.ContainingType.Arity > 0)
                return false;

            // If the user is already accessing the static through its containing type, there's
            // nothing we need to simplify to.
            var containerSymbol = _semanticModel.GetSymbolInfo(left).Symbol;
            if (Equals(containerSymbol, nameSymbol.ContainingType))
                return false;

            return TrySimplify(node);
        }

        private static bool IsNamedTypeOrStaticSymbol(ISymbol nameSymbol)
            => nameSymbol is INamedTypeSymbol || nameSymbol?.IsStatic == true;

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
            var rightmostName = node.GetRightmostName();
            if (rightmostName == null)
                return false;

            var identifier = rightmostName.Identifier.ValueText;
            if (TryReplaceWithPredefinedType(node, identifier))
                return true;

            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithAlias(node, identifier, ref symbol))
                return true;

            var parts = TryGetPartsOfQualifiedName(node);
            if (parts != null &&
                TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, parts.Value.right, ref symbol))
            {
                return true;
            }

            return false;
        }

        private bool SimplifyMemberAccessInNameofExpression(MemberAccessExpressionSyntax node)
        {
            if (!node.IsNameOfArgumentExpression())
                return false;

            // in a nameof(...) expr, we cannot simplify to predefined types, or nullable. We can
            // simplify to an alias if it has the same name as us.
            INamespaceOrTypeSymbol symbol = null;
            var memberName = node.Name.Identifier.ValueText;
            if (TryReplaceWithAlias(node, memberName, ref symbol))
                return true;

            if (TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, node.Name, ref symbol))
                return true;

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

            if (current.Kind() == SyntaxKind.AliasQualifiedName ||
                current.Kind() == SyntaxKind.IdentifierName ||
                current.Kind() == SyntaxKind.GenericName)
            {
                return true;
            }

            if (current.IsKind(SyntaxKind.PredefinedType, out PredefinedTypeSyntax predefinedType) &&
                predefinedType.Keyword.Kind() == SyntaxKind.ObjectKeyword)
            {
                return true;
            }

            return false;
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

        private bool TryReplaceWithAlias(
            SyntaxNode node, string typeName, ref INamespaceOrTypeSymbol symbol)
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
                    return TrySimplify(node);
                }
            }

            return false;
        }

        private bool TryReplaceWithPredefinedType(SyntaxNode node, string typeName)
        {
            if (!_preferPredefinedTypeInDecl && !_preferPredefinedTypeInMemberAccess)
                return false;

            if (!s_predefinedTypeNames.Contains(typeName))
                return false;

            return TrySimplify(node);
        }

        private bool IsNameOfArgumentExpression(SyntaxNode node)
            => node is ExpressionSyntax expr && expr.IsNameOfArgumentExpression();

        private bool TryReplaceWithNullable(SyntaxNode node, string typeName)
        {
            if (typeName != nameof(Nullable))
                return false;

            return TrySimplify(node);
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

        private bool TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(
            SyntaxNode root, SimpleNameSyntax right, ref INamespaceOrTypeSymbol symbol)
        {
            // We have a name like A.B or A::B.

            var rightIdentifier = right.Identifier.ValueText;

            // First see if we even have a type/namespace in scope called 'B'.  If not,
            // there's nothing we need to do further.
            if (!Peek(_declarationNamesInScopeStack).Contains(rightIdentifier))
                return false;

            if (root is QualifiedNameSyntax qualifiedName &&
                IsNameOfUsingDirective(qualifiedName, out var usingDirective))
            {
                symbol ??= GetNamespaceOrTypeSymbol(root);
                if (symbol == null)
                    return false;

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
                if (usingDirective.StaticKeyword != default || usingDirective.Alias != default)
                    return false;
            }

            return TrySimplify(root);
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
