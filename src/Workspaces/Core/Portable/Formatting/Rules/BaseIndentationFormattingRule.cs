// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal class BaseIndentationFormattingRule : AbstractFormattingRule
    {
        private readonly AbstractFormattingRule _vbHelperFormattingRule;
        private readonly int _baseIndentation;
        private readonly SyntaxToken _token1;
        private readonly SyntaxToken _token2;
        private readonly SyntaxNode _commonNode;
        private readonly TextSpan _span;

        public BaseIndentationFormattingRule(SyntaxNode root, TextSpan span, int baseIndentation, AbstractFormattingRule vbHelperFormattingRule = null)
        {
            _span = span;
            SetInnermostNodeForSpan(root, ref _span, out _token1, out _token2, out _commonNode);

            _baseIndentation = baseIndentation;
            _vbHelperFormattingRule = vbHelperFormattingRule;
        }

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
        {
            // for the common node itself, return absolute indentation
            if (_commonNode == node)
            {
                // TODO: If the first line of the span includes a node, we want to align with the position of that node 
                // in the primary buffer.  That's what Dev12 does for C#, but it doesn't match Roslyn's current model
                // of each statement being formatted independently with respect to it's parent.
                list.Add(new IndentBlockOperation(_token1, _token2, _span, _baseIndentation, IndentBlockOption.AbsolutePosition));
            }
            else if (node.Span.Contains(_span))
            {
                // any node bigger than our span is ignored.
                return;
            }

            // Add everything to the list.
            AddNextIndentBlockOperations(list, node, optionSet, in nextOperation);

            // Filter out everything that encompasses our span.
            AdjustIndentBlockOperation(list);
        }

        private void AddNextIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
        {
            if (_vbHelperFormattingRule == null)
            {
                base.AddIndentBlockOperations(list, node, optionSet, in nextOperation);
                return;
            }

            _vbHelperFormattingRule.AddIndentBlockOperations(list, node, optionSet, in nextOperation);
        }

        private void AdjustIndentBlockOperation(List<IndentBlockOperation> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var operation = list[i];

                // already filtered out operation
                if (operation == null)
                {
                    continue;
                }

                // if span is same as us, make sure we only include ourselves.
                if (_span == operation.TextSpan && !Myself(operation))
                {
                    list[i] = null;
                    continue;
                }

                // inside of us, skip it.
                if (_span.Contains(operation.TextSpan))
                {
                    continue;
                }

                // throw away operation that encloses ourselves
                if (operation.TextSpan.Contains(_span))
                {
                    list[i] = null;
                    continue;
                }

                // now we have an interesting case where indentation block intersects with us.
                // this can happen if code is split in two different script blocks or nuggets.
                // here, we will re-adjust block to be contained within our span.
                if (operation.TextSpan.IntersectsWith(_span))
                {
                    list[i] = CloneAndAdjustFormattingOperation(operation);
                }
            }
        }

        private bool Myself(IndentBlockOperation operation)
        {
            return operation.TextSpan == _span &&
                   operation.StartToken == _token1 &&
                   operation.EndToken == _token2 &&
                   operation.IndentationDeltaOrPosition == _baseIndentation &&
                   operation.Option == IndentBlockOption.AbsolutePosition;
        }

        private IndentBlockOperation CloneAndAdjustFormattingOperation(IndentBlockOperation operation)
        {
            switch (operation.Option)
            {
                case IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine:
                    return FormattingOperations.CreateRelativeIndentBlockOperation(operation.BaseToken, operation.StartToken, operation.EndToken, AdjustTextSpan(operation.TextSpan), operation.IndentationDeltaOrPosition, operation.Option);
                case IndentBlockOption.RelativePosition:
                case IndentBlockOption.AbsolutePosition:
                    return FormattingOperations.CreateIndentBlockOperation(operation.StartToken, operation.EndToken, AdjustTextSpan(operation.TextSpan), operation.IndentationDeltaOrPosition, operation.Option);
                default:
                    throw ExceptionUtilities.UnexpectedValue(operation.Option);
            }
        }

        private TextSpan AdjustTextSpan(TextSpan textSpan)
        {
            return TextSpan.FromBounds(Math.Max(_span.Start, textSpan.Start), Math.Min(_span.End, textSpan.End));
        }

        private void SetInnermostNodeForSpan(SyntaxNode root, ref TextSpan span, out SyntaxToken token1, out SyntaxToken token2, out SyntaxNode commonNode)
        {
            commonNode = default;

            GetTokens(root, span, out token1, out token2);

            span = GetSpanFromTokens(span, token1, token2);

            if (token1.RawKind == 0 || token2.RawKind == 0)
            {
                return;
            }

            commonNode = token1.GetCommonRoot(token2);
        }

        private static void GetTokens(SyntaxNode root, TextSpan span, out SyntaxToken token1, out SyntaxToken token2)
        {
            // get tokens within given span
            token1 = root.FindToken(span.Start);
            token2 = root.FindTokenFromEnd(span.End);

            // It is possible the given span doesn't have any tokens in them. In that case, 
            // make tokens to be the adjacent ones to the given span.
            if (span.End < token1.Span.Start)
            {
                token1 = token1.GetPreviousToken();
            }

            if (token2.Span.End < span.Start)
            {
                token2 = token2.GetNextToken();
            }
        }

        private static TextSpan GetSpanFromTokens(TextSpan span, SyntaxToken token1, SyntaxToken token2)
        {
            var tree = token1.SyntaxTree;

            // adjust span to include all whitespace before and after the given span.
            var start = token1.Span.End;

            // current token is inside of the given span, get previous token's end position
            if (span.Start <= token1.Span.Start)
            {
                token1 = token1.GetPreviousToken();
                start = token1.Span.End;

                // If token1, that was passed, is the first visible token of the tree then we want to
                // the beginning of the span to start from the beginning of the tree
                if (token1.RawKind == 0)
                {
                    start = 0;
                }
            }

            var end = token2.Span.Start;

            // current token is inside of the given span, get next token's start position.
            if (token2.Span.End <= span.End)
            {
                token2 = token2.GetNextToken();
                end = token2.Span.Start;

                // If token2, that was passed, was the last visible token of the tree then we want the
                // span to expand till the end of the tree
                if (token2.RawKind == 0)
                {
                    end = tree.Length;
                }
            }

            if (token1.Equals(token2) && end < start)
            {
                // This can happen if `token1.Span` is larger than `span` on each end (due to trivia) and occurs when
                // only a single token is projected into a buffer and the projection is sandwiched between two other
                // projections into the same backing buffer.  An example of this is during broken code scenarios when
                // typing certain Razor `@` directives.
                var temp = end;
                end = start;
                start = temp;
            }

            return TextSpan.FromBounds(start, end);
        }
    }
}
