// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ChainedFormattingRules
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly OptionSet _optionSet;

        // Operation func caches
        //
        // we cache funcs to all operations kind to make sure we don't allocate any heap memory
        // during invocation of each method with continuation style.
        //
        // each of these operations will be called hundreds of thousands times during formatting,
        // make sure it doesn't allocate any memory during invocations.
        private readonly OperationCache<AdjustNewLinesOperation> _newLinesFuncCache;
        private readonly OperationCache<AdjustSpacesOperation> _spaceFuncCache;

        public ChainedFormattingRules(IEnumerable<AbstractFormattingRule> formattingRules, OptionSet set)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(set);

            _formattingRules = formattingRules.ToImmutableArray();
            _optionSet = set;

            // cache all funcs to reduce heap allocations
            _newLinesFuncCache = new OperationCache<AdjustNewLinesOperation>(
                (int index, SyntaxToken token1, SyntaxToken token2, in NextOperation<AdjustNewLinesOperation> next) => _formattingRules[index].GetAdjustNewLinesOperation(token1, token2, _optionSet, in next),
                this.GetContinuedOperations);

            _spaceFuncCache = new OperationCache<AdjustSpacesOperation>(
                (int index, SyntaxToken token1, SyntaxToken token2, in NextOperation<AdjustSpacesOperation> next) => _formattingRules[index].GetAdjustSpacesOperation(token1, token2, _optionSet, in next),
                this.GetContinuedOperations);
        }

        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode currentNode)
        {
            var action = new NextSuppressOperationAction(_formattingRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAnchorIndentationOperationAction(_formattingRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode currentNode)
        {
            var action = new NextIndentBlockOperationAction(_formattingRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode currentNode)
        {
            var action = new NextAlignTokensOperationAction(_formattingRules, index: 0, currentNode, _optionSet, list);
            action.Invoke();
        }

        public AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            return GetContinuedOperations(0, previousToken, currentToken, _newLinesFuncCache);
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            return GetContinuedOperations(0, previousToken, currentToken, _spaceFuncCache);
        }

        private TResult GetContinuedOperations<TResult>(int index, SyntaxToken token1, SyntaxToken token2, in OperationCache<TResult> funcCache)
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (index >= _formattingRules.Length)
            {
                return default;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                var continuation = new NextOperation<TResult>(index + 1, token1, token2, funcCache);
                return funcCache.NextOperation(index, token1, token2, in continuation);
            }
        }
    }
}
