// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Provide a custom formatting operation provider that can intercept/filter/replace default formatting operations.
    /// </summary>
    /// <remarks>All methods defined in this class can be called concurrently. Must be thread-safe.</remarks>
    internal abstract class AbstractFormattingRule
    {
        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
        /// filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextSuppressOperationAction nextOperation)
            => nextOperation.Invoke();

        /// <summary>
        /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextAnchorIndentationOperationAction nextOperation)
            => nextOperation.Invoke();

        /// <summary>
        /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextIndentBlockOperationAction nextOperation)
            => nextOperation.Invoke();

        /// <summary>
        /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextAlignTokensOperationAction nextOperation)
            => nextOperation.Invoke();

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, in NextGetAdjustNewLinesOperation nextOperation)
            => nextOperation.Invoke();

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, in NextGetAdjustSpacesOperation nextOperation)
            => nextOperation.Invoke();
    }
}
