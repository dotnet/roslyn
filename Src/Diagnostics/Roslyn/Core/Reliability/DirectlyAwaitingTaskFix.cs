// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CodeFixes
{
    public abstract class DirectlyAwaitingTaskFix<TExpressionSyntax> : CodeFixProvider where TExpressionSyntax : SyntaxNode
    {
        protected abstract TExpressionSyntax FixExpression(TExpressionSyntax syntaxNode, CancellationToken cancellationToken);
        protected abstract string FalseLiteralString { get; }

        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return new[] { RoslynDiagnosticIds.DirectlyAwaitingTaskAnalyzerRuleId };
        }

        public sealed override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return GetFixesCore(document, root, diagnostics, cancellationToken);
        }

        private IEnumerable<CodeAction> GetFixesCore(Document document, SyntaxNode root, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var expression = root.FindNode(diagnostic.Location.SourceSpan) as TExpressionSyntax;

                if (expression != null)
                {
                    yield return new MyCodeAction(
                        "Append .ConfigureAwait(" + FalseLiteralString + ")",
                        c => GetFix(document, root, expression, c));
                }
            }
        }

        private Task<Document> GetFix(Document document, SyntaxNode root, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            // Rewrite the expression to include a .ConfigureAwait() after it. We reattach trailing trivia to the end.
            // This is especially important for VB, as the end-of-line may be in the trivia
            var fixedExpression = FixExpression(expression.WithoutTrailingTrivia(), cancellationToken)
                                      .WithTrailingTrivia(expression.GetTrailingTrivia());
            var fixedDocument = document.WithSyntaxRoot(root.ReplaceNode(expression, fixedExpression));
            return Simplifier.ReduceAsync(fixedDocument, fixedExpression.FullSpan, cancellationToken: cancellationToken);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
