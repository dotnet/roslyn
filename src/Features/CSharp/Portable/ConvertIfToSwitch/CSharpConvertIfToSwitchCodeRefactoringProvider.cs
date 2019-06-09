// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertIfToSwitchCodeRefactoringProvider)), Shared]
    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider : AbstractConvertIfToSwitchCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpConvertIfToSwitchCodeRefactoringProvider()
        {
        }

        protected override IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            => new CSharpAnalyzer(syntaxFacts, semanticModel);

        private sealed class CSharpAnalyzer : Analyzer<StatementSyntax, IfStatementSyntax, ExpressionSyntax, CasePatternSwitchLabelSyntax>
        {
            public CSharpAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
                : base(syntaxFacts, semanticModel)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_to_switch;

            protected override IPattern<CasePatternSwitchLabelSyntax> CreatePatternFromExpression(ExpressionSyntax operand)
            {
                switch (operand.Kind())
                {
                    case SyntaxKind.EqualsExpression:
                        {
                            // Look for the form "x == 5" or "5 == x" where "x" is equivalent to the switch expression.
                            // This will turn into a constant pattern e.g. "case 5:".
                            var node = (BinaryExpressionSyntax)operand;
                            if (!TryDetermineConstant(node.Right, node.Left,
                                out var constant, out var expression))
                            {
                                return null;
                            }

                            if (!SetInitialOrIsEquivalentToSwitchExpression(expression))
                            {
                                return null;
                            }

                            return new Pattern.ByValue(constant);
                        }

                    case SyntaxKind.IsExpression:
                        {
                            // Look for the form "x is T" where "x" is equivalent to the  switch expression.
                            // This will turn into a discarded type pattern e.g. "case T _:".
                            var node = (BinaryExpressionSyntax)operand;
                            if (!SetInitialOrIsEquivalentToSwitchExpression(node.Left))
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
                            if (!SetInitialOrIsEquivalentToSwitchExpression(node.Expression))
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

                            var pattern = CreatePatternFromExpression(leftmost);
                            if (pattern == null)
                            {
                                return null;
                            }

                            // In case of chained "&&" operators, we need to justify the precedence of operands in which
                            // the "&&" operator is applied. For example, in "if (expr && cond1 && cond2)" we need to
                            // separate out the leftmost expression and reconstruct the rest of conditions into a new
                            // expression, so that they would be evaluated entirely after "expr" e.g. "expr && (cond1 && cond2)".
                            // Afterwards, we can directly use it in a case guard e.g. "case pat when cond1 && cond2:".
                            var parentBinary = (BinaryExpressionSyntax)leftmost.WalkUpParentheses().Parent;
                            var grandparentBinary = parentBinary.WalkUpParentheses().Parent as BinaryExpressionSyntax;

                            var condition = grandparentBinary?.WithLeft(parentBinary.Right);

                            return new Pattern.Guarded(pattern, (condition ?? node.Right).WalkDownParentheses());
                        }

                    default:
                        return null;
                }
            }

            // We do not offer a fix if the if-statement contains a break-statement, e.g.
            //
            //      while (...)
            //      {
            //          if (...) {
            //              break;
            //          }
            //      }
            // 
            // When the 'break' moves into the switch, it will have different flow control impact.
            protected override bool CanConvertIfToSwitch(IfStatementSyntax ifStatement)
                => !_semanticModel.AnalyzeControlFlow(ifStatement).ExitPoints.Any(n => n.IsKind(SyntaxKind.BreakStatement));

            protected override IEnumerable<ExpressionSyntax> GetLogicalOrOperands(
                ExpressionSyntax syntaxNode)
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

            protected override IEnumerable<(ExpressionSyntax, StatementSyntax)> GetIfElseStatementChain(IfStatementSyntax currentStatement)
            {
                StatementSyntax elseClauseStatement;
                do
                {
                    yield return (currentStatement.Condition, currentStatement.Statement);
                    elseClauseStatement = currentStatement.Else?.Statement;
                    currentStatement = elseClauseStatement as IfStatementSyntax;
                }
                while (currentStatement != null);

                if (elseClauseStatement != null)
                {
                    yield return (null, elseClauseStatement);
                }
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

            protected override IEnumerable<SyntaxNode> GetSwitchSectionBody(StatementSyntax node)
            {
                var requiresBreak = _semanticModel.AnalyzeControlFlow(node).EndPointIsReachable;
                var requiresBlock = !_semanticModel.AnalyzeDataFlow(node).VariablesDeclared.IsDefaultOrEmpty;

                if (node is BlockSyntax block)
                {
                    if (block.Statements.Count == 0)
                    {
                        yield return SyntaxFactory.BreakStatement();
                    }
                    else if (requiresBlock)
                    {
                        if (requiresBreak)
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

                        if (requiresBreak)
                        {
                            yield return SyntaxFactory.BreakStatement();
                        }
                    }
                }
                else
                {
                    yield return node;

                    if (requiresBreak)
                    {
                        yield return SyntaxFactory.BreakStatement();
                    }
                }
            }

            protected override bool EndPointIsReachable(IfStatementSyntax ifStatement)
                => _semanticModel.AnalyzeControlFlow(ifStatement.Statement).EndPointIsReachable;

            protected override ExpressionSyntax UnwrapCast(ExpressionSyntax expression)
            {
                switch (expression)
                {
                    case BinaryExpressionSyntax binaryExpression
                        when expression.IsKind(SyntaxKind.AsExpression):
                        return binaryExpression.Left;

                    case CastExpressionSyntax castExpression:
                        return castExpression.Expression;

                    default:
                        return expression;
                }
            }

            protected override SyntaxNode CreateSwitchStatement(
                IfStatementSyntax ifStatement, ExpressionSyntax expression, List<SyntaxNode> sectionList)
            {
                var block = ifStatement.Statement as BlockSyntax;

                return SyntaxFactory.SwitchStatement(
                    SyntaxFactory.Token(SyntaxKind.SwitchKeyword).WithTriviaFrom(ifStatement.IfKeyword),
                    ifStatement.OpenParenToken,
                    expression,
                    ifStatement.CloseParenToken.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker),
                    block?.OpenBraceToken ?? SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                    new SyntaxList<SwitchSectionSyntax>(sectionList.OfType<SwitchSectionSyntax>()),
                    block?.CloseBraceToken ?? SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
            }
        }
    }
}
