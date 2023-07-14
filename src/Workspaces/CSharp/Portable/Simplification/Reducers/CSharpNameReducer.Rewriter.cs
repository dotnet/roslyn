// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpNameReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
            }

            public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitPredefinedType(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitAliasQualifiedName(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitQualifiedName(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitMemberAccessExpression(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitIdentifierName(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitGenericName(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitQualifiedCref(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitArrayType(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitNullableType(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitTupleType(TupleTypeSyntax node)
            {
                var oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyNode(
                    node,
                    newNode: base.VisitTupleType(node),
                    simplifier: s_simplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var isOrAsNode = node.Kind() is SyntaxKind.AsExpression or SyntaxKind.IsExpression;

                var result = (ExpressionSyntax)base.VisitBinaryExpression(node);

                if (result != node && isOrAsNode)
                {
                    // In order to handle cases in which simplifying a name would result in code
                    // that parses different, we pre-emptively add parentheses that will be
                    // simplified away later.
                    //
                    // For example, this code...
                    //
                    //     var x = y as Nullable<int> + 1;
                    //
                    // ...should simplify as...
                    //
                    //     var x = (y as int?) + 1;
                    return result.Parenthesize().WithAdditionalAnnotations(Formatter.Annotation);
                }

                return result;
            }
        }
    }
}
