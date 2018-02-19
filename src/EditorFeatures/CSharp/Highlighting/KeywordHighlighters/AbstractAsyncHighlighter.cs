﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    internal abstract class AbstractAsyncHighlighter<TNode> : AbstractKeywordHighlighter<TNode> where TNode : SyntaxNode
    {
        protected void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
        {
            // Highlight async keyword
            switch (node)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    var asyncModifier = methodDeclaration.Modifiers.FirstOrDefault(m => m.Kind() == SyntaxKind.AsyncKeyword);
                    if (asyncModifier.Kind() != SyntaxKind.None)
                    {
                        spans.Add(asyncModifier.Span);
                    }
                    break;

                case SimpleLambdaExpressionSyntax simpleLambda:
                    if (simpleLambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        spans.Add(simpleLambda.AsyncKeyword.Span);
                    }
                    break;

                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    if (parenthesizedLambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        spans.Add(parenthesizedLambda.AsyncKeyword.Span);
                    }
                    break;

                case AnonymousMethodExpressionSyntax anonymousMethod:
                    if (anonymousMethod.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        spans.Add(anonymousMethod.AsyncKeyword.Span);
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
