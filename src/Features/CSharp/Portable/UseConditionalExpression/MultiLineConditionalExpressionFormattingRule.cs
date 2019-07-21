// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    /// <summary>
    /// Special formatting rule that will convert a conditional expression into the following
    /// form if it has the <see cref="SpecializedFormattingAnnotation"/> on it:
    /// 
    /// <code>
    ///     var v = expr
    ///         ? whenTrue
    ///         : whenFalse
    /// </code>
    /// 
    /// i.e. both branches will be on a newline, indented once from the parent indentation.
    /// </summary>
    internal class MultiLineConditionalExpressionFormattingRule : AbstractFormattingRule
    {
        public static readonly AbstractFormattingRule Instance = new MultiLineConditionalExpressionFormattingRule();

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
            SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (IsQuestionOrColonOfNewConditional(currentToken))
            {
                // We want to force the ? and : to each be put onto the following line.
                return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
            }

            return nextOperation.Invoke();
        }

        public override void AddIndentBlockOperations(
            List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
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

            nextOperation.Invoke();
        }
    }
}
