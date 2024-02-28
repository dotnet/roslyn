// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionCodeFixHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;

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

    private static bool IsQuestionOrColonOfNewConditional(SyntaxToken token)
        => token.Kind() is SyntaxKind.QuestionToken or SyntaxKind.ColonToken && token.Parent.HasAnnotation(SpecializedFormattingAnnotation);

    public override AdjustNewLinesOperation GetAdjustNewLinesOperation(
        in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
    {
        if (IsQuestionOrColonOfNewConditional(currentToken))
        {
            // We want to force the ? and : to each be put onto the following line.
            return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
        }

        return nextOperation.Invoke(in previousToken, in currentToken);
    }

    public override void AddIndentBlockOperations(
        List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
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
