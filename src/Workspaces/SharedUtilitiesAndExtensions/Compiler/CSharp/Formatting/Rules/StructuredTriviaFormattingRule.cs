// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class StructuredTriviaFormattingRule : BaseFormattingRule
{
    internal const string Name = "CSharp Structured Trivia Formatting Rule";

    public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
    {
        if (previousToken.Parent is StructuredTriviaSyntax || currentToken.Parent is StructuredTriviaSyntax)
        {
            return null;
        }

        return nextOperation.Invoke(in previousToken, in currentToken);
    }

    public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
    {
        if (previousToken.Parent is StructuredTriviaSyntax || currentToken.Parent is StructuredTriviaSyntax)
        {
            // this doesn't take care of all cases where tokens belong to structured trivia. this is only for cases we care
            if (previousToken.Kind() == SyntaxKind.HashToken && SyntaxFacts.IsPreprocessorKeyword(currentToken.Kind()))
            {
                return CreateAdjustSpacesOperation(space: 0, option: AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            if (previousToken.Kind() == SyntaxKind.RegionKeyword && currentToken.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                return CreateAdjustSpacesOperation(space: 0, option: AdjustSpacesOption.PreserveSpaces);
            }

            if (currentToken.Kind() == SyntaxKind.EndOfDirectiveToken)
            {
                return CreateAdjustSpacesOperation(space: 0, option: AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }
        }

        return nextOperation.Invoke(in previousToken, in currentToken);
    }
}
