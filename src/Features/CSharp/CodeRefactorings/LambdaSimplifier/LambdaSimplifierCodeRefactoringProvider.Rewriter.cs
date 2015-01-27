// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.LambdaSimplifier
{
    internal partial class LambdaSimplifierCodeRefactoringProvider
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly LambdaSimplifierCodeRefactoringProvider codeIssueProvider;
            private readonly SemanticDocument document;
            private readonly Func<SyntaxNode, bool> predicate;
            private readonly CancellationToken cancellationToken;

            public Rewriter(
                LambdaSimplifierCodeRefactoringProvider codeIssueProvider,
                SemanticDocument document,
                Func<SyntaxNode, bool> predicate,
                CancellationToken cancellationToken)
            {
                this.codeIssueProvider = codeIssueProvider;
                this.document = document;
                this.predicate = predicate;
                this.cancellationToken = cancellationToken;
            }

            private ExpressionSyntax SimplifyInvocation(InvocationExpressionSyntax invocation)
            {
                var expression = invocation.Expression;
                var memberAccess = expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var symbolMap = SemanticMap.From(document.SemanticModel, memberAccess.Expression, cancellationToken);
                    var anySideEffects = symbolMap.AllReferencedSymbols.Any(s =>
                        s.Kind == SymbolKind.Method || s.Kind == SymbolKind.Property);

                    if (anySideEffects)
                    {
                        var annotation = WarningAnnotation.Create("Warning: Expression may have side effects. Code meaning may change.");
                        expression = expression.ReplaceNode(memberAccess.Expression, memberAccess.Expression.WithAdditionalAnnotations(annotation));
                    }
                }

                return expression.Parenthesize()
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                if (predicate(node) && CanSimplify(document, node, cancellationToken))
                {
                    var invocation = TryGetInvocationExpression(node.Body);
                    if (invocation != null)
                    {
                        return SimplifyInvocation(invocation);
                    }
                }

                return base.VisitSimpleLambdaExpression(node);
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                if (predicate(node) && CanSimplify(document, node, cancellationToken))
                {
                    var invocation = TryGetInvocationExpression(node.Body);
                    if (invocation != null)
                    {
                        return SimplifyInvocation(invocation);
                    }
                }

                return base.VisitParenthesizedLambdaExpression(node);
            }
        }
    }
}
