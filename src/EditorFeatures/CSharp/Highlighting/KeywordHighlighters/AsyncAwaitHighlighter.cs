// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class AsyncAwaitHighlighter : AbstractKeywordHighlighter
    {
        private static readonly ObjectPool<Stack<SyntaxNode>> s_stackPool
            = new ObjectPool<Stack<SyntaxNode>>(() => new Stack<SyntaxNode>());

        [ImportingConstructor]
        public AsyncAwaitHighlighter()
        {
        }

        protected override bool IsHighlightableNode(SyntaxNode node)
            => node.IsReturnableConstruct();

        protected override IEnumerable<TextSpan> GetHighlightsForNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();

            foreach (var current in WalkChildren(node))
            {
                cancellationToken.ThrowIfCancellationRequested();
                HighlightRelatedKeywords(current, spans);
            }

            return spans;
        }

        private IEnumerable<SyntaxNode> WalkChildren(SyntaxNode node)
        {
            using (var pooledObject = s_stackPool.GetPooledObject())
            {
                var stack = pooledObject.Object;
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    yield return current;

                    foreach (var child in current.ChildNodes())
                    {
                        // Only recurse if we have anything to do
                        if (!child.IsReturnableConstruct())
                        {
                            stack.Push(child);
                        }
                    }
                }
            }
        }

        private static void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
        {
            // Highlight async keyword
            switch (node)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    TryAddAsyncModifier(methodDeclaration.Modifiers, spans);
                    return;
                case LocalFunctionStatementSyntax localFunction:
                    TryAddAsyncModifier(localFunction.Modifiers, spans);
                    return;
                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    TryAddAsyncOrAwaitKeyword(anonymousFunction.AsyncKeyword, spans);
                    return;
                case UsingStatementSyntax usingStatement:
                    TryAddAsyncOrAwaitKeyword(usingStatement.AwaitKeyword, spans);
                    return;
                case LocalDeclarationStatementSyntax localDeclaration:
                    if (localDeclaration.UsingKeyword.Kind() == SyntaxKind.UsingKeyword)
                    {
                        TryAddAsyncOrAwaitKeyword(localDeclaration.AwaitKeyword, spans);
                    }
                    return;
                case CommonForEachStatementSyntax forEachStatement:
                    TryAddAsyncOrAwaitKeyword(forEachStatement.AwaitKeyword, spans);
                    return;
                case AwaitExpressionSyntax awaitExpression:
                    TryAddAsyncOrAwaitKeyword(awaitExpression.AwaitKeyword, spans);
                    return;
            }
        }

        private static void TryAddAsyncModifier(SyntaxTokenList modifiers, List<TextSpan> spans)
        {
            foreach (var mod in modifiers)
            {
                if (TryAddAsyncOrAwaitKeyword(mod, spans))
                {
                    return;
                }
            }
        }

        private static bool TryAddAsyncOrAwaitKeyword(SyntaxToken mod, List<TextSpan> spans)
        {
            if (mod.IsKind(SyntaxKind.AsyncKeyword, SyntaxKind.AwaitKeyword))
            {
                // Note if there is already a highlight for the previous token, merge it with this
                // span. That way, we highlight nested awaits with a single span.

                if (spans.Count > 0)
                {
                    var previousToken = mod.GetPreviousToken();
                    var lastSpan = spans[spans.Count - 1];
                    if (lastSpan == previousToken.Span)
                    {
                        spans[spans.Count - 1] = TextSpan.FromBounds(lastSpan.Start, mod.Span.End);
                        return true;
                    }
                }

                spans.Add(mod.Span);
                return true;
            }

            return false;
        }
    }
}
