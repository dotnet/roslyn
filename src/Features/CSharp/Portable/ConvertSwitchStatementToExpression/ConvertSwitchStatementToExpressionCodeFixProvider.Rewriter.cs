// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp.Extensions;
    using static SyntaxFactory;

    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider
    {
        private sealed class Rewriter : CSharpSyntaxVisitor<SyntaxNode>
        {
            private List<ExpressionSyntax> _assignmentTargetsOpt;

            private Rewriter()
            {
            }

            public static ExpressionSyntax Rewrite(SwitchStatementSyntax node, out List<ExpressionSyntax> assignmentTargetsOpt)
            {
                var rewriter = new Rewriter();
                var result = rewriter.VisitSwitchStatement(node);
                assignmentTargetsOpt = rewriter._assignmentTargetsOpt;
                return (ExpressionSyntax)result;
            }

            public override SyntaxNode VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
            {
                return node.Pattern;
            }

            public override SyntaxNode VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
            {
                return ConstantPattern(node.Value);
            }

            public override SyntaxNode VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
            {
                return DiscardPattern();
            }

            public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node)
            {
                Debug.Assert(node.Labels.Count == 1);
                var switchLabel = node.Labels[0];
                return SwitchExpressionArm(
                    pattern: (PatternSyntax)base.Visit(switchLabel),
                    whenClause: switchLabel.IsKind(SyntaxKind.CasePatternSwitchLabel, out CasePatternSwitchLabelSyntax patternSwitchLabel)
                        ? patternSwitchLabel.WhenClause
                        : null,
                    expression: VisitStatements(node.Statements));
            }

            public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                (_assignmentTargetsOpt ?? (_assignmentTargetsOpt = new List<ExpressionSyntax>())).Add(node.Left);
                return node.Right;
            }

            private ExpressionSyntax VisitStatements(SyntaxList<StatementSyntax> statements)
            {
                Debug.Assert(statements.Count > 0);
                Debug.Assert(!statements[0].IsKind(SyntaxKind.BreakStatement));

                var nodes = new List<ExpressionSyntax>();
                foreach (var statement in statements)
                {
                    if (statement.IsKind(SyntaxKind.BreakStatement))
                    {
                        break;
                    }

                    nodes.Add((ExpressionSyntax)Visit(statement));
                }

                Debug.Assert(nodes.Count > 0);
                return nodes.Count == 1 ? nodes[0] : TupleExpression(SeparatedList(nodes.Select(Argument)));
            }

            public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
            {
                return SwitchExpression(node.Expression, SeparatedList(node.Sections.Select(VisitSwitchSection)));
            }

            public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
            {
                return node.Expression;
            }

            public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node)
            {
                return ThrowExpression(node.Expression);
            }

            public override SyntaxNode DefaultVisit(SyntaxNode node)
            {
                throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }
    }
}
