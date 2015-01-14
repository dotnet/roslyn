// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
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

            public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitPredefinedType(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitAliasQualifiedName(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitQualifiedName(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitMemberAccessExpression(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitIdentifierName(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitGenericName(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = SimplifyExpression(
                    node,
                    newNode: base.VisitQualifiedCref(node),
                    simplifier: SimplifyName);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = base.VisitArrayType(node);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
            }

            public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
            {
                bool oldAlwaysSimplify = this.alwaysSimplify;
                if (!this.alwaysSimplify)
                {
                    this.alwaysSimplify = node.HasAnnotation(Simplifier.Annotation);
                }

                var result = base.VisitNullableType(node);

                this.alwaysSimplify = oldAlwaysSimplify;

                return result;
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
