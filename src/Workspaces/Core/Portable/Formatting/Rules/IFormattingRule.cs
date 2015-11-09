// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Provide a custom formatting operation provider that can intercept/filter/replace default formatting operations.
    /// </summary>
    /// <remarks>All methods defined in this interface can be called concurrently. Must be thread-safe.</remarks>
    internal interface IFormattingRule
    {
        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
        /// filtering/replacing operations returned by NextOperation
        /// </summary>
        void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation);

        /// <summary>
        /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation);

        /// <summary>
        /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation);

        /// <summary>
        /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AlignTokensOperation> nextOperation);

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation);

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation);
    }
}
