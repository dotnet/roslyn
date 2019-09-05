// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class AsyncAwaitHighlighter : AbstractKeywordHighlighter
    {
        [ImportingConstructor]
        public AsyncAwaitHighlighter()
        {
        }

        protected override bool IsHighlightableNode(SyntaxNode node)
            => node.IsReturnableConstruct();

        protected override IEnumerable<TextSpan> GetHighlightsForNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();
            HighlightRelatedKeywords(node, spans);
            return spans;
        }

        private static void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
        {
            // Highlight async keyword
            switch (node)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    {
                        var asyncModifier = methodDeclaration.Modifiers.FirstOrDefault(m => m.Kind() == SyntaxKind.AsyncKeyword);
                        if (asyncModifier.Kind() != SyntaxKind.None)
                        {
                            spans.Add(asyncModifier.Span);
                        }
                        break;
                    }
                case LocalFunctionStatementSyntax localFunction:
                    {
                        var asyncModifier = localFunction.Modifiers.FirstOrDefault(m => m.Kind() == SyntaxKind.AsyncKeyword);
                        if (asyncModifier.Kind() != SyntaxKind.None)
                        {
                            spans.Add(asyncModifier.Span);
                        }
                        break;
                    }
                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    if (anonymousFunction.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        spans.Add(anonymousFunction.AsyncKeyword.Span);
                    }
                    break;

                case AwaitExpressionSyntax awaitExpression:
                    // Note if there is already a highlight for the previous token, merge it
                    // with this span. That way, we highlight nested awaits with a single span.
                    var handled = false;
                    var awaitToken = awaitExpression.AwaitKeyword;
                    var previousToken = awaitToken.GetPreviousToken();
                    if (!previousToken.Span.IsEmpty)
                    {
                        var index = spans.FindIndex(s => s.Contains(previousToken.Span));
                        if (index >= 0)
                        {
                            var span = spans[index];
                            spans[index] = TextSpan.FromBounds(span.Start, awaitToken.Span.End);
                            handled = true;
                        }
                    }

                    if (!handled)
                    {
                        spans.Add(awaitToken.Span);
                    }
                    break;

                case UsingStatementSyntax usingStatement:
                    if (usingStatement.AwaitKeyword.Kind() == SyntaxKind.AwaitKeyword)
                    {
                        spans.Add(usingStatement.AwaitKeyword.Span);
                    }
                    break;

                case LocalDeclarationStatementSyntax localDeclaration:
                    if (localDeclaration.AwaitKeyword.Kind() == SyntaxKind.AwaitKeyword && localDeclaration.UsingKeyword.Kind() == SyntaxKind.UsingKeyword)
                    {
                        spans.Add(localDeclaration.AwaitKeyword.Span);
                    }
                    break;

                case CommonForEachStatementSyntax forEachStatement:
                    if (forEachStatement.AwaitKeyword.Kind() == SyntaxKind.AwaitKeyword)
                    {
                        spans.Add(forEachStatement.AwaitKeyword.Span);
                    }
                    break;
            }

            foreach (var child in node.ChildNodes())
            {
                // Only recurse if we have anything to do
                if (!child.IsReturnableConstruct())
                {
                    HighlightRelatedKeywords(child, spans);
                }
            }
        }
    }
}
