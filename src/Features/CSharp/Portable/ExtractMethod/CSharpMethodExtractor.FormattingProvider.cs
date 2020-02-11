﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor : MethodExtractor
    {
        private class FormattingRule : AbstractFormattingRule
        {
            public FormattingRule()
            {
            }

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // for extract method case, for a hybrid case, don't force rule, but preserve user style
                var operation = base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, in nextOperation);
                if (operation == null)
                {
                    return null;
                }

                if (operation.Option == AdjustNewLinesOption.ForceLinesIfOnSingleLine)
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(operation.Line, AdjustNewLinesOption.PreserveLines);
                }

                if (operation.Option != AdjustNewLinesOption.ForceLines)
                {
                    return operation;
                }

                if (previousToken.RawKind == (int)SyntaxKind.OpenBraceToken)
                {
                    return operation;
                }

                if (previousToken.BetweenFieldAndNonFieldMember(currentToken))
                {
                    // make sure to have at least 2 line breaks between field and other members except field
                    return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.PreserveLines);
                }

                if (previousToken.HasHybridTriviaBetween(currentToken))
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(operation.Line, AdjustNewLinesOption.PreserveLines);
                }

                return operation;
            }

            public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, in NextAnchorIndentationOperationAction nextOperation)
            {
                if (node.IsKind(SyntaxKind.SimpleLambdaExpression) || node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || node.IsKind(SyntaxKind.AnonymousMethodExpression))
                {
                    return;
                }

                nextOperation.Invoke();
            }
        }
    }
}
