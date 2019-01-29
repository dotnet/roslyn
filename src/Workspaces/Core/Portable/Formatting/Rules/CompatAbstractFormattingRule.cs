// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal abstract class CompatAbstractFormattingRule : AbstractFormattingRule
    {
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextAction<SuppressOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddSuppressOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, in NextAction<AnchorIndentationOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAnchorIndentationOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextAction<IndentBlockOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddIndentBlockOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, in NextAction<AlignTokensOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            AddAlignTokensOperationsSlow(list, node, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            return GetAdjustNewLinesOperationSlow(previousToken, currentToken, optionSet, ref nextOperationCopy);
        }

        [Obsolete("Do not call this method directly (it will Stack Overflow).", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextOperation<AdjustSpacesOperation> nextOperation)
        {
            var nextOperationCopy = nextOperation;
            return GetAdjustSpacesOperationSlow(previousToken, currentToken, optionSet, ref nextOperationCopy);
        }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node either by itself or by
        /// filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddSuppressOperationsSlow(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAction<SuppressOperation> nextOperation)
        {
            base.AddSuppressOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AnchorIndentationOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAnchorIndentationOperationsSlow(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAction<AnchorIndentationOperation> nextOperation)
        {
            base.AddAnchorIndentationOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns IndentBlockOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddIndentBlockOperationsSlow(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAction<IndentBlockOperation> nextOperation)
        {
            base.AddIndentBlockOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AlignTokensOperations under a node either by itself or by filtering/replacing operations returned by NextOperation
        /// </summary>
        public virtual void AddAlignTokensOperationsSlow(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, ref NextAction<AlignTokensOperation> nextOperation)
        {
            base.AddAlignTokensOperations(list, node, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustNewLinesOperation GetAdjustNewLinesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, ref NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            return base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, in nextOperation);
        }

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens either by itself or by filtering/replacing a operation returned by NextOperation
        /// </summary>
        public virtual AdjustSpacesOperation GetAdjustSpacesOperationSlow(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, ref NextOperation<AdjustSpacesOperation> nextOperation)
        {
            return base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, in nextOperation);
        }
    }
}
