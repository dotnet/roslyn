// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormattingContext
{
    private class InitialContextFinder
    {
        private readonly TokenStream _tokenStream;
        private readonly ChainedFormattingRules _formattingRules;
        private readonly SyntaxNode _rootNode;

        public InitialContextFinder(
            TokenStream tokenStream,
            ChainedFormattingRules formattingRules,
            SyntaxNode rootNode)
        {
            Contract.ThrowIfNull(tokenStream);
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(rootNode);

            _tokenStream = tokenStream;
            _formattingRules = formattingRules;
            _rootNode = rootNode;
        }

        public (List<IndentBlockOperation> indentOperations, ImmutableArray<SuppressOperation> suppressOperations) Do(SyntaxToken startToken, SyntaxToken endToken)
        {
            // we are formatting part of document, try to find initial context that formatting will be based on such as
            // initial indentation and etc.
            using (Logger.LogBlock(FunctionId.Formatting_ContextInitialization, CancellationToken.None))
            {
                // first try to set initial indentation information
                var initialIndentationOperations = this.GetInitialIndentBlockOperations(startToken, endToken);

                // second try to set suppress wrapping regions
                var initialSuppressOperations = GetInitialSuppressOperations(startToken, endToken);
                if (initialSuppressOperations != null)
                {
                    Debug.Assert(
                        initialSuppressOperations.All(
                            o => o.TextSpan.Contains(startToken.SpanStart) ||
                                 o.TextSpan.Contains(endToken.SpanStart)));
                }

                return (initialIndentationOperations, initialSuppressOperations);
            }
        }

        private List<IndentBlockOperation> GetInitialIndentBlockOperations(SyntaxToken startToken, SyntaxToken endToken)
        {
            var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            var node = startToken.GetCommonRoot(endToken)!.GetParentWithBiggerSpan();
            var previous = (SyntaxNode?)null;

            // starting from the common node, move up to the parent
            var operations = new List<IndentBlockOperation>();
            var list = new List<IndentBlockOperation>();
            while (node != null)
            {
                // get all operations for the nodes that contains the formatting span, but not ones contained by the span
                node.DescendantNodesAndSelf(n => n != previous && n.Span.IntersectsWith(span) && !span.Contains(n.Span))
                    .Do(n =>
                        {
                            _formattingRules.AddIndentBlockOperations(list, n);
                            foreach (var element in list)
                            {
                                if (element != null)
                                {
                                    operations.Add(element);
                                }
                            }

                            list.Clear();
                        });

                // found some. use these as initial indentation
                if (operations.Any(o => o.TextSpan.Contains(span)))
                {
                    break;
                }

                previous = node;
                node = node.Parent;
            }

            // make sure operations we have has effects over the formatting span
            operations.RemoveAll(o => o == null || !o.TextSpan.IntersectsWith(span));

            // we couldn't find anything
            // return initial location so that we can get base indentation correctly
            if (operations.Count == 0)
            {
                operations.Add(new IndentBlockOperation(
                    startToken: _rootNode.GetFirstToken(includeZeroWidth: true),
                    endToken: _rootNode.GetLastToken(includeZeroWidth: true),
                    textSpan: _rootNode.FullSpan,
                    indentationDelta: 0,
                    option: IndentBlockOption.AbsolutePosition));

                return operations;
            }

            operations.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);
            return operations;
        }

        private ImmutableArray<SuppressOperation> GetInitialSuppressOperations(SyntaxToken startToken, SyntaxToken endToken)
        {
            using var _ = ArrayBuilder<SuppressOperation>.GetInstance(out var result);

            this.AddInitialSuppressOperations(startToken, endToken, SuppressOption.NoWrapping, result);
            this.AddInitialSuppressOperations(startToken, endToken, SuppressOption.NoSpacing, result);

            result.Sort(CommonFormattingHelpers.SuppressOperationComparer);
            return result.ToImmutable();
        }

        private void AddInitialSuppressOperations(
            SyntaxToken startToken, SyntaxToken endToken, SuppressOption mask, ArrayBuilder<SuppressOperation> result)
        {
            this.AddInitialSuppressOperations(startToken, mask, result);
            this.AddInitialSuppressOperations(endToken, mask, result);
        }

        private void AddInitialSuppressOperations(SyntaxToken token, SuppressOption mask, ArrayBuilder<SuppressOperation> result)
        {
            var startNode = token.Parent;
            var startPosition = token.SpanStart;

            // starting from given token, move up to root until the first meaningful
            // operation has found
            using var _ = ArrayBuilder<SuppressOperation>.GetInstance(out var buffer);

            var currentIndentationNode = startNode;
            while (currentIndentationNode != null)
            {
                _formattingRules.AddSuppressOperations(buffer, currentIndentationNode);

                buffer.RemoveAll(match: Predicate, arg: (startPosition, _tokenStream, mask));
                if (buffer.Count > 0)
                {
                    result.AddRange(buffer);
                    return;
                }

                currentIndentationNode = currentIndentationNode.Parent;
            }

            return;

            static bool Predicate(SuppressOperation operation, (int startPosition, TokenStream tokenStream, SuppressOption mask) tuple)
            {
                if (!operation.TextSpan.Contains(tuple.startPosition))
                    return true;

                if (operation.ContainsElasticTrivia(tuple.tokenStream) && !operation.Option.IsOn(SuppressOption.IgnoreElasticWrapping))
                    return true;

                if (!operation.Option.IsMaskOn(tuple.mask))
                    return true;

                return false;
            }
        }
    }
}
