// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class IndentBlockFormattingRule : BaseFormattingRule
{
    internal const string Name = "CSharp IndentBlock Formatting Rule";

    private readonly CSharpSyntaxFormattingOptions _options;

    public IndentBlockFormattingRule()
        : this(CSharpSyntaxFormattingOptions.Default)
    {
    }

    private IndentBlockFormattingRule(CSharpSyntaxFormattingOptions options)
    {
        _options = options;
    }

    public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
    {
        var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

        if (_options.LabelPositioning == newOptions.LabelPositioning &&
            _options.Indentation == newOptions.Indentation)
        {
            return this;
        }

        return new IndentBlockFormattingRule(newOptions);
    }

    public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
    {
        nextOperation.Invoke();

        AddAlignmentBlockOperation(list, node);

        AddBlockIndentationOperation(list, node);

        AddBracketIndentationOperation(list, node);

        AddLabelIndentationOperation(list, node);

        AddSwitchIndentationOperation(list, node);

        AddEmbeddedStatementsIndentationOperation(list, node);

        AddTypeParameterConstraintClauseOperation(list, node);
    }

    private static void AddTypeParameterConstraintClauseOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node is TypeParameterConstraintClauseSyntax { Parent: { } declaringNode })
        {
            var baseToken = declaringNode.GetFirstToken();
            AddIndentBlockOperation(list, baseToken, node.GetFirstToken(), node.GetLastToken());
        }
    }

    private void AddSwitchIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        if (node is not SwitchSectionSyntax section)
        {
            return;
        }

        // can this ever happen?
        if (section.Labels.Count == 0 &&
            section.Statements.Count == 0)
        {
            return;
        }

        if (!_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) && !_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock))
        {
            // Never indent
            return;
        }

        var alwaysIndent = _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) && _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock);
        if (!alwaysIndent)
        {
            // Only one of these values can be true at this point.
            Debug.Assert(_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents) != _options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock));

            var firstStatementIsBlock = section.Statements is [(kind: SyntaxKind.Block), ..];
            if (_options.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock) != firstStatementIsBlock)
            {
                return;
            }
        }

        // see whether we are the last statement
        RoslynDebug.AssertNotNull(node.Parent);
        var switchStatement = (SwitchStatementSyntax)node.Parent;
        var lastSection = switchStatement.Sections.Last() == node;

        if (section.Statements.Count == 0)
        {
            // even if there is no statement under section, we still want indent operation
            var lastTokenOfLabel = section.Labels.Last().GetLastToken(includeZeroWidth: true);
            var nextToken = lastTokenOfLabel.GetNextToken(includeZeroWidth: true);

            AddIndentBlockOperation(list, lastTokenOfLabel, lastTokenOfLabel,
                lastSection ?
                    TextSpan.FromBounds(lastTokenOfLabel.FullSpan.End, nextToken.SpanStart) : TextSpan.FromBounds(lastTokenOfLabel.FullSpan.End, lastTokenOfLabel.FullSpan.End));
            return;
        }

        var startToken = section.Statements.First().GetFirstToken(includeZeroWidth: true);
        var endToken = section.Statements.Last().GetLastToken(includeZeroWidth: true);

        // see whether we are the last statement
        var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);
        span = lastSection ? span : TextSpan.FromBounds(span.Start, endToken.FullSpan.End);

        AddIndentBlockOperation(list, startToken, endToken, span);
    }

    private void AddLabelIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // label statement
        if (node is LabeledStatementSyntax labeledStatement)
        {
            if (_options.LabelPositioning == LabelPositionOptions.OneLess)
            {
                AddUnindentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
            }
            else if (_options.LabelPositioning == LabelPositionOptions.LeftMost)
            {
                AddAbsoluteZeroIndentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
            }
        }
    }

    private static void AddAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        switch (node)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
                SetAlignmentBlockOperation(list, simpleLambda, simpleLambda.Body);
                return;
            case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                SetAlignmentBlockOperation(list, parenthesizedLambda, parenthesizedLambda.Body);
                return;
            case AnonymousMethodExpressionSyntax anonymousMethod:
                SetAlignmentBlockOperation(list, anonymousMethod, anonymousMethod.Block);
                return;
            case BaseObjectCreationExpressionSyntax objectCreation when objectCreation.Initializer != null:
                SetAlignmentBlockOperation(list, objectCreation, objectCreation.Initializer);
                return;
            case AnonymousObjectCreationExpressionSyntax anonymousObjectCreation:
                SetAlignmentBlockOperation(list, anonymousObjectCreation.NewKeyword, anonymousObjectCreation.OpenBraceToken, anonymousObjectCreation.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, arrayCreation.NewKeyword, arrayCreation.Initializer.OpenBraceToken, arrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ImplicitArrayCreationExpressionSyntax implicitArrayCreation when implicitArrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, implicitArrayCreation.NewKeyword, implicitArrayCreation.Initializer.OpenBraceToken, implicitArrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case StackAllocArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, arrayCreation.StackAllocKeyword, arrayCreation.Initializer.OpenBraceToken, arrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation when implicitArrayCreation.Initializer != null:
                SetAlignmentBlockOperation(list, implicitArrayCreation.StackAllocKeyword, implicitArrayCreation.Initializer.OpenBraceToken, implicitArrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case SwitchExpressionSyntax switchExpression:
                SetAlignmentBlockOperation(list, switchExpression.GetFirstToken(), switchExpression.OpenBraceToken, switchExpression.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case WithExpressionSyntax withExpression:
                SetAlignmentBlockOperation(list, withExpression.GetFirstToken(), withExpression.Initializer.OpenBraceToken, withExpression.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            case PropertyPatternClauseSyntax propertyPatternClause:
                if (propertyPatternClause.Parent is RecursivePatternSyntax { Parent: { } recursivePatternParent })
                {
                    var baseTokenForAlignment = recursivePatternParent.GetFirstToken();
                    if (baseTokenForAlignment == propertyPatternClause.OpenBraceToken)
                    {
                        // It only makes sense to set the alignment for the '{' when it's on a separate line from
                        // the base token for alignment. This is never the case when they are the same token.
                        return;
                    }

                    SetAlignmentBlockOperation(list, baseTokenForAlignment, propertyPatternClause.OpenBraceToken, propertyPatternClause.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine | IndentBlockOption.IndentIfConditionOfAnchorToken);
                }

                return;
        }
    }

    private static void SetAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode baseNode, SyntaxNode body)
    {
        var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;

        var baseToken = baseNode.GetFirstToken(includeZeroWidth: true);
        var firstToken = body.GetFirstToken(includeZeroWidth: true);
        var lastToken = body.GetLastToken(includeZeroWidth: true);

        SetAlignmentBlockOperation(list, baseToken, firstToken, lastToken, option);
    }

    private void AddBlockIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        var bracePair = node.GetBracePair();

        // don't put block indentation operation if the block only contains label statement
        if (!bracePair.IsValidBracketOrBracePair())
        {
            return;
        }

        // for lambda, set alignment around braces so that users can put brace wherever they want
        if (node.IsLambdaBodyBlock() || node.IsAnonymousMethodBlock() || node.Kind() is SyntaxKind.PropertyPatternClause or SyntaxKind.SwitchExpression)
        {
            AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
        }

        // For ArrayInitializationExpression, set indent to relative to the open brace so the content is properly indented
        if (node.IsKind(SyntaxKind.ArrayInitializerExpression) && node.Parent != null && node.Parent.IsKind(SyntaxKind.ArrayCreationExpression))
        {
            AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
        }

        if (node is BlockSyntax && !_options.Indentation.HasFlag(IndentationPlacement.BlockContents))
        {
            // do not add indent operation for block
            return;
        }

        if (node is SwitchStatementSyntax && !_options.Indentation.HasFlag(IndentationPlacement.SwitchSection))
        {
            // do not add indent operation for switch statement
            return;
        }

        AddIndentBlockOperation(list, bracePair.openBrace.GetNextToken(includeZeroWidth: true), bracePair.closeBrace.GetPreviousToken(includeZeroWidth: true));
    }

    private static void AddBracketIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        var bracketPair = node.GetBracketPair();

        if (!bracketPair.IsValidBracketOrBracePair())
            return;

        if (node.Parent != null && node.Kind() is SyntaxKind.ListPattern or SyntaxKind.CollectionExpression)
        {
            // Brackets in list patterns are formatted like blocks, so align close bracket with open bracket
            AddIndentBlockOperation(list, bracketPair.openBracket.GetNextToken(includeZeroWidth: true), bracketPair.closeBracket.GetPreviousToken(includeZeroWidth: true));

            // If we have:
            //
            // return Goo([ //<-- determining indentation here.
            //
            // Then we want to compute the indentation relative to the construct that the collection expression is
            // attached to.  So ask to be relative to the start of the line the prior token is on if we're on the
            // same line as it.
            var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;

            var firstToken = node.GetFirstToken(includeZeroWidth: true);
            var lastToken = node.GetLastToken(includeZeroWidth: true);
            var baseToken = firstToken.GetPreviousToken(includeZeroWidth: true);

            SetAlignmentBlockOperation(list, baseToken, firstToken, lastToken, option);
        }
    }

    private static void AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(List<IndentBlockOperation> list, (SyntaxToken openBrace, SyntaxToken closeBrace) bracePair)
    {
        var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;
        SetAlignmentBlockOperation(list, bracePair.openBrace, bracePair.openBrace.GetNextToken(includeZeroWidth: true), bracePair.closeBrace, option);
    }

    private static void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
    {
        // increase indentation - embedded statement cases
        if (node is IfStatementSyntax ifStatement && ifStatement.Statement != null && !(ifStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, ifStatement.Statement);
            return;
        }

        if (node is ElseClauseSyntax elseClause && elseClause.Statement != null)
        {
            if (elseClause.Statement is not (BlockSyntax or IfStatementSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, elseClause.Statement);
            }

            return;
        }

        if (node is WhileStatementSyntax whileStatement && whileStatement.Statement != null && !(whileStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, whileStatement.Statement);
            return;
        }

        if (node is ForStatementSyntax forStatement && forStatement.Statement != null && !(forStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, forStatement.Statement);
            return;
        }

        if (node is CommonForEachStatementSyntax foreachStatement && foreachStatement.Statement != null && !(foreachStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, foreachStatement.Statement);
            return;
        }

        if (node is UsingStatementSyntax usingStatement && usingStatement.Statement != null && !(usingStatement.Statement is BlockSyntax || usingStatement.Statement is UsingStatementSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, usingStatement.Statement);
            return;
        }

        if (node is FixedStatementSyntax fixedStatement && fixedStatement.Statement != null && !(fixedStatement.Statement is BlockSyntax || fixedStatement.Statement is FixedStatementSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, fixedStatement.Statement);
            return;
        }

        if (node is DoStatementSyntax doStatement && doStatement.Statement != null && !(doStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, doStatement.Statement);
            return;
        }

        if (node is LockStatementSyntax lockStatement && lockStatement.Statement != null && !(lockStatement.Statement is BlockSyntax))
        {
            AddEmbeddedStatementsIndentationOperation(list, lockStatement.Statement);
            return;
        }
    }

    private static void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, StatementSyntax statement)
    {
        var firstToken = statement.GetFirstToken(includeZeroWidth: true);
        var lastToken = statement.GetLastToken(includeZeroWidth: true);

        if (lastToken.IsMissing)
        {
            // embedded statement is not done, consider following as part of embedded statement
            AddIndentBlockOperation(list, firstToken, lastToken);
        }
        else
        {
            // embedded statement is done
            AddIndentBlockOperation(list, firstToken, lastToken, TextSpan.FromBounds(firstToken.FullSpan.Start, lastToken.FullSpan.End));
        }
    }
}
