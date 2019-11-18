// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class BottomUpBaseIndentationFinder
    {
        private readonly TokenStream _tokenStream;
        private readonly ChainedFormattingRules _formattingRules;
        private readonly int _tabSize;
        private readonly int _indentationSize;

        public BottomUpBaseIndentationFinder(
            ChainedFormattingRules formattingRules,
            int tabSize,
            int indentationSize,
            TokenStream tokenStream)
        {
            Contract.ThrowIfNull(formattingRules);

            _formattingRules = formattingRules;
            _tabSize = tabSize;
            _indentationSize = indentationSize;
            _tokenStream = tokenStream;
        }

        public int? FromIndentBlockOperations(
            SyntaxTree tree, SyntaxToken token, int position, CancellationToken cancellationToken)
        {
            // we use operation service to see whether it is a starting point of new indentation.
            // ex)
            //  if (true)
            //  {
            //     | <= this is new starting point of new indentation
            var operation = GetIndentationDataFor(tree.GetRoot(cancellationToken), token, position);

            // try find indentation based on indentation operation
            if (operation != null)
            {
                // make sure we found new starting point of new indentation.
                // such operation should start span after the token (a token that is right before the new indentation),
                // contains current position, and position should be before the existing next token
                if (token.Span.End <= operation.TextSpan.Start &&
                    operation.TextSpan.IntersectsWith(position) &&
                    position <= token.GetNextToken(includeZeroWidth: true).SpanStart)
                {
                    return GetIndentationOfCurrentPosition(tree, token, position, cancellationToken);
                }
            }

            return null;
        }

        public int? FromAlignTokensOperations(SyntaxTree tree, SyntaxToken token)
        {
            // let's check whether there is any missing token under us and whether
            // there is an align token operation for that missing token.
            var nextToken = token.GetNextToken(includeZeroWidth: true);
            if (nextToken.RawKind != 0 &&
                nextToken.Width() <= 0)
            {
                // looks like we have one. find whether there is a align token operation for this token
                var alignmentBaseToken = GetAlignmentBaseTokenFor(nextToken);
                if (alignmentBaseToken.RawKind != 0)
                {
                    return tree.GetTokenColumn(alignmentBaseToken, _tabSize);
                }
            }

            return null;
        }

        public int GetIndentationOfCurrentPosition(
            SyntaxTree tree, SyntaxToken token, int position, CancellationToken cancellationToken)
        {
            return GetIndentationOfCurrentPosition(tree, token, position, extraSpaces: 0, cancellationToken: cancellationToken);
        }

        public int GetIndentationOfCurrentPosition(
            SyntaxTree tree, SyntaxToken token, int position, int extraSpaces, CancellationToken cancellationToken)
        {
            // gather all indent operations 
            var list = GetParentIndentBlockOperations(token);

            return GetIndentationOfCurrentPosition(
                tree.GetRoot(cancellationToken),
                token, list, position, extraSpaces,
                t => tree.GetTokenColumn(t, _tabSize),
                cancellationToken);
        }

        public int GetIndentationOfCurrentPosition(
            SyntaxNode root,
            IndentBlockOperation startingOperation,
            Func<SyntaxToken, int> tokenColumnGetter,
            CancellationToken cancellationToken)
        {
            var token = startingOperation.StartToken;

            // gather all indent operations 
            var list = GetParentIndentBlockOperations(token);

            // remove one that is smaller than current one
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (CommonFormattingHelpers.IndentBlockOperationComparer(startingOperation, list[i]) < 0)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }

            return GetIndentationOfCurrentPosition(root, token, list, token.SpanStart, /* extraSpaces */ 0, tokenColumnGetter, cancellationToken);
        }

        private int GetIndentationOfCurrentPosition(
            SyntaxNode root,
            SyntaxToken token,
            List<IndentBlockOperation> list,
            int position,
            int extraSpaces,
            Func<SyntaxToken, int> tokenColumnGetter,
            CancellationToken cancellationToken)
        {
            var tuple = GetIndentationRuleOfCurrentPosition(root, token, list, position);
            var indentationLevel = tuple.indentation;
            var operation = tuple.operation;

            if (operation == null)
            {
                return indentationLevel * _indentationSize + extraSpaces;
            }

            if (operation.IsRelativeIndentation)
            {
                var baseToken = operation.BaseToken;

                // If the SmartIndenter created this IndentationFinder then tokenStream will be a null hence we should do a null check on the tokenStream
                if (operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
                {
                    if (_tokenStream != null)
                    {
                        baseToken = _tokenStream.FirstTokenOfBaseTokenLine(baseToken);
                    }
                    else
                    {
                        var textLine = baseToken.SyntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(baseToken.SpanStart);
                        baseToken = baseToken.SyntaxTree.GetRoot(cancellationToken).FindToken(textLine.Start);
                    }
                }

                var baseIndentation = tokenColumnGetter(baseToken);
                return Math.Max(0, baseIndentation + (indentationLevel + operation.IndentationDeltaOrPosition) * _indentationSize);
            }

            if (operation.Option.IsOn(IndentBlockOption.AbsolutePosition))
            {
                return Math.Max(0, indentationLevel + extraSpaces);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private (int indentation, IndentBlockOperation operation) GetIndentationRuleOfCurrentPosition(
            SyntaxNode root, SyntaxToken token, List<IndentBlockOperation> list, int position)
        {
            var indentationLevel = 0;
            var operations = GetIndentBlockOperationsFromSmallestSpan(root, list, position);
            foreach (var operation in operations)
            {
                if (operation.Option.IsOn(IndentBlockOption.AbsolutePosition))
                {
                    return (operation.IndentationDeltaOrPosition + _indentationSize * indentationLevel, operation);
                }

                if (operation.Option == IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine)
                {
                    return (indentationLevel, operation);
                }

                if (operation.IsRelativeIndentation)
                {
                    return (indentationLevel, operation);
                }

                // move up to its containing operation
                indentationLevel += operation.IndentationDeltaOrPosition;
            }

            return (indentationLevel, null);
        }

        private List<IndentBlockOperation> GetParentIndentBlockOperations(SyntaxToken token)
        {
            var allNodes = GetParentNodes(token);

            // gather all indent operations 
            var list = new List<IndentBlockOperation>();
            allNodes.Do(n => _formattingRules.AddIndentBlockOperations(list, n));

            // sort them in right order
            list.RemoveAll(CommonFormattingHelpers.IsNull);
            list.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);

            return list;
        }

        // Get parent nodes, including walking out of structured trivia.
        private IEnumerable<SyntaxNode> GetParentNodes(SyntaxToken token)
        {
            var current = token.Parent;

            while (current != null)
            {
                yield return current;
                if (current.IsStructuredTrivia)
                {
                    current = ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent;
                }
                else
                {
                    current = current.Parent;
                }
            }
        }

        private SyntaxToken GetAlignmentBaseTokenFor(SyntaxToken token)
        {
            var startNode = token.Parent;

            var list = new List<AlignTokensOperation>();
            var currentNode = startNode;

            while (currentNode != null)
            {
                list.Clear();
                _formattingRules.AddAlignTokensOperations(list, currentNode);

                if (list.Count == 0)
                {
                    currentNode = currentNode.Parent;
                    continue;
                }

                // make sure we have the given token as one of tokens to be aligned to the base token
                var match = list.FirstOrDefault(o => o != null && o.Tokens.Contains(token));
                if (match != null)
                {
                    return match.BaseToken;
                }

                currentNode = currentNode.Parent;
            }

            return default;
        }

        private IndentBlockOperation GetIndentationDataFor(SyntaxNode root, SyntaxToken token, int position)
        {
            var startNode = token.Parent;

            // starting from given token, move up to the root until it finds the first set of appropriate operations
            var list = new List<IndentBlockOperation>();

            var currentNode = startNode;
            while (currentNode != null)
            {
                _formattingRules.AddIndentBlockOperations(list, currentNode);

                if (list.Any(o => o != null && o.TextSpan.Contains(position)))
                {
                    break;
                }

                currentNode = currentNode.Parent;
            }

            // well, found no appropriate one
            list.RemoveAll(CommonFormattingHelpers.IsNull);
            if (list.Count == 0)
            {
                return null;
            }

            // now sort the found ones in right order
            list.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);

            return GetIndentBlockOperationsFromSmallestSpan(root, list, position).FirstOrDefault();
        }

        private static IEnumerable<IndentBlockOperation> GetIndentBlockOperationsFromSmallestSpan(SyntaxNode root, List<IndentBlockOperation> list, int position)
        {
            var lastVisibleToken = default(SyntaxToken);
            var map = new HashSet<TextSpan>();

            // iterate backward
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var operation = list[i];
                if (map.Contains(operation.TextSpan))
                {
                    // no duplicated one
                    continue;
                }

                map.Add(operation.TextSpan);

                // normal case. the operation contains the position
                if (operation.TextSpan.Contains(position))
                {
                    yield return operation;
                    continue;
                }

                // special case for empty span. in case of empty span, consider it
                // contains the position if start == position
                if (operation.TextSpan.IsEmpty && operation.TextSpan.Start == position)
                {
                    yield return operation;
                    continue;
                }

                var nextToken = operation.EndToken.GetNextToken(includeZeroWidth: true);

                // special case where position is same as end position of an operation and
                // its next token is missing token. in this case, we will consider current position 
                // to belong to current operation.
                // this can happen in malformed code where end of indentation is missing
                if (operation.TextSpan.End == position && nextToken.IsMissing)
                {
                    yield return operation;
                    continue;
                }

                // special case where position is same as end position of the operation and
                // its next token is right at the position
                if (operation.TextSpan.End == position && position == nextToken.SpanStart)
                {
                    yield return operation;
                    continue;
                }

                // special case for the end of the span == position
                // if position is at the end of the last token of the tree. consider the position
                // belongs to the operation
                if (root.FullSpan.End == position && operation.TextSpan.End == position)
                {
                    yield return operation;
                    continue;
                }

                // more expensive check
                lastVisibleToken = (lastVisibleToken.RawKind == 0) ? root.GetLastToken() : lastVisibleToken;
                if (lastVisibleToken.Span.End <= position && operation.TextSpan.End == position)
                {
                    yield return operation;
                    continue;
                }
            }
        }
    }
}
