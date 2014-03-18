// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if MEF
    [ExportFormattingRule(Name, LanguageNames.CSharp)]
    [ExtensionOrder(After = StructuredTriviaFormattingRule.Name)]
#endif
    internal class IndentBlockFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp IndentBlock Formatting Rule";

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            AddBlockIndentationOperation(list, node, optionSet);

            AddLabelIndentationOperation(list, node, optionSet);

            AddSwitchIndentationOperation(list, node, optionSet);

            AddEmbeddedStatementsIndentationOperation(list, node);

            AddTypeParameterContraintClauseOperation(list, node);
        }

        private void AddTypeParameterContraintClauseOperation(List<IndentBlockOperation> list, SyntaxNode node)
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
                var startToken = labeledStatement.GetFirstToken().GetNextToken().GetNextToken();

                // make sure that label is not the only statement inside of a block
                if (labeledStatement.Parent.BlockContainsOnlyLabel() && startToken.CSharpKind() != SyntaxKind.None)
                {
                    // Nothing to do for NoIndent
                    if (labelPositioningOption == LabelPositionOptions.OneLess)
                    {
                        AddIndentBlockOperation(list, startToken, labeledStatement.GetLastToken());
                    }
                    else if (labelPositioningOption == LabelPositionOptions.LeftMost)
                    {
                        AddAbsoluteZeroIndentBlockOperation(list, labeledStatement.GetFirstToken(includeZeroWidth: true), labeledStatement.GetFirstToken().GetNextToken());
                        if (optionSet.GetOption(CSharpFormattingOptions.IndentBlock))
                        {
                            AddIndentBlockOperation(list, labeledStatement.Parent.GetFirstToken(), startToken, labeledStatement.GetLastToken());
                        }
                    }
                }
                else
                {
                    if (labelPositioningOption == LabelPositionOptions.OneLess)
                    {
                        AddUnindentBlockOperation(list, labeledStatement.GetFirstToken(includeZeroWidth: true), labeledStatement.GetFirstToken().GetNextToken());
                    }
                    else if (labelPositioningOption == LabelPositionOptions.LeftMost)
                    {
                        AddAbsoluteZeroIndentBlockOperation(list, labeledStatement.GetFirstToken(includeZeroWidth: true), labeledStatement.GetFirstToken().GetNextToken());
                    }
                }
            }
        }

        private void AddBlockIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            var bracePair = node.GetBracePair();

            // don't put block indentation operation if the block only contains label statement
            if (node.BlockContainsOnlyLabel() || !bracePair.IsValidBracePair())
            {
                return;
            }

            if (IsExpressionWithBraces(node))
            {
                var option = IndentBlockOption.RelativePosition;
                if (node.IsLambdaBodyBlock() || node is InitializerExpressionSyntax)
                {
                    option = IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine;
                }

                AddIndentBlockOperation(list, GetBaseTokenForRelativeIndentation(node, bracePair.Item1, optionSet),
                    bracePair.Item1.GetNextToken(includeZeroWidth: true), bracePair.Item2.GetPreviousToken(includeZeroWidth: true), option);

                return;
            }

            AddIndentBlockOperation(list, bracePair.Item1.GetNextToken(includeZeroWidth: true), bracePair.Item2.GetPreviousToken(includeZeroWidth: true));
        }

        private SyntaxToken GetBaseTokenForRelativeIndentation(SyntaxNode node, SyntaxToken openingBrace, OptionSet optionSet)
        {
            var useBrace = node.IsLambdaBodyBlock() ||
                           node is InitializerExpressionSyntax ||
                           (node is AnonymousObjectCreationExpressionSyntax && optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousType)) ||
                           (node is AnonymousMethodExpressionSyntax && optionSet.GetOption(CSharpFormattingOptions.OpenBracesInNewLineForAnonymousMethods));

            if (useBrace)
            {
                return openingBrace;
            }

            return GetFirstTokenFromContainingStatement(openingBrace);
        }

        private bool IsExpressionWithBraces(SyntaxNode node)
        {
            return node.IsLambdaBodyBlock() ||
                   node is AnonymousObjectCreationExpressionSyntax ||
                   node is AnonymousMethodExpressionSyntax ||
                   node is InitializerExpressionSyntax;
        }

        private SyntaxToken GetFirstTokenFromContainingStatement(SyntaxToken oldBase)
        {
            var currentParent = oldBase.Parent;
            while (currentParent != null)
            {
                if (currentParent is StatementSyntax && !(currentParent is BlockSyntax))
                {
                    break;
                }
                else
                {
                    currentParent = currentParent.Parent;
                }
            }

            if (currentParent != null)
            {
                return currentParent.GetFirstToken(includeZeroWidth: true);
            }

            return oldBase;
        }

        private void AddEmbeddedStatementsIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node)
        {
            // increase indentation - embeded statement cases
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
                // embeded statement is not done, consider following as part of embeded statement
                AddIndentBlockOperation(list, firstToken, lastToken);
            }
            else
            {
                // embeded statement is done
                AddIndentBlockOperation(list, firstToken, lastToken, TextSpan.FromBounds(firstToken.FullSpan.Start, lastToken.FullSpan.End));
            }
        }
    }
}