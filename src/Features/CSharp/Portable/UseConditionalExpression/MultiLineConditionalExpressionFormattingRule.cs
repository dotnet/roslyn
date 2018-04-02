// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionForAssignmentHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    internal class MultiLineConditionalExpressionFormattingRule : AbstractFormattingRule
    {
        public static readonly IFormattingRule Instance = new MultiLineConditionalExpressionFormattingRule();

        private MultiLineConditionalExpressionFormattingRule()
        {
        }

        private bool IsQuestionOrColonOfNewConditional(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.QuestionToken ||
                token.Kind() == SyntaxKind.ColonToken)
            {
                return token.Parent.HasAnnotation(SpecializedFormattingAnnotation);
            }

            return false;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(
            SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            if (IsQuestionOrColonOfNewConditional(currentToken))
            {
                // We want to force the ? and : to each be put onto the following line.
                return FormattingOperations.CreateAdjustNewLinesOperation(
                    1, AdjustNewLinesOption.ForceLines);
            }

            return nextOperation.Invoke();
        }

        public override void AddIndentBlockOperations(
            List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            if (node.HasAnnotation(SpecializedFormattingAnnotation) &&
                node is ConditionalExpressionSyntax conditional)
            {
                var statement = conditional.FirstAncestorOrSelf<StatementSyntax>();
                if (statement != null)
                {
                    var baseToken = statement.GetFirstToken();

                    // we want to indent the ? and : in one level from the containing statement.
                    list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                        baseToken, conditional.QuestionToken, conditional.WhenTrue.GetLastToken(),
                        indentationDelta: 1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
                    list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                        baseToken, conditional.ColonToken, conditional.WhenFalse.GetLastToken(),
                        indentationDelta: 1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
                    return;
                }
            }

            nextOperation.Invoke(list);
        }
    }
}
