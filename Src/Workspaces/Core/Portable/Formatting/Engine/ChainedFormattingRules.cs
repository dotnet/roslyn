// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ChainedFormattingRules
    {
        private readonly List<IFormattingRule> formattingRules;
        private readonly OptionSet optionSet;

        // Operation func caches
        //
        // we cache funcs to all operations kind to make sure we don't allocate any heap memory
        // during invocation of each method with continuation style.
        //
        // each of these operations will be called hundreds of thousands times during formatting,
        // make sure it doesn't allocate any memory during invocations.
        private readonly ActionCache<SuppressOperation> suppressWrappingFuncCache;
        private readonly ActionCache<AnchorIndentationOperation> anchorFuncCache;
        private readonly ActionCache<IndentBlockOperation> indentFuncCache;
        private readonly ActionCache<AlignTokensOperation> alignFuncCache;
        private readonly OperationCache<AdjustNewLinesOperation> newLinesFuncCache;
        private readonly OperationCache<AdjustSpacesOperation> spaceFuncCache;

        public ChainedFormattingRules(IEnumerable<IFormattingRule> formattingRules, OptionSet set)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(set);

            this.formattingRules = formattingRules.ToList();
            this.optionSet = set;

            // cache all funcs to reduce heap allocations
            this.suppressWrappingFuncCache = new ActionCache<SuppressOperation>(
                (index, list, node, next) => this.formattingRules[index].AddSuppressOperations(list, node, optionSet, next),
                this.AddContinuedOperations);

            this.anchorFuncCache = new ActionCache<AnchorIndentationOperation>(
                (index, list, node, next) => this.formattingRules[index].AddAnchorIndentationOperations(list, node, optionSet, next),
                this.AddContinuedOperations);

            this.indentFuncCache = new ActionCache<IndentBlockOperation>(
                (index, list, node, next) => this.formattingRules[index].AddIndentBlockOperations(list, node, optionSet, next),
                this.AddContinuedOperations);

            this.alignFuncCache = new ActionCache<AlignTokensOperation>(
                (index, list, node, next) => this.formattingRules[index].AddAlignTokensOperations(list, node, optionSet, next),
                this.AddContinuedOperations);

            this.newLinesFuncCache = new OperationCache<AdjustNewLinesOperation>(
                (index, token1, token2, next) => this.formattingRules[index].GetAdjustNewLinesOperation(token1, token2, optionSet, next),
                this.GetContinuedOperations);

            this.spaceFuncCache = new OperationCache<AdjustSpacesOperation>(
                (index, token1, token2, next) => this.formattingRules[index].GetAdjustSpacesOperation(token1, token2, optionSet, next),
                this.GetContinuedOperations);
        }

        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode currentNode)
        {
            AddContinuedOperations(0, list, currentNode, this.suppressWrappingFuncCache);
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode currentNode)
        {
            AddContinuedOperations(0, list, currentNode, this.anchorFuncCache);
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode currentNode)
        {
            AddContinuedOperations(0, list, currentNode, this.indentFuncCache);
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode currentNode)
        {
            AddContinuedOperations(0, list, currentNode, this.alignFuncCache);
        }

        public AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            return GetContinuedOperations(0, previousToken, currentToken, this.newLinesFuncCache);
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            return GetContinuedOperations(0, previousToken, currentToken, this.spaceFuncCache);
        }

        private void AddContinuedOperations<TArg1>(int index, List<TArg1> arg1, SyntaxNode node, IActionHolder<TArg1> actionCache)
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (index >= this.formattingRules.Count)
            {
                return;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                var continuation = new NextAction<TArg1>(index + 1, node, actionCache);
                actionCache.NextOperation(index, arg1, node, continuation);
                return;
            }
        }

        private TResult GetContinuedOperations<TResult>(int index, SyntaxToken token1, SyntaxToken token2, IOperationHolder<TResult> funcCache)
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (index >= this.formattingRules.Count)
            {
                return default(TResult);
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                var continuation = new NextOperation<TResult>(index + 1, token1, token2, funcCache);
                return funcCache.NextOperation(index, token1, token2, continuation);
            }
        }
    }
}
