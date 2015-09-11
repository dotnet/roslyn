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
            private readonly LambdaSimplifierCodeRefactoringProvider _codeIssueProvider;
            private readonly SemanticDocument _document;
            private readonly Func<SyntaxNode, bool> _predicate;
            private readonly CancellationToken _cancellationToken;

            public Rewriter(
                LambdaSimplifierCodeRefactoringProvider codeIssueProvider,
                SemanticDocument document,
                Func<SyntaxNode, bool> predicate,
                CancellationToken cancellationToken)
            {
                _codeIssueProvider = codeIssueProvider;
                _document = document;
                _predicate = predicate;
                _cancellationToken = cancellationToken;
            }

            private ExpressionSyntax SimplifyInvocation(InvocationExpressionSyntax invocation)
            {
                var expression = invocation.Expression;
                var memberAccess = expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var symbolMap = SemanticMap.From(_document.SemanticModel, memberAccess.Expression, _cancellationToken);
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
                if (_predicate(node) && CanSimplify(_document, node, _cancellationToken))
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
                if (_predicate(node) && CanSimplify(_document, node, _cancellationToken))
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
