// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal abstract class CompatAbstractFormattingRule : AbstractFormattingRule
    {
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
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
