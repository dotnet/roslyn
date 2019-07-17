// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal sealed class EndRegionFormattingRule : AbstractFormattingRule
    {
        public static readonly EndRegionFormattingRule Instance = new EndRegionFormattingRule();

        private EndRegionFormattingRule()
        {
        }

        private bool IsAfterEndRegionBeforeMethodDeclaration(SyntaxToken previousToken)
        {
            if (previousToken.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                var previousPreviousToken = previousToken.GetPreviousToken();
                return previousPreviousToken.Kind() == SyntaxKind.EndRegionKeyword;
            }

            return false;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (IsAfterEndRegionBeforeMethodDeclaration(previousToken))
            {
                return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
            }

            return nextOperation.Invoke();
        }
    }
}
