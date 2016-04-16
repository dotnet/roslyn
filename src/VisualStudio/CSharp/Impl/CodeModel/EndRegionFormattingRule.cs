// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal class EndRegionFormattingRule : IFormattingRule
    {
        public void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        public void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AlignTokensOperation> nextOperation)
        {
            nextOperation.Invoke(list);
        }

        private bool IsAfterEndRegionBeforeMethodDeclaration(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (previousToken.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                var previousPreviousToken = previousToken.GetPreviousToken();
                return previousPreviousToken.Kind() == SyntaxKind.EndRegionKeyword;
            }

            return false;
        }

        public AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            if (IsAfterEndRegionBeforeMethodDeclaration(previousToken, currentToken))
            {
                return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
            }

            return nextOperation.Invoke();
        }

        public AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            return nextOperation.Invoke();
        }
    }
}
