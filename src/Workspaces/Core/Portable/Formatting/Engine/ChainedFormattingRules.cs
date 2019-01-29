// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ChainedFormattingRules
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly OptionSet _optionSet;

        public ChainedFormattingRules(IEnumerable<AbstractFormattingRule> formattingRules, OptionSet set)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(set);

            _formattingRules = formattingRules.ToImmutableArray();
            _optionSet = set;
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
            var action = new NextGetAdjustNewLinesOperation(_formattingRules, index: 0, previousToken, currentToken, _optionSet);
            return action.Invoke();
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            var action = new NextGetAdjustSpacesOperation(_formattingRules, index: 0, previousToken, currentToken, _optionSet);
            return action.Invoke();
        }
    }
}
