// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal abstract class CompatAbstractFormattingRule : AbstractFormattingRule
    {
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddSuppressOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, in NextAnchorIndentationOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAnchorIndentationOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddIndentBlockOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, in NextAlignTokensOperationAction nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAlignTokensOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            var nextOperationCopy = nextOperation;
            return GetAdjustNewLinesOperationSlow(previousToken, currentToken, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
        {
            var nextOperationCopy = nextOperation;
            return GetAdjustSpacesOperationSlow(previousToken, currentToken, optionSet, ref nextOperationCopy);
        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
        /// filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddSuppressOperationsSlow(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, ref NextSuppressOperationAction nextOperation)
        {
            base.AddSuppressOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAnchorIndentationOperationsSlow(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAnchorIndentationOperationAction nextOperation)
        {
            base.AddAnchorIndentationOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddIndentBlockOperationsSlow(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, ref NextIndentBlockOperationAction nextOperation)
        {
            base.AddIndentBlockOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAlignTokensOperationsSlow(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAlignTokensOperationAction nextOperation)
        {
            base.AddAlignTokensOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustNewLinesOperation GetAdjustNewLinesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, ref NextGetAdjustNewLinesOperation nextOperation)
        {
            return base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustSpacesOperation GetAdjustSpacesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, ref NextGetAdjustSpacesOperation nextOperation)
        {
            return base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, in nextOperation);
        }
    }
}
