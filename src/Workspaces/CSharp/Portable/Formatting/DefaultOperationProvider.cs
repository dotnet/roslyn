// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    // the default provider that will be called by the engine at the end of provider's chain.
    // there is no way for a user to be remove this provider.
    //
    // to reduce number of unnecessary heap allocations, most of them just return null.
    internal class DefaultOperationProvider : IFormattingRule
    {
        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AlignTokensOperation> nextOperation)
        {
        }

        public AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            return null;
        }

        // return 1 space for every token pairs as a default operation
        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            int space = currentToken.Kind() == SyntaxKind.EndOfFileToken ? 0 : 1;
            return FormattingOperations.CreateAdjustSpacesOperation(space, AdjustSpacesOption.DefaultSpacesIfOnSingleLine);
        }
    }
}
