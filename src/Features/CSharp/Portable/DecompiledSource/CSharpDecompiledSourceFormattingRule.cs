// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.CSharp.DecompiledSource;

internal sealed class CSharpDecompiledSourceFormattingRule : AbstractFormattingRule
{
    public static readonly AbstractFormattingRule Instance = new CSharpDecompiledSourceFormattingRule();

    private CSharpDecompiledSourceFormattingRule()
    {
    }

    public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(
        in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
    {
        var operation = GetAdjustNewLinesOperation(previousToken, currentToken);
        return operation ?? nextOperation.Invoke(in previousToken, in currentToken);
    }

    private static AdjustNewLinesOperation? GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        // To help code not look too tightly packed, we place a blank line after every statement that ends with a
        // `}` (unless it's also followed by another `}`).
        if (previousToken.Kind() != SyntaxKind.CloseBraceToken)
            return null;

        if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
            return null;

        if (previousToken.Parent == null || currentToken.Parent == null)
            return null;

        var previousStatement = previousToken.Parent.FirstAncestorOrSelf<StatementSyntax>();
        var nextStatement = currentToken.Parent.FirstAncestorOrSelf<StatementSyntax>();

        if (previousStatement == null || nextStatement == null || previousStatement == nextStatement)
            return null;

        // Ensure that we're only updating the whitespace between statements.
        if (previousStatement.GetLastToken() != previousToken || nextStatement.GetFirstToken() != currentToken)
            return null;

        // Ensure a blank line between these two.
        return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);
    }
}
