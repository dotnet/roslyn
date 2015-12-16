// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpNameReducer
    {
        private class Rewriter : AbstractExpressionRewriter
        {
            public Rewriter(OptionSet optionSet, CancellationToken cancellationToken)
                : base(optionSet, cancellationToken)
            {
            }

            private SyntaxNode SimplifyVisit<TExpression>(
                TExpression node,
                Func<TExpression, SyntaxNode> baseVisit)
                where TExpression : SyntaxNode
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                this.alwaysSimplify |= node.HasAnnotation(Simplifier.Annotation);

                var result = baseVisit(node);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            private SyntaxNode SimplifyExpression<TExpression>(
                TExpression node,
                Func<TExpression, SyntaxNode> baseVisit)
                where TExpression : SyntaxNode
            {
                return SimplifyVisit(node, n => SimplifyExpression(n, newNode: baseVisit(n), simplifier: SimplifyName));
            }

            public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
            {
                return SimplifyExpression(node, base.VisitPredefinedType);
            }

            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                return SimplifyExpression(node, base.VisitAliasQualifiedName);
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                return SimplifyExpression(node, base.VisitQualifiedName);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                return SimplifyExpression(node, base.VisitMemberAccessExpression);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                return SimplifyExpression(node, base.VisitIdentifierName);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                return SimplifyExpression(node, base.VisitGenericName);
            }

            public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node)
            {
                return SimplifyExpression(node, base.VisitQualifiedCref);
            }

            public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
            {
                return SimplifyVisit(node, base.VisitArrayType);
            }

            public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
            {
                return SimplifyVisit(node, base.VisitNullableType);
            }

            public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                bool isOrAsNode = node.Kind() == SyntaxKind.AsExpression || node.Kind() == SyntaxKind.IsExpression;

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
