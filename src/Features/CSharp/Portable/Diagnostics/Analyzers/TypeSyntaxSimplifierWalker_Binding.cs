// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    /// <summary>
    /// This walker sees if we can simplify types/namespaces that it encounters. Importantly, it
    /// only checks types/namespaces in contexts that are known to only allows types/namespaces only
    /// (i.e. declarations, casts, etc.).  It does not check general expression contexts.
    /// <para/>
    /// A core concept here is that we'd like to perform as little binding as possible. So this
    /// walker builds up information as it walks the tree (like what names are in scope) so it can
    /// avoid binding nodes it knows cannot be simplified at all.
    /// </summary>
    internal partial class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker
    {
        /// <summary>
        /// This is the root helper that all other TrySimplify methods in this type must call
        /// through once they think there is a good chance something is simplifiable.  It does the
        /// work of actually going through the real simplification system to validate that the
        /// simplification is legal and does not affect semantics.
        /// </summary>
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
            // one of our other top level Visit methods (like VisitQualifiedName).
            if (!node.IsRightSideOfDotOrArrowOrColonColon())
            {
                // If we have an identifier, we would only ever replace it with an alias or a
                // predefined-type name.
                var typeName = node.Identifier.ValueText!;
                if (TryReplaceWithPredefinedType(node, typeName))
                    return;

                if (TryReplaceWithAlias(node, typeName))
                    return;
            }

            // No need to call `base.VisitIdentifierName()`.  identifiers have no children we need
            // to process.
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            // Don't bother looking at the right side of A.G<> or A::G<>.  We will process those in
            // VisitQualifiedName, VisitAliasQualifiedName or VisitMemberAccessExpression.
            if (!node.IsRightSideOfDotOrColonColon())
            {
                // A generic name is never a predefined type. So we don't need to check for that.
                var identifier = node.Identifier.ValueText!;
                if (TryReplaceWithAlias(node, identifier))
                    return;

                // Might be a reference to `Nullable<T>` that we can replace with `T?`
                if (TryReplaceWithNullable(node, identifier))
                    return;
            }

            // See if we can remove the type argument list entirely from the generic name
            // i.e. `G<int>(0)` => `G(0)`.
            if (TrySimplifyTypeArgumentListInInvocation(node))
                return;

            // Even if the generic name itself wasn't simplifiable, its type arguments might be.
            // i.e. `G<System.Int32>` => `G<int>`.
            this.Visit(node.TypeArgumentList);
        }

        private bool TrySimplifyTypeArgumentListInInvocation(GenericNameSyntax node)
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

        private bool TrySimplifyQualifiedReferenceToNamespaceOrType(ExpressionSyntax node, string typeName)
        {
            // We have a qualified name (like A.B).

            // First Check and see if 'B' is the name of predefined type.
            if (TryReplaceWithPredefinedType(node, typeName))
                return true;

            // Then see if there's something aliased to the name B.
            if (TryReplaceWithAlias(node, typeName))
                return true;

            // Then see if they explicitly wrote out `A.Nullable<T>` and replace with T?
            if (TryReplaceWithNullable(node, typeName))
                return true;

            // Finally, see if we can just reduce it to 'B'.
            if (TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(node, typeName))
                return true;

            return false;
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (TrySimplifyQualifiedReferenceToNamespaceOrType(node, node.Right.Identifier.ValueText!))
                return;

            // we could have something like `A.B.C<D.E>`.  We want to visit both A.B to see if that
            // can be simplified as well as D.E.
            base.VisitQualifiedName(node);
        }

        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            if (TrySimplifyQualifiedReferenceToNamespaceOrType(node, node.Name.Identifier.ValueText!))
                return;

            // We still want to simplify the right side of this name.  We might have something
            // like `A::G<X.Y>` which could be simplified to `A::G<Y>`.
            this.Visit(node.Name);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (TrySimplifyBaseAccessExpression(node))
                return;

            var memberName = node.Name.Identifier.ValueText!;
            if (TrySimplifyPredefinedAccessExpression(node, memberName))
                return;

            // Look for one of the following:
            //
            //      A.B.C
            //      X::A.B.C
            //      A.B.C<X.Y>
            //
            // In these cases we want to see if we can simplify what's on the left of 'C'.
            //
            // To be able to simplify, we have to only contain other member-accesses or qualified
            // names.
            if (IsDottedSimpleNameSequence(node))
            {
                // The `A.B` part might be referring to type/namespace.  See if we can simplify
                // that. portion.
                if (TrySimplifyQualifiedReferenceToNamespaceOrType(node, memberName))
                    return;

                // See if we can just simplify to `C`.
                if (TrySimplifyStaticMemberAccessInScope(node, memberName))
                    return;

                // See if `A` can be simplified to a base type.
                if (TrySimplifyStaticMemberAccessThroughDerivedType(node))
                    return;
            }

            // Only need to visit the name if it is generic.
            if (node.Name.IsKind(SyntaxKind.GenericName, out GenericNameSyntax genericName))
                VisitGenericName(genericName);

            Visit(node.Expression);
        }

        private bool TrySimplifyPredefinedAccessExpression(MemberAccessExpressionSyntax node, string memberName)
        {
            // if we have a call like `object.Equals(...)` we may be able to reduce that to just
            // `Equals(...)` since `object` member are in scope within us.
            //
            // Also, predefined types might be in scope statically.  i.e. `using static System.String;`
            // Have to try to simplify to the member access call off of them if that's teh case.

            if (!node.Expression.IsKind(SyntaxKind.PredefinedType))
                return false;

            return TrySimplifyStaticMemberAccessInScope(node, memberName);
        }

        private bool TrySimplifyBaseAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression.Kind() != SyntaxKind.BaseExpression)
                return false;

            // We have `base.SomeMember(...)`.  This is potentially simplifiable to `SomeMember`.
            // However, this involves complex overload-instance-resolution logic.  So we fall-back
            // to the full simplification analysis system.
            return TrySimplify(node);
        }

        private bool TrySimplifyStaticMemberAccessInScope(MemberAccessExpressionSyntax node, string memberName)
        {
            // see if we can just access this member using it's name alone here.
            if (!_staticNamesInScope.Contains(memberName))
                return false;

            return TrySimplify(node);
        }

        private bool TrySimplifyStaticMemberAccessThroughDerivedType(MemberAccessExpressionSyntax node)
        {
            // We have `Y.Z`.  It might be that `Z` is a static member actually declared through a
            // base-type of `Y` called `X`.  This can be simplified to `X.Z`.
            //
            // To avoid having to call this on every `A.B` member access, we ensure that
            // there's actually a type called `A` somewhere in our compilation. This helps
            // avoid costly binding in many cases.

            var exprName = node.Expression.GetRightmostName();
            if (!_compilationTypeNames.Contains(exprName.Identifier.ValueText!))
                return false;

            // Member on the right of the dot needs to be a static member or another named type.
            var nameSymbol = _semanticModel.GetSymbolInfo(node.Name).Symbol;
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
            var containerSymbol = _semanticModel.GetSymbolInfo(node.Expression).Symbol;
            if (Equals(containerSymbol, nameSymbol.ContainingType))
                return false;

            return TrySimplify(node);
        }

        public override void VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            // A qualified cref could be many different types of things.  It could be referencing a
            // namespace, type or member (including static and instance members).  Because of this
            // we basically have no avenues for bailing early and we have to try out all possible
            // simplifications paths.
            if (TrySimplify(node))
                return;

            base.VisitQualifiedCref(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            // Check if we have an attribute like `[X.GooAttribute]`.  If so, try to simplify it to
            // just `[X.Goo]`
            var rightmostNameNode = node.Name.GetRightmostName();
            var rightmostName = rightmostNameNode.Identifier.ValueText!;
            if (rightmostName.HasAttributeSuffix(isCaseSensitive: true) &&
                TrySimplify(rightmostNameNode))
            {
                // Even if we simplify the name, we still want to descend into the arg-list for more
                // stuff to simplify.
                Visit(node.ArgumentList);
                return;
            }

            base.VisitAttribute(node);
        }

        private static bool IsNamedTypeOrStaticSymbol([NotNullWhen(true)] ISymbol? nameSymbol)
            => nameSymbol is INamedTypeSymbol || nameSymbol?.IsStatic == true;

        private bool IsDottedSimpleNameSequence(MemberAccessExpressionSyntax node)
        {
            var current = node.Expression;
            while (current.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax currentMember))
            {
                current = currentMember.Expression;
                continue;
            }

            return current.IsKind(SyntaxKind.AliasQualifiedName,
                                  SyntaxKind.IdentifierName,
                                  SyntaxKind.GenericName);
        }

        private bool TryReplaceWithAlias(ExpressionSyntax node, string typeName)
        {
            // See if we actually have an alias to something with our name.
            if (!_aliasedSymbolNames.Contains(typeName))
                return false;

            return TrySimplify(node);
        }

        private bool TryReplaceWithPredefinedType(ExpressionSyntax node, string typeName)
        {
            // No point even checking this if the user doesn't like using predefined types.
            if (!_preferPredefinedTypeInDecl && !_preferPredefinedTypeInMemberAccess)
                return false;

            // Only check if the name actually is the name of a built-in type.
            if (!s_predefinedTypeNames.Contains(typeName))
                return false;

            return TrySimplify(node);
        }

        private bool TryReplaceWithNullable(ExpressionSyntax node, string typeName)
        {
            // Only both checking this if the user referenced `Nullable`.
            if (typeName != nameof(Nullable))
                return false;

            return TrySimplify(node);
        }

        private bool TypeReplaceQualifiedReferenceToNamespaceOrTypeWithName(ExpressionSyntax root, string typeName)
        {
            // We have a name like A.B or A::B.
            //
            // First see if we even have a type/namespace in scope called 'B'.  If not,
            // there's nothing we need to do further.
            if (!_declarationNamesInScope.Contains(typeName))
                return false;

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
