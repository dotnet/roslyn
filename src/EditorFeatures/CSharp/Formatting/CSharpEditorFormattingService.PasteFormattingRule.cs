// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Formatting
{
    internal partial class CSharpEditorFormattingService : IEditorFormattingService
    {
        internal class PasteFormattingRule : AbstractFormattingRule
        {
            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (currentToken.Parent != null)
                {
                    var currentTokenParentParent = currentToken.Parent.Parent;
                    if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
                        (currentTokenParentParent.Kind() == SyntaxKind.SimpleLambdaExpression ||
                         currentTokenParentParent.Kind() == SyntaxKind.ParenthesizedLambdaExpression ||
                         currentTokenParentParent.Kind() == SyntaxKind.AnonymousMethodExpression))
                    {
                        return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                    }
                }

                return nextOperation.Invoke();
            }
        }
    }
}
