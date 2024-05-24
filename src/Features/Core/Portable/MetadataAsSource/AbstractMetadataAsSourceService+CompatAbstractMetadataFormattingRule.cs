// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    protected abstract class CompatAbstractMetadataFormattingRule : AbstractMetadataFormattingRule
    {
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override void AddSuppressOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddSuppressOperationsSlow(list, node, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, in NextAnchorIndentationOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAnchorIndentationOperationsSlow(list, node, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddIndentBlockOperationsSlow(list, node, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, in NextAlignTokensOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAlignTokensOperationsSlow(list, node, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            var previousTokenCopy = previousToken;
            var currentTokenCopy = currentToken;
            var nextOperationCopy = nextOperation;
            return GetAdjustNewLinesOperationSlow(ref previousTokenCopy, ref currentTokenCopy, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override AdjustSpacesOperation GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            var previousTokenCopy = previousToken;
            var currentTokenCopy = currentToken;
            var nextOperationCopy = nextOperation;
            return GetAdjustSpacesOperationSlow(ref previousTokenCopy, ref currentTokenCopy, ref nextOperationCopy);
        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
        /// filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddSuppressOperationsSlow(ArrayBuilder<SuppressOperation> list, SyntaxNode node, ref NextSuppressOperationAction nextOperation)
            => base.AddSuppressOperations(list, node, in nextOperation);

        /// <summary>
        /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAnchorIndentationOperationsSlow(List<AnchorIndentationOperation> list, SyntaxNode node, ref NextAnchorIndentationOperationAction nextOperation)
            => base.AddAnchorIndentationOperations(list, node, in nextOperation);

        /// <summary>
        /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddIndentBlockOperationsSlow(List<IndentBlockOperation> list, SyntaxNode node, ref NextIndentBlockOperationAction nextOperation)
            => base.AddIndentBlockOperations(list, node, in nextOperation);

        /// <summary>
        /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAlignTokensOperationsSlow(List<AlignTokensOperation> list, SyntaxNode node, ref NextAlignTokensOperationAction nextOperation)
            => base.AddAlignTokensOperations(list, node, in nextOperation);

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustNewLinesOperation GetAdjustNewLinesOperationSlow(ref SyntaxToken previousToken, ref SyntaxToken currentToken, ref NextGetAdjustNewLinesOperation nextOperation)
            => base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustSpacesOperation GetAdjustSpacesOperationSlow(ref SyntaxToken previousToken, ref SyntaxToken currentToken, ref NextGetAdjustSpacesOperation nextOperation)
            => base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);
    }
}
