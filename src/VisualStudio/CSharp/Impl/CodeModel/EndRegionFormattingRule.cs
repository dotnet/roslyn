// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal sealed class EndRegionFormattingRule : AbstractFormattingRule
    {
        public static readonly EndRegionFormattingRule Instance = new();

        private EndRegionFormattingRule()
        {
        }

        private static bool IsAfterEndRegionBeforeMethodDeclaration(SyntaxToken previousToken)
        {
            if (previousToken.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                var previousPreviousToken = previousToken.GetPreviousToken();
                return previousPreviousToken.Kind() == SyntaxKind.EndRegionKeyword;
            }

            return false;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (IsAfterEndRegionBeforeMethodDeclaration(previousToken))
            {
                return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }
    }
}
