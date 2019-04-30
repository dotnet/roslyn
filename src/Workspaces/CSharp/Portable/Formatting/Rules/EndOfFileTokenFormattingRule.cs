// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting.Rules;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class EndOfFileTokenFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp End Of File Token Formatting Rule";

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            // * <End Of File> case for C#, make sure we don't insert new line between * and <End of
            // File> tokens.
            if (currentToken.Kind() == SyntaxKind.EndOfFileToken)
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return nextOperation.Invoke();
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
        {
            // * <End Of File) case
            // for C#, make sure we have nothing between these two tokens
            if (currentToken.Kind() == SyntaxKind.EndOfFileToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            return nextOperation.Invoke();
        }
    }
}
