// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Utilities
{
    internal sealed class BlankLineInGeneratedMethodFormattingRule : AbstractFormattingRule
    {
        public static readonly BlankLineInGeneratedMethodFormattingRule Instance = new BlankLineInGeneratedMethodFormattingRule();

        private BlankLineInGeneratedMethodFormattingRule()
        {
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            // case: insert blank line in empty method body.
            if (previousToken.Kind() == SyntaxKind.OpenBraceToken &&
                currentToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                if (currentToken.Parent.Kind() == SyntaxKind.Block &&
                    currentToken.Parent.Parent.Kind() == SyntaxKind.MethodDeclaration)
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
                }
            }

            return nextOperation.Invoke();
        }
    }
}
