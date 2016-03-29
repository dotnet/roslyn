// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportFormattingRule(Name, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = StructuredTriviaFormattingRule.Name)]
    internal class IndentBlockFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp IndentBlock Formatting Rule";

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            AddAlignmentBlockOperation(list, node, optionSet);

            AddBlockIndentationOperation(list, node, optionSet);

            AddLabelIndentationOperation(list, node, optionSet);

            AddSwitchIndentationOperation(list, node, optionSet);

            AddEmbeddedStatementsIndentationOperation(list, node);

            AddTypeParameterConstraintClauseOperation(list, node);
        }

        private void AddTypeParameterConstraintClauseOperation(List<IndentBlockOperation> list, SyntaxNode node)
        {
            var typeParameterConstraintClause = node as TypeParameterConstraintClauseSyntax;
            if (typeParameterConstraintClause != null)
            {
                var declaringNode = typeParameterConstraintClause.Parent;
                var baseToken = declaringNode.GetFirstToken();
                AddIndentBlockOperation(list, baseToken, node.GetFirstToken(), node.GetLastToken());
            }
        }

        private void AddSwitchIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            var section = node as SwitchSectionSyntax;
            if (section == null || !optionSet.GetOption(CSharpFormattingOptions.IndentSwitchCaseSection))
            {
                return;
            }

            // can this ever happen?
            if (section.Labels.Count == 0 &&
                section.Statements.Count == 0)
            {
                return;
            }

            // see whether we are the last statement
            var switchStatement = node.Parent as SwitchStatementSyntax;
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

        private void AddLabelIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            // label statement
            var labeledStatement = node as LabeledStatementSyntax;
            if (labeledStatement != null)
            {
                var labelPositioningOption = optionSet.GetOption(CSharpFormattingOptions.LabelPositioning);

                if (labelPositioningOption == LabelPositionOptions.OneLess)
                {
                    AddUnindentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
                }
                else if (labelPositioningOption == LabelPositionOptions.LeftMost)
                {
                    AddAbsoluteZeroIndentBlockOperation(list, labeledStatement.Identifier, labeledStatement.ColonToken);
                }
            }
        }

        private void AddAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            var simpleLambda = node as SimpleLambdaExpressionSyntax;
            if (simpleLambda != null)
            {
                SetAlignmentBlockOperation(list, simpleLambda, simpleLambda.Body);
                return;
            }

            var parenthesizedLambda = node as ParenthesizedLambdaExpressionSyntax;
            if (parenthesizedLambda != null)
            {
                SetAlignmentBlockOperation(list, parenthesizedLambda, parenthesizedLambda.Body);
                return;
            }

            var anonymousMethod = node as AnonymousMethodExpressionSyntax;
            if (anonymousMethod != null)
            {
                SetAlignmentBlockOperation(list, anonymousMethod, anonymousMethod.Block);
                return;
            }

            var objectCreation = node as ObjectCreationExpressionSyntax;
            if (objectCreation != null && objectCreation.Initializer != null)
            {
                SetAlignmentBlockOperation(list, objectCreation, objectCreation.Initializer);
                return;
            }

            var anonymousObjectCreation = node as AnonymousObjectCreationExpressionSyntax;
            if (anonymousObjectCreation != null)
            {
                SetAlignmentBlockOperation(list, anonymousObjectCreation.NewKeyword, anonymousObjectCreation.OpenBraceToken, anonymousObjectCreation.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            }

            var arrayCreation = node as ArrayCreationExpressionSyntax;
            if (arrayCreation != null && arrayCreation.Initializer != null)
            {
                SetAlignmentBlockOperation(list, arrayCreation.NewKeyword, arrayCreation.Initializer.OpenBraceToken, arrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            }

            var implicitArrayCreation = node as ImplicitArrayCreationExpressionSyntax;
            if (implicitArrayCreation != null && implicitArrayCreation.Initializer != null)
            {
                SetAlignmentBlockOperation(list, implicitArrayCreation.NewKeyword, implicitArrayCreation.Initializer.OpenBraceToken, implicitArrayCreation.Initializer.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            }

            var propertyPattern = node as PropertyPatternSyntax;
            if (propertyPattern?.PatternList != null)
            {
                SetAlignmentBlockOperation(list, propertyPattern.Type.GetFirstToken(), propertyPattern.PatternList.OpenBraceToken, propertyPattern.PatternList.CloseBraceToken, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine);
                return;
            }
        }

        private void SetAlignmentBlockOperation(List<IndentBlockOperation> list, SyntaxNode baseNode, SyntaxNode body)
        {
            var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;

            var baseToken = baseNode.GetFirstToken(includeZeroWidth: true);
            var firstToken = body.GetFirstToken(includeZeroWidth: true);
            var lastToken = body.GetLastToken(includeZeroWidth: true);

            SetAlignmentBlockOperation(list, baseToken, firstToken, lastToken, option);
        }

        private void AddBlockIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            var bracePair = node.GetBracePair();

            // don't put block indentation operation if the block only contains label statement
            if (!bracePair.IsValidBracePair())
            {
                return;
            }

            // for lambda, set alignment around braces so that users can put brace wherever they want
            if (node.IsLambdaBodyBlock() || node.IsAnonymousMethodBlock())
            {
                AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
            }

            // For ArrayInitializationExpression, set indent to relative to the open brace so the content is properly indented
            if (node.IsKind(SyntaxKind.ArrayInitializerExpression) && node.Parent != null && node.Parent.IsKind(SyntaxKind.ArrayCreationExpression))
            {
                AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(list, bracePair);
            }

            if (node is BlockSyntax && !optionSet.GetOption(CSharpFormattingOptions.IndentBlock))
            {
                // do not add indent operation for block
                return;
            }

            if (node is SwitchStatementSyntax && !optionSet.GetOption(CSharpFormattingOptions.IndentSwitchSection))
            {
                // do not add indent operation for switch statement
                return;
            }

            AddIndentBlockOperation(list, bracePair.Item1.GetNextToken(includeZeroWidth: true), bracePair.Item2.GetPreviousToken(includeZeroWidth: true));
        }

        private void AddAlignmentBlockOperationRelativeToFirstTokenOnBaseTokenLine(List<IndentBlockOperation> list, Roslyn.Utilities.ValueTuple<SyntaxToken, SyntaxToken> bracePair)
        {
            var option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;
            SetAlignmentBlockOperation(list, bracePair.Item1, bracePair.Item1.GetNextToken(includeZeroWidth: true), bracePair.Item2, option);
        }

        private void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
        {
            // increase indentation - embedded statement cases
            var ifStatement = node as IfStatementSyntax;
            if (ifStatement != null && ifStatement.Statement != null && !(ifStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, ifStatement.Statement);
                return;
            }

            var elseClause = node as ElseClauseSyntax;
            if (elseClause != null && elseClause.Statement != null)
            {
                if (!(elseClause.Statement is BlockSyntax || elseClause.Statement is IfStatementSyntax))
                {
                    AddEmbeddedStatementsIndentationOperation(list, elseClause.Statement);
                }

                return;
            }

            var whileStatement = node as WhileStatementSyntax;
            if (whileStatement != null && whileStatement.Statement != null && !(whileStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, whileStatement.Statement);
                return;
            }

            var forStatement = node as ForStatementSyntax;
            if (forStatement != null && forStatement.Statement != null && !(forStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, forStatement.Statement);
                return;
            }

            var foreachStatement = node as ForEachStatementSyntax;
            if (foreachStatement != null && foreachStatement.Statement != null && !(foreachStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, foreachStatement.Statement);
                return;
            }

            var usingStatement = node as UsingStatementSyntax;
            if (usingStatement != null && usingStatement.Statement != null && !(usingStatement.Statement is BlockSyntax || usingStatement.Statement is UsingStatementSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, usingStatement.Statement);
                return;
            }

            var doStatement = node as DoStatementSyntax;
            if (doStatement != null && doStatement.Statement != null && !(doStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, doStatement.Statement);
                return;
            }

            var lockStatement = node as LockStatementSyntax;
            if (lockStatement != null && lockStatement.Statement != null && !(lockStatement.Statement is BlockSyntax))
            {
                AddEmbeddedStatementsIndentationOperation(list, lockStatement.Statement);
                return;
            }
        }

        private void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, StatementSyntax statement)
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
}
