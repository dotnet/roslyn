// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using static SyntaxFactory;

    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider
    {
        private sealed class Rewriter : CSharpSyntaxVisitor<ExpressionSyntax>
        {
            private readonly ArrayBuilder<ExpressionSyntax> _assignmentTargets;

            private Rewriter()
            {
                _assignmentTargets = ArrayBuilder<ExpressionSyntax>.GetInstance();
            }

            private void Free()
            {
                _assignmentTargets.Free();
            }

            public static StatementSyntax Rewrite(SyntaxNode node)
            {
                var rewriter = new Rewriter();
                var finalStatement = rewriter.GetFinalStatement(rewriter.Visit(node));
                rewriter.Free();
                return finalStatement;
            }

            private StatementSyntax GetFinalStatement(ExpressionSyntax switchExpression)
            {
                if (_assignmentTargets.Count == 0)
                {
                    return ReturnStatement(switchExpression);
                }

                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        left: _assignmentTargets.Count == 1
                            ? _assignmentTargets[0]
                            : TupleExpression(SeparatedList(_assignmentTargets.Select(Argument))),
                        right: switchExpression));
            }

            private SwitchExpressionArmSyntax GetSwitchExpressionArm(SwitchSectionSyntax node)
            {
                Debug.Assert(node.Labels.Count == 1);
                return SwitchExpressionArm(
                    pattern: GetPattern(node.Labels[0], out var whenClauseOpt),
                    whenClause: whenClauseOpt,
                    expression: RewriteStatements(node.Statements));
            }

            private static PatternSyntax GetPattern(SwitchLabelSyntax switchLabel, out WhenClauseSyntax whenClauseOpt)
            {
                switch (switchLabel.Kind())
                {
                    case SyntaxKind.CasePatternSwitchLabel:
                        var node = (CasePatternSwitchLabelSyntax)switchLabel;
                        whenClauseOpt = node.WhenClause;
                        return node.Pattern;

                    case SyntaxKind.CaseSwitchLabel:
                        whenClauseOpt = null;
                        return ConstantPattern(((CaseSwitchLabelSyntax)switchLabel).Value);

                    case SyntaxKind.DefaultSwitchLabel:
                        whenClauseOpt = null;
                        return DiscardPattern();

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override ExpressionSyntax VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                // Make sure assignments are under the same switch section
                // to avoid adding dupliacted nodes.
                if (_assignmentTargets.Count == 0 || 
                    _assignmentTargets[0].Parent.Parent.Parent == node.Parent.Parent)
                {
                    _assignmentTargets.Add(node.Left);
                }

                return node.Right;
            }

            private ExpressionSyntax RewriteStatements(SyntaxList<StatementSyntax> statements)
            {
                Debug.Assert(statements.Count > 0);
                Debug.Assert(!statements[0].IsKind(SyntaxKind.BreakStatement));

                var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
                foreach (var statement in statements)
                {
                    if (statement.IsKind(SyntaxKind.BreakStatement))
                    {
                        break;
                    }

                    var expression = Visit(statement);
                    expressions.Add(expression);

                    // In case we had a nested switch expression,
                    // the possible next node is already morphed
                    // into the default case. See below.
                    if (expression.IsKind(SyntaxKind.SwitchExpression))
                    {
                        break;
                    }
                }

                Debug.Assert(expressions.Count > 0);
                var result = expressions.Count == 1
                    ? expressions[0]
                    : TupleExpression(SeparatedList(expressions.Select(Argument)));
                expressions.Free();
                return result;
            }

            public override ExpressionSyntax VisitSwitchStatement(SwitchStatementSyntax node)
            {
                var switchArms = node.Sections
                    // The default label must come last in the switch expression.
                    .OrderBy(section => section.Labels[0].IsKind(SyntaxKind.DefaultSwitchLabel))
                    .SelectAsArray(GetSwitchExpressionArm);

                var nextStatement = node.GetNextStatement();
                if (nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement))
                {
                    // If there's another statement, it should be a "return" or "throw"
                    // statement which is already validated in the analysis phase.
                    switchArms = switchArms.Add(SwitchExpressionArm(DiscardPattern(), Visit(nextStatement)));
                }

                return SwitchExpression(node.Expression, SeparatedList(switchArms));
            }

            public override ExpressionSyntax VisitReturnStatement(ReturnStatementSyntax node)
            {
                Debug.Assert(node.Expression != null);
                return node.Expression;
            }

            public override ExpressionSyntax VisitThrowStatement(ThrowStatementSyntax node)
            {
                Debug.Assert(node.Expression != null);
                return ThrowExpression(node.Expression);
            }

            public override ExpressionSyntax VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return Visit(node.Expression);
            }

            public override ExpressionSyntax DefaultVisit(SyntaxNode node)
            {
                throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }
    }
}
