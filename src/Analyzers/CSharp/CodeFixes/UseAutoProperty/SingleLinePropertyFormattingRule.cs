// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

internal sealed partial class CSharpUseAutoPropertyCodeFixProvider
{
    private sealed class SingleLinePropertyFormattingRule : AbstractFormattingRule
    {
        private static bool ForceSingleSpace(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
                return true;

            if (previousToken.IsKind(SyntaxKind.OpenBraceToken) && previousToken.Parent.IsKind(SyntaxKind.AccessorList))
                return true;

            if (currentToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
                return true;

            if (previousToken.IsKind(SyntaxKind.SemicolonToken) && currentToken.Parent is AccessorDeclarationSyntax)
                return true;

            return false;
        }

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
                return null;

            return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
        }

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
                return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);

            return base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);
        }
    }
}
