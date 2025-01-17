// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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

    private static bool NeedsWrapping(ConditionalExpressionSyntax conditionalExpression)
        => conditionalExpression.HasAnnotation(SpecializedFormattingAnnotation) ||
           conditionalExpression.ColonToken.LeadingTrivia.Any(t => t.IsSingleOrMultiLineComment());

    private static bool IsQuestionOrColonOfNewConditional(SyntaxToken token)
        => token.Kind() is SyntaxKind.QuestionToken or SyntaxKind.ColonToken &&
           token.Parent is ConditionalExpressionSyntax conditionalExpression &&
           NeedsWrapping(conditionalExpression);

    public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(
        in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
    {
        // Check if we want to force the ? and : to each be put onto the following line.
        if (IsQuestionOrColonOfNewConditional(currentToken))
            return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);

        return nextOperation.Invoke(in previousToken, in currentToken);
    }

    public override void AddIndentBlockOperations(
        List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
    {
        if (node is ConditionalExpressionSyntax conditional &&
            NeedsWrapping(conditional))
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
