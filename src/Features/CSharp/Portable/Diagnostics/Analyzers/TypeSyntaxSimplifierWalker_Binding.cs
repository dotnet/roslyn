// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
        private static bool IsInNamespaceOrTypeContext(SyntaxNode location)
            => location is ExpressionSyntax expr && SyntaxFacts.IsInNamespaceOrTypeContext(expr);

        private ImmutableArray<ISymbol> LookupName(SyntaxNode location, bool isNamespaceOrTypeContext, string name)
            => isNamespaceOrTypeContext
                ? _semanticModel.LookupNamespacesAndTypes(location.SpanStart, name: name)
                : _semanticModel.LookupSymbols(location.SpanStart, name: name);

        // For back-compat, we treat everything in a cref as if it's not a declaration context.
        private bool InDeclarationContext(SyntaxNode node)
            => !_inCref && IsInNamespaceOrTypeContext(node);

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
                INamespaceOrTypeSymbol symbol = null;
                if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                    return;

                if (TryReplaceWithAlias(node, identifier, nameMustMatch: false, ref symbol))
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
                if (TryReplaceWithAlias(node, identifier, nameMustMatch: false, ref symbol))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier, ref symbol))
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
            if (IsNameOfInvocation(node))
            {
                var replacementNode = SyntaxFactory.IdentifierName(node.Identifier);
                if (node.CanReplaceWithReducedName(replacementNode, _semanticModel, _cancellationToken))
                    return this.AddDiagnostic(node.TypeArgumentList, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
            }

            return false;
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
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                return true;

            if (TryReplaceWithAlias(node, identifier, nameMustMatch: false, ref symbol))
                return true;

            if (TryReplaceWithNullable(node, identifier, ref symbol))
                return true;

            // Wasn't predefined or an alias.  See if we can just reduce it to 'B'.
            if (TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, left, right, IDEDiagnosticIds.SimplifyNamesDiagnosticId, ref symbol))
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

            if (SimplifyStaticMemberAccessInScope(node, left, right))
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
            if (!node.CanReplaceWithReducedName(node.Name, _semanticModel, _cancellationToken))
                return false;

            return this.AddDiagnostic(node.Expression, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId);
        }

        private bool SimplifyStaticMemberAccessInScope(
            SyntaxNode node, ExpressionSyntax left, SimpleNameSyntax right)
        {
            var memberNameNode = right;

            // see if we can just access this member using it's name alone here.
            var memberName = memberNameNode.Identifier.ValueText;
            if (!Peek(_staticNamesInScopeStack).Contains(memberName))
                return false;

            var nameSymbol = _semanticModel.GetSymbolInfo(memberNameNode).Symbol;
            if (!IsNamedTypeOrStaticSymbol(nameSymbol))
                return false;

            var foundSymbols = LookupName(node, isNamespaceOrTypeContext: false, memberName);
            return AddMatches(nameSymbol, foundSymbols, memberNameNode.Arity,
                left, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId);
        }

        private bool AddMatches(
            ISymbol nameSymbol, ImmutableArray<ISymbol> foundSymbols, int arity,
            SyntaxNode diagnosticLocation, string diagnoticId)
        {
            using var matches = SharedPools.Default<List<ISymbol>>().GetPooledObject();

            foreach (var found in foundSymbols)
            {
                if (!SkipArityCheck(found, arity) &&
                    found.GetArity() != arity)
                {
                    continue;
                }

                matches.Object.Add(found);
            }

            if (matches.Object.Count == 0)
                return false;

            if (matches.Object.Count >= 2)
            {
                // It's only ok to get multiple results if we're getting method overloads.
                if (matches.Object[0] is IMethodSymbol)
                {
                    foreach (var match in matches.Object)
                    {
                        if (IsMatch(nameSymbol, match))
                            return this.AddDiagnostic(diagnosticLocation, diagnoticId);
                    }
                }

                return false;
            }

            Debug.Assert(matches.Object.Count == 1);
            if (IsMatch(nameSymbol, matches.Object[0]))
                return this.AddDiagnostic(diagnosticLocation, diagnoticId);

            return false;
        }

        private bool IsMatch(ISymbol nameSymbol, ISymbol match)
        {
            return Equals(nameSymbol.OriginalDefinition, match.OriginalDefinition) &&
                   Equals(nameSymbol.ContainingSymbol, match.ContainingSymbol);
        }

        private bool SimplifyStaticMemberAccessThroughDerivedType(
            SyntaxNode node, ExpressionSyntax left, SimpleNameSyntax right)
        {
            var memberNameNode = right;

            // Member on the right of the dot needs to be a static member or another named type.
            var nameSymbol = _semanticModel.GetSymbolInfo(memberNameNode).Symbol;
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

            return this.AddDiagnostic(left, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId);
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
            INamespaceOrTypeSymbol symbol = null;
            if (TryReplaceWithPredefinedType(node, identifier, ref symbol))
                return true;

            if (TryReplaceWithAlias(node, identifier, nameMustMatch: false, ref symbol))
                return true;

            var parts = TryGetPartsOfQualifiedName(node);
            if (parts != null &&
                TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, parts.Value.left, parts.Value.right, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, ref symbol))
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
            if (TryReplaceWithAlias(node, memberName, nameMustMatch: true, ref symbol))
                return true;

            if (TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, node.Expression, node.Name, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, ref symbol))
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

        private bool AddAliasDiagnostic(SyntaxNode node, string alias)
        {
            if (node is IdentifierNameSyntax identifier &&
                alias == identifier.Identifier.ValueText)
            {
                // No point simplifying an identifier to the same alias name.
                return false;
            }

            var diagnosticId = node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                ? IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId
                : IDEDiagnosticIds.SimplifyNamesDiagnosticId;

            // If we're replacing a qualified name with an alias that is the same as
            // the RHS, then don't mark the entire type-syntax as being simplified.
            // Only mark the LHS.
            var parts = TryGetPartsOfQualifiedName(node);
            if (parts != null &&
                parts.Value.right is IdentifierNameSyntax identifier2 &&
                alias == identifier2.Identifier.ValueText)
            {
                return this.AddDiagnostic(parts.Value.left, diagnosticId);
            }

            return this.AddDiagnostic(node, diagnosticId);
        }

        private bool TryReplaceWithAlias(
            SyntaxNode node, string typeName, bool nameMustMatch, ref INamespaceOrTypeSymbol symbol)
        {
            // See if we actually have an alias to something with our name.
            if (!Peek(_aliasedSymbolNamesStack).Contains(typeName))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(node);
            if (symbol == null)
                return false;

            // Next, see if there's an alias in scope we can bind to.
            var isNamespaceOrTypeContext = IsInNamespaceOrTypeContext(node);
            for (var i = _aliasStack.Count - 1; i >= 0; i--)
            {
                var symbolToAlias = _aliasStack[i];
                if (symbolToAlias.TryGetValue(symbol, out var alias))
                {
                    if (nameMustMatch && alias != typeName)
                        continue;

                    var foundSymbols = LookupName(node, isNamespaceOrTypeContext, alias);
                    foreach (var found in foundSymbols)
                    {
                        if (found is IAliasSymbol aliasSymbol && aliasSymbol.Target.Equals(symbol))
                            return AddAliasDiagnostic(node, alias);
                    }
                }
            }

            return false;
        }

        private bool TryReplaceWithPredefinedType(
            SyntaxNode node, string typeName, ref INamespaceOrTypeSymbol symbol)
        {
            var inDeclaration = InDeclarationContext(node);
            if (inDeclaration && !_preferPredefinedTypeInDecl)
                return false;

            if (!inDeclaration && !_preferPredefinedTypeInMemberAccess)
                return false;

            if (s_predefinedTypeNames.Contains(typeName) &&
                !node.IsParentKind(SyntaxKind.UsingDirective) &&
                !IsNameOfArgumentExpression(node))
            {
                symbol ??= GetNamespaceOrTypeSymbol(node);
                if (symbol is ITypeSymbol typeSymbol)
                {
                    var specialTypeKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(typeSymbol.SpecialType);
                    if (specialTypeKind != SyntaxKind.None)
                    {
                        return this.AddDiagnostic(node, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);
                    }
                }
            }

            return false;
        }

        private bool IsNameOfArgumentExpression(SyntaxNode node)
            => node is ExpressionSyntax expr && expr.IsNameOfArgumentExpression();

        private bool TryReplaceWithNullable(
            SyntaxNode node, string typeName, ref INamespaceOrTypeSymbol symbol)
        {
            // `int?` can only be used in a type-decl context.  i.e. it can't be used like 
            // `int?.Equals()`
            if (typeName == nameof(Nullable) &&
                !node.IsParentKind(SyntaxKind.UsingDirective) &&
                IsInNamespaceOrTypeContext(node))
            {
                symbol ??= GetNamespaceOrTypeSymbol(node);
                if (symbol is ITypeSymbol typeSymbol &&
                    typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return AddDiagnostic(node, IDEDiagnosticIds.SimplifyNamesDiagnosticId);
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

        private bool TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(
            SyntaxNode root, ExpressionSyntax left, SimpleNameSyntax right,
            string diagnosticId, ref INamespaceOrTypeSymbol symbol)
        {
            // We have a name like A.B or A::B.

            var rightIdentifier = right.Identifier.ValueText;

            // First see if we even have a type/namespace in scope called 'B'.  If not,
            // there's nothing we need to do further.
            if (!Peek(_declarationNamesInScopeStack).Contains(rightIdentifier))
                return false;

            symbol ??= GetNamespaceOrTypeSymbol(root);
            if (symbol == null)
                return false;

            if (root is QualifiedNameSyntax qualifiedName &&
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
                if (usingDirective.StaticKeyword != default || usingDirective.Alias != default)
                    return false;
            }

            // Now try to bind just 'B' in our current location.  If it binds to 'A.B' then we can
            // reduce to just that name.
            var isNamespaceOrTypeContext = IsInNamespaceOrTypeContext(root);
            var foundSymbols = LookupName(root, isNamespaceOrTypeContext, rightIdentifier);
            var rightArity = right.Arity;
            if (AddMatches(symbol, foundSymbols, rightArity, left, diagnosticId))
                return true;

            // See if we're in the `Color Color` case.  i.e user may have written `X.Color.Red`.  We
            // need to retry binding `Color` as a decl here to see if we can simplify to that.
            //
            // From the spec:
            //
            // In a member access of the form E.I, if E is a single identifier, and if the meaning
            // of E as a simple_name (Simple names) is a constant, field, property, local variable,
            // or parameter with the same type as the meaning of E as a type_name (Namespace and
            // type names), then both possible meanings of E are permitted

            if (!isNamespaceOrTypeContext &&
                rightArity == 0 &&
                IsColorColorCase(foundSymbols))
            {
                if (root.Parent is MemberAccessExpressionSyntax ||
                    root.Parent is QualifiedCrefSyntax)
                {
                    foundSymbols = LookupName(root, isNamespaceOrTypeContext: true, rightIdentifier);
                    if (foundSymbols.Length == 1 &&
                        AddMatches(symbol, foundSymbols, rightArity, left, diagnosticId))
                        return true;
                }
            }

            return false;
        }

        private static bool SkipArityCheck(ISymbol found, int arity)
            => found is IMethodSymbol && arity == 0;

        private bool IsColorColorCase(ImmutableArray<ISymbol> foundSymbols)
        {
            if (foundSymbols.Length == 1)
            {
                var found = foundSymbols[0];
                return found switch
                {
                    IPropertySymbol property => found.Name == property.Type.Name,
                    IFieldSymbol field => found.Name == field.Type.Name,
                    ILocalSymbol local => found.Name == local.Type.Name,
                    IParameterSymbol parameter => found.Name == parameter.Type.Name,
                    _ => false,
                };
            }

            return false;
        }

        private bool AddDiagnostic(SyntaxNode node, string diagnosticId)
        {
            this.Diagnostics.Add(CSharpSimplifyTypeNamesDiagnosticAnalyzer.CreateDiagnostic(
                _semanticModel, _optionSet, node.Span, diagnosticId, InDeclarationContext(node)));
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
