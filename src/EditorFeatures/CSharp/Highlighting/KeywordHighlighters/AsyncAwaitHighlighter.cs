﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class AsyncAwaitHighlighter : AbstractKeywordHighlighter
    {
        private static readonly ObjectPool<Stack<SyntaxNode>> s_stackPool
            = SharedPools.Default<Stack<SyntaxNode>>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AsyncAwaitHighlighter()
        {
        }

        protected override bool IsHighlightableNode(SyntaxNode node)
            => node.IsReturnableConstruct();

        protected override void AddHighlightsForNode(SyntaxNode node, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            foreach (var current in WalkChildren(node))
            {
                cancellationToken.ThrowIfCancellationRequested();
                HighlightRelatedKeywords(current, highlights);
            }
        }

        private static IEnumerable<SyntaxNode> WalkChildren(SyntaxNode node)
        {
            using var pooledObject = s_stackPool.GetPooledObject();

            var stack = pooledObject.Object;
            stack.Push(node);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                // 'Reverse' isn't really necessary, but it means we walk the nodes in document
                // order, which is nicer when debugging and understanding the results produced.
                foreach (var child in current.ChildNodesAndTokens().Reverse())
                {
                    if (child.IsNode)
                    {
                        var childNode = child.AsNode();

                        // Only process children if they're not the start of another construct
                        // that async/await would be related to.
                        if (!childNode.IsReturnableConstruct())
                        {
                            stack.Push(childNode);
                        }
                    }
                }
            }
        }

        private static bool HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
            => node switch
            {
                MethodDeclarationSyntax methodDeclaration => TryAddAsyncModifier(methodDeclaration.Modifiers, spans),
                LocalFunctionStatementSyntax localFunction => TryAddAsyncModifier(localFunction.Modifiers, spans),
                AnonymousFunctionExpressionSyntax anonymousFunction => TryAddAsyncOrAwaitKeyword(anonymousFunction.AsyncKeyword, spans),
                UsingStatementSyntax usingStatement => TryAddAsyncOrAwaitKeyword(usingStatement.AwaitKeyword, spans),
                LocalDeclarationStatementSyntax localDeclaration =>
                    localDeclaration.UsingKeyword.Kind() == SyntaxKind.UsingKeyword && TryAddAsyncOrAwaitKeyword(localDeclaration.AwaitKeyword, spans),
                CommonForEachStatementSyntax forEachStatement => TryAddAsyncOrAwaitKeyword(forEachStatement.AwaitKeyword, spans),
                AwaitExpressionSyntax awaitExpression => TryAddAsyncOrAwaitKeyword(awaitExpression.AwaitKeyword, spans),
                _ => false,
            };

        private static bool TryAddAsyncModifier(SyntaxTokenList modifiers, List<TextSpan> spans)
        {
            foreach (var mod in modifiers)
            {
                if (TryAddAsyncOrAwaitKeyword(mod, spans))
                {
                    return true;
                }
            }

            return false;
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
                    var lastSpan = spans[^1];
                    if (lastSpan == previousToken.Span)
                    {
                        spans[^1] = TextSpan.FromBounds(lastSpan.Start, mod.Span.End);
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
