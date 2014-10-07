using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal class CommonOperationBasedSmartIndenter
    {
        private readonly IEnumerable<IFormattingRule> formattingRules;
        private readonly int tabSize;
        private readonly int indentationSize;

        public CommonOperationBasedSmartIndenter(
            IEnumerable<IFormattingRule> formattingRules,
            int tabSize,
            int indentationSize)
        {
            Contract.ThrowIfNull(formattingRules);

            this.formattingRules = formattingRules;
            this.tabSize = tabSize;
            this.indentationSize = indentationSize;
        }

        public int? GetIndentationForBeginningOfNewIndentation(CommonSyntaxTree syntaxTree, CommonSyntaxToken token, int position)
        {
            // we use operation service to see whether it is a starting point of new indentation.
            // ex)
            //  if (true)
            //  {
            //     | <= this is new starting point of new indentation
            var operation = GetIndentationDataFor(syntaxTree, token, position);

            // try find indentation based on indentation operation
            if (operation != null)
            {
                // make sure we found new starting point of new indentation.
                // such operation should start span after the token (a token that is right before the new indentation),
                // contains current position, and position should be before the existing next token
                if (token.Span.End <= operation.Span.Start && 
                    operation.Span.IntersectsWith(position) &&
                    position <= token.GetNextToken(CommonSyntaxHelper.Any).Span.Start)
                {
                    return GetIndentationOfCurrentPosition(syntaxTree, token, position);
                }
            }

            return null;
        }

        public int? GetIndentationForNextAlignmentToken(CommonSyntaxTree syntaxTree, CommonSyntaxToken token)
        {
            // let's check whether there is any missing token under us and whether
            // there is an align token operation for that missing token.
            var nextToken = token.GetNextToken(CommonSyntaxHelper.Any);
            if (nextToken.Kind != 0 &&
                nextToken.Width() <= 0)
            {
                // looks like we have one. find whether there is a align token operation for this token
                var alignmentBaseToken = GetAlignmentBaseTokenFor(nextToken);
                if (alignmentBaseToken.Kind != 0)
                {
                    return CommonFormattingHelpers.GetTokenColumn(syntaxTree, alignmentBaseToken, this.tabSize);
                }
            }

            return null;
        }

        public int GetIndentationOfCurrentPosition(CommonSyntaxTree tree, CommonSyntaxToken token, int position)
        {
            return GetIndentationOfCurrentPosition(
                tree.Root,
                token,
                position,
                t => CommonFormattingHelpers.IsFirstTokenOnLine(tree, t),
                t => CommonFormattingHelpers.GetTokenColumn(tree, t, this.tabSize));
        }

        public int GetIndentationOfCurrentPosition(
            CommonSyntaxNode root,
            CommonSyntaxToken token,
            int position,
            Func<CommonSyntaxToken, bool> checkFirstTokenOnLine,
            Func<CommonSyntaxToken, int> tokenColumnGetter)
        {
            var tuple = GetIndentationRuleOfCurrentPosition(root, token, position, checkFirstTokenOnLine);
            var indentationLevel = tuple.Item1;
            var operation = tuple.Item2;

            if (operation == null)
            {
                return indentationLevel * this.indentationSize;
            }

            if (operation.IsRelativeIndentation)
            {
                var baseIndentation = tokenColumnGetter(operation.BaseToken);
                return Math.Max(0, baseIndentation + (indentationLevel + operation.IndentationDeltaOrPosition) * this.indentationSize);
            }

            if (operation.Option.IsFlagOn(IndentBlockOption.AbsolutePosition))
            {
                return Math.Max(0, operation.IndentationDeltaOrPosition);
            }

            // indenation for normal case
            {
                var baseIndentation = tokenColumnGetter(operation.StartToken);
                return Math.Max(0, baseIndentation + indentationLevel * this.indentationSize);
            }
        }

        private ValueTuple<int, IndentBlockOperation> GetIndentationRuleOfCurrentPosition(
            CommonSyntaxNode root,
            CommonSyntaxToken token,
            int position,
            Func<CommonSyntaxToken, bool> checkFirstTokenOnLine)
        {
            var allNodes = GetAllParentNodes(token);

            // gather all indent operations 
            var list = new List<IndentBlockOperation>();
            allNodes.Do(n => list.AddRange(Formatter.GetIndentBlockOperations(this.formattingRules, n)));

            // sort them in right order
            list.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);

            var indentationLevel = 0;
            var operations = GetIndentOperations(root, list, position);
            foreach (var operation in operations)
            {
                if (operation.IsRelativeIndentation ||
                    operation.Option.IsFlagOn(IndentBlockOption.AbsolutePosition))
                {
                    return ValueTuple.Create(indentationLevel, operation);
                }

                // found base indenation for normal case
                // IndentOperation has a range (Start and End Token) that the indentaiton will apply to.
                // this will try to find an operation that contains given position and its start token being used
                // as indentation (first token on line)
                // we check ordering of tokens since operations can have reversed start/end token if the range was empty
                if (operation.StartToken != token &&
                    operation.StartToken.IsMissing == false &&
                    operation.StartToken.Span.End <= token.Span.Start &&
                    operation.StartToken.Span.End <= operation.EndToken.Span.Start &&
                    checkFirstTokenOnLine(operation.StartToken))
                {
                    return ValueTuple.Create(indentationLevel, operation);
                }

                // move up to its contains operation
                indentationLevel += operation.IndentationDeltaOrPosition;
            }

            return new ValueTuple<int, IndentBlockOperation>(indentationLevel, null);
        }

        private IEnumerable<IndentBlockOperation> GetIndentOperations(CommonSyntaxNode root, List<IndentBlockOperation> list, int position)
        {
            var map = new HashSet<TextSpan>();

            // iterate backward
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var operation = list[i];
                if (map.Contains(operation.Span))
                {
                    // no duplicated one
                    continue;
                }

                map.Add(operation.Span);

                // normal case. the operation contains the position
                if (operation.Span.Contains(position))
                {
                    yield return operation;
                    continue;
                }

                // special case for empty span. in case of empty span, consider it
                // contains the position if start == position
                if (operation.Span.IsEmpty && operation.Span.Start == position)
                {
                    yield return operation;
                    continue;
                }

                // special case for the end of the span == position
                // if position is at the end of the last token of the tree. consider the position
                // belongs to the operation
                if (root.FullSpan.End == position && operation.Span.End == position)
                {
                    yield return operation;
                    continue;
                }

                // more expensive check
                var lastVisibleToken = root.GetLastToken();
                if (lastVisibleToken.Span.End <= position && operation.Span.End == position)
                {
                    yield return operation;
                    continue;
                }
            }
        }

        // Get parent nodes, including walking out of structured trivia.
        private IEnumerable<CommonSyntaxNode> GetAllParentNodes(CommonSyntaxToken token)
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

        private CommonSyntaxToken GetAlignmentBaseTokenFor(CommonSyntaxToken token)
        {
            var startNode = token.Parent;

            var currentNode = startNode;
            while (currentNode != null)
            {
                var operations = Formatter.GetAlignTokensOperations(this.formattingRules, currentNode);
                if (operations.Count() == 0)
                {
                    currentNode = currentNode.Parent;
                    continue;
                }

                // make sure we have the given token as one of tokens to be aligned to the base token
                var match = operations.FirstOrDefault(o => o.Option != AlignTokensOption.AlignPositionOfTokensToIndentation && o.Tokens.Contains(token));
                if (match != null)
                {
                    return match.BaseToken;
                }

                currentNode = currentNode.Parent;
            }

            return default(CommonSyntaxToken);
        }

        private IndentBlockOperation GetIndentationDataFor(CommonSyntaxTree syntaxTree, CommonSyntaxToken token, int position)
        {
            var startNode = token.Parent;

            // starting from given token, move up to the root until it finds the first set of appropriate operations
            var list = new List<IndentBlockOperation>();

            var currentNode = startNode;
            while (currentNode != null)
            {
                list.AddRange(Formatter.GetIndentBlockOperations(this.formattingRules, currentNode));
                if (list.Any(o => o.Span.Contains(position)))
                {
                    break;
                }

                currentNode = currentNode.Parent;
            }

            // well, found no appropriate one
            if (list.Count == 0)
            {
                return null;
            }

            // now sort the found ones in right order
            list.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);

            return GetIndentOperations(syntaxTree.Root, list, position).FirstOrDefault();
        }
    }
}