// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        protected abstract class CompatAbstractMetadataFormattingRule : AbstractMetadataFormattingRule
        {
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextSuppressOperationAction nextOperation)
            {
                var nextOperationCopy = nextOperation;
                AddSuppressOperationsSlow(list, node, options, ref nextOperationCopy);
            }

            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextAnchorIndentationOperationAction nextOperation)
            {
                var nextOperationCopy = nextOperation;
                AddAnchorIndentationOperationsSlow(list, node, options, ref nextOperationCopy);
            }

            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextIndentBlockOperationAction nextOperation)
            {
                var nextOperationCopy = nextOperation;
                AddIndentBlockOperationsSlow(list, node, options, ref nextOperationCopy);
            }

            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextAlignTokensOperationAction nextOperation)
            {
                var nextOperationCopy = nextOperation;
                AddAlignTokensOperationsSlow(list, node, options, ref nextOperationCopy);
            }

            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, in NextGetAdjustNewLinesOperation nextOperation)
            {
                var nextOperationCopy = nextOperation;
                return GetAdjustNewLinesOperationSlow(previousToken, currentToken, options, ref nextOperationCopy);
            }

            [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public override sealed AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, in NextGetAdjustSpacesOperation nextOperation)
            {
                var nextOperationCopy = nextOperation;
                return GetAdjustSpacesOperationSlow(previousToken, currentToken, options, ref nextOperationCopy);
            }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

            /// <summary>
            /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
            /// filtering/replacing operations returned by NextOperation
            /// </summary>
            public virtual void AddSuppressOperationsSlow(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions options, ref NextSuppressOperationAction nextOperation)
                => base.AddSuppressOperations(list, node, options, in nextOperation);

            /// <summary>
            /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
            /// </summary>
            public virtual void AddAnchorIndentationOperationsSlow(List<AnchorIndentationOperation> list, SyntaxNode node, AnalyzerConfigOptions options, ref NextAnchorIndentationOperationAction nextOperation)
                => base.AddAnchorIndentationOperations(list, node, options, in nextOperation);

            /// <summary>
            /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
            /// </summary>
            public virtual void AddIndentBlockOperationsSlow(List<IndentBlockOperation> list, SyntaxNode node, AnalyzerConfigOptions options, ref NextIndentBlockOperationAction nextOperation)
                => base.AddIndentBlockOperations(list, node, options, in nextOperation);

            /// <summary>
            /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
            /// </summary>
            public virtual void AddAlignTokensOperationsSlow(List<AlignTokensOperation> list, SyntaxNode node, AnalyzerConfigOptions options, ref NextAlignTokensOperationAction nextOperation)
                => base.AddAlignTokensOperations(list, node, options, in nextOperation);

            /// <summary>
            /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
            /// </summary>
            public virtual AdjustNewLinesOperation GetAdjustNewLinesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, ref NextGetAdjustNewLinesOperation nextOperation)
                => base.GetAdjustNewLinesOperation(previousToken, currentToken, options, in nextOperation);

            /// <summary>
            /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
            /// </summary>
            public virtual AdjustSpacesOperation GetAdjustSpacesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, ref NextGetAdjustSpacesOperation nextOperation)
                => base.GetAdjustSpacesOperation(previousToken, currentToken, options, in nextOperation);
        }
    }
}
