// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertIfToSwitch
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertIfToSwitchCodeRefactoringProvider)), Shared]
    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider :
        AbstractConvertIfToSwitchCodeRefactoringProvider<
            StatementSyntax, IfStatementSyntax, ExpressionSyntax, CSharpConvertIfToSwitchCodeRefactoringProvider.Pattern>
    {
        protected override Pattern CreatePatternFromExpression(ExpressionSyntax operand, SemanticModel semanticModel, ref ExpressionSyntax switchExpression)
        {
            switch (operand.Kind())
            {
                case SyntaxKind.EqualsExpression:
                    {
                        // Look for the form "x == 5" or "5 == x" where "x" is equivalent to the switch expression.
                        // This will turn into a constant pattern e.g. "case 5:".
                        var node = (BinaryExpressionSyntax)operand;
                        if (!TryDetermineConstant(node.Right, node.Left, semanticModel,
                            out var constant, out var expression))
                        {
                            return null;
                        }

                        if (!AreEquivalent(expression, ref switchExpression))
                        {
                            return null;
                        }

                        return new Pattern.ByValue(constant);
                    }

                case SyntaxKind.IsExpression:
                    {
                        // Look for the form "x is T" where "x" is equivalent to the switch expression.
                        // This will turn into a discarded type pattern e.g. "case T _:".
                        var node = (BinaryExpressionSyntax)operand;
                        if (!AreEquivalent(node.Left, ref switchExpression))
                        {
                            return null;
                        }

                        return new Pattern.Discarded((TypeSyntax)node.Right);
                    }

                case SyntaxKind.IsPatternExpression:
                    {
                        // Look for the form "x is T t" where "x" is equivalent to the switch expression.
                        // This will turn into a type pattern e.g. "case T t:".
                        var node = (IsPatternExpressionSyntax)operand;
                        if (!AreEquivalent(node.Expression, ref switchExpression))
                        {
                            return null;
                        }

                        switch (node.Pattern)
                        {
                            case DeclarationPatternSyntax p
                                when p.Designation is SingleVariableDesignationSyntax designation:
                                return new Pattern.ByType(p.Type, designation.Identifier);

                            case ConstantPatternSyntax p:
                                return new Pattern.ByValue(p.Expression);

                            default:
                                return null;
                        }
                    }

                case SyntaxKind.LogicalAndExpression:
                    {
                        // Look for the form "expr && cond" where "expr" must be one of the above cases.
                        // This will turn into a case guard e.g. "case pat when cond:".
                        var node = (BinaryExpressionSyntax)operand;

                        // Since "&&" operator has a left associativity, first we need to find the leftmost
                        // expression in the if-condition, then we can replace it with an appropriate pattern.
                        var leftmost = GetLeftmostCondition(node);

                        // Make sure that it is one of the expressions that we can handle to prevent recursion.
                        switch (leftmost.Kind())
                        {
                            case SyntaxKind.IsPatternExpression:
                            case SyntaxKind.IsExpression:
                            case SyntaxKind.EqualsExpression:
                                break;

                            default:
                                return null;
                        }

                        var pattern = CreatePatternFromExpression(leftmost, semanticModel, ref switchExpression);
                        if (pattern == null)
                        {
                            return null;
                        }

                        // In case of chained "&&" operators, we need to justify the precedence of operands in which
                        // the "&&" operator is applied. For example, in "if (expr && cond1 && cond2)" we need to
                        // separate out the leftmost expression and reconstruct the rest of conditions into a new
                        // expression, so that they would be evaluated entirely after "expr" e.g. "expr && (cond1 && cond2)".
                        // Afterwards, we can directly use it in a case guard e.g. "case pat when cond1 && cond2:".
                        var condition = (leftmost.Parent.Parent as BinaryExpressionSyntax)
                            ?.WithLeft(((BinaryExpressionSyntax)leftmost.Parent).Right);

                        return new Pattern.Guarded(pattern, (condition ?? node.Right).WalkDownParentheses());
                    }

                default:
                    return null;
            }
        }

        protected override SyntaxToken GetIfKeyword(IfStatementSyntax ifStatement)
            => ifStatement.IfKeyword;

        protected override bool AreEquivalentCore(ExpressionSyntax expression, ExpressionSyntax switchExpression)
            => SyntaxFactory.AreEquivalent(expression, switchExpression);

        protected override bool CanConvertIfToSwitch(IfStatementSyntax ifStatement, SemanticModel semanticModel)
            => !semanticModel.AnalyzeControlFlow(ifStatement).ExitPoints.Any(n => n.IsKind(SyntaxKind.BreakStatement));

        protected override IEnumerable<ExpressionSyntax>
            GetLogicalOrExpressionOperands(ExpressionSyntax syntaxNode)
        {
            syntaxNode = syntaxNode.WalkDownParentheses();
            while (syntaxNode.IsKind(SyntaxKind.LogicalOrExpression))
            {
                var binaryExpression = (BinaryExpressionSyntax)syntaxNode;
                yield return binaryExpression.Right.WalkDownParentheses();
                syntaxNode = binaryExpression.Left.WalkDownParentheses();
            }

            yield return syntaxNode;
        }

        protected override IEnumerable<(StatementSyntax, ExpressionSyntax)>
            GetIfElseStatementChain(IfStatementSyntax ifStatement)
        {
            StatementSyntax elseBody;
            do
            {
                yield return (ifStatement.Statement, ifStatement.Condition);
                elseBody = ifStatement.Else?.Statement;
                ifStatement = elseBody as IfStatementSyntax;
            }
            while (ifStatement != null);

            yield return (elseBody, null);
        }

        protected override SyntaxNode CreateSwitchStatement(
            StatementSyntax switchDefaultBody,
            ExpressionSyntax switchExpression,
            SemanticModel semanticModel,
            List<(List<Pattern> patterns, StatementSyntax body)> sections)
        {
            var sectionList = SyntaxFactory.List(sections.Select(section => SwitchSection(section.body, semanticModel,
                labels: SyntaxFactory.List<SwitchLabelSyntax>(section.patterns.Select(pattern => pattern.CreateSwitchLabel())))));

            if (switchDefaultBody != null)
            {
                sectionList = sectionList.Add(SwitchSection(switchDefaultBody, semanticModel,
                    labels: SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel())));
            }

            return SyntaxFactory.SwitchStatement(switchExpression, sectionList);
        }

        private static ExpressionSyntax GetLeftmostCondition(ExpressionSyntax syntaxNode)
        {
            syntaxNode = syntaxNode.WalkDownParentheses();
            while (syntaxNode.IsKind(SyntaxKind.LogicalAndExpression))
            {
                syntaxNode = ((BinaryExpressionSyntax)syntaxNode).Left.WalkDownParentheses();
            }

            return syntaxNode;
        }

        private static SwitchSectionSyntax SwitchSection(StatementSyntax body, SemanticModel semanticModel, SyntaxList<SwitchLabelSyntax> labels)
            => SyntaxFactory.SwitchSection(labels, SyntaxFactory.List(GetSwitchSectionBody(body, semanticModel)));

        private static IEnumerable<SyntaxNode> GetSwitchSectionBody(StatementSyntax node, SemanticModel semanticModel)
        {
            bool RequiresBreak() => semanticModel.AnalyzeControlFlow(node).EndPointIsReachable;
            bool RequiresBlock() => !semanticModel.AnalyzeDataFlow(node).VariablesDeclared.IsDefaultOrEmpty;

            if (node is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    yield return SyntaxFactory.BreakStatement();
                }
                else if (RequiresBlock())
                {
                    if (RequiresBreak())
                    {
                        yield return block.AddStatements(SyntaxFactory.BreakStatement());
                    }
                    else
                    {
                        yield return block;
                    }
                }
                else
                {
                    foreach (var statement in block.Statements)
                    {
                        yield return statement;
                    }

                    if (RequiresBreak())
                    {
                        yield return SyntaxFactory.BreakStatement();
                    }
                }
            }
            else
            {
                yield return node;

                if (RequiresBreak())
                {
                    yield return SyntaxFactory.BreakStatement();
                }
            }
        }
    }
}
