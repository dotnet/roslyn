// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// base IFormattingRule implementation that users can override to provide
    /// their own functionality
    /// </summary>
    internal abstract class AbstractFormattingRule : IFormattingRule
    {
        public virtual void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public virtual void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, AnalyzerConfigOptions optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public virtual void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, AnalyzerConfigOptions optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public virtual void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, AnalyzerConfigOptions optionSet, NextAction<AlignTokensOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public virtual AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            return nextOperation.Invoke();
        }

        public virtual AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            return nextOperation.Invoke();
        }
    }
}
