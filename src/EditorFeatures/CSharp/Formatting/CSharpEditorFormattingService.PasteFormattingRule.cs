// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Formatting
{
    internal partial class CSharpEditorFormattingService : IEditorFormattingService
    {
        internal class PasteFormattingRule : AbstractFormattingRule
        {
            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (currentToken.Parent != null)
                {
                    var currentTokenParentParent = currentToken.Parent.Parent;
                    if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null &&
                        (currentTokenParentParent.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                         currentTokenParentParent.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                         currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression)))
                    {
                        return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                    }
                }

                return nextOperation.Invoke(in previousToken, in currentToken);
            }
        }
    }
}
