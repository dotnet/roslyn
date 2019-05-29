﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<SyntaxKind>
        {
            private ExpressionSyntax _assignmentTargetOpt;

            private Analyzer()
            {
            }

            public static SyntaxKind Analyze(SwitchStatementSyntax node, out bool shouldRemoveNextStatement)
            {
                return new Analyzer().AnalyzeSwitchStatement(node, out shouldRemoveNextStatement);
            }

            private static bool IsDefaultSwitchLabel(SwitchLabelSyntax node)
            {
                // default:
                if (node.IsKind(SyntaxKind.DefaultSwitchLabel))
                {
                    return true;
                }

                if (node.IsKind(SyntaxKind.CasePatternSwitchLabel, out CasePatternSwitchLabelSyntax @case))
                {
                    // case _:
                    if (@case.Pattern.IsKind(SyntaxKind.DiscardPattern))
                    {
                        return true;
                    }

                    // case var _:
                    // case var x:
                    if (@case.Pattern.IsKind(SyntaxKind.VarPattern, out VarPatternSyntax varPattern) &&
                        varPattern.Designation.IsKind(SyntaxKind.DiscardDesignation, SyntaxKind.SingleVariableDesignation))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override SyntaxKind VisitSwitchStatement(SwitchStatementSyntax node)
            {
                return AnalyzeSwitchStatement(node, out _);
            }

            private SyntaxKind AnalyzeSwitchStatement(SwitchStatementSyntax switchStatement, out bool shouldRemoveNextStatement)
            {
                shouldRemoveNextStatement = false;

                // Fail if the switch statement is empty or any of sections have more than one "case" label.
                // Once we have "or" patterns, we can relax this to accept multi-case sections.
                var sections = switchStatement.Sections;
                if (sections.Count == 0 || sections.Any(section => section.Labels.Count != 1))
                {
                    return default;
                }

                // If there's no "default" case, we look at the next statement.
                // For instance, it could be a "return" statement which we'll use
                // as the default case in the switch expression.
                var nextStatement = AnalyzeNextStatement(switchStatement, ref shouldRemoveNextStatement);

                // We do need to intersect the next statement analysis result to catch possible
                // arm kind mismatch, e.g. a "return" after a non-exhaustive assignment switch.
                return Aggregate(nextStatement, sections, (result, section) => Intersect(result, AnalyzeSwitchSection(section)));
            }

            private SyntaxKind AnalyzeNextStatement(SwitchStatementSyntax switchStatement, ref bool shouldRemoveNextStatement)
            {
                if (switchStatement.Sections.Any(section => IsDefaultSwitchLabel(section.Labels[0])))
                {
                    // Throw can be overridden by other section bodies, therefore it has no effect on the result.
                    return SyntaxKind.ThrowStatement;
                }

                shouldRemoveNextStatement = true;
                return AnalyzeNextStatement(switchStatement.GetNextStatement());
            }

            private static SyntaxKind Intersect(SyntaxKind left, SyntaxKind right)
            {
                if (left == SyntaxKind.ThrowStatement)
                {
                    return right;
                }

                if (right == SyntaxKind.ThrowStatement)
                {
                    return left;
                }

                if (left == right)
                {
                    return left;
                }

                return default;
            }

            private SyntaxKind AnalyzeNextStatement(StatementSyntax nextStatement)
            {
                // Only the following "throw" and "return" can be moved into the switch expression.
                return nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement)
                    ? Visit(nextStatement)
                    : default;
            }

            private SyntaxKind AnalyzeSwitchSection(SwitchSectionSyntax section)
            {
                switch (section.Statements.Count)
                {
                    case 1:
                    case 2 when section.Statements[1].IsKind(SyntaxKind.BreakStatement):
                        return Visit(section.Statements[0]);
                    default:
                        return default;
                }
            }

            private static SyntaxKind Aggregate<T>(SyntaxKind seed, SyntaxList<T> nodes, Func<SyntaxKind, T, SyntaxKind> func)
                where T : SyntaxNode
            {
                var result = seed;
                foreach (var node in nodes)
                {
                    result = func(result, node);
                    if (result == default)
                    {
                        // No point to continue if any node was not
                        // convertible to a switch arm's expression
                        break;
                    }
                }

                return result;
            }

            public override SyntaxKind VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (_assignmentTargetOpt != null)
                {
                    if (!SyntaxFactory.AreEquivalent(node.Left, _assignmentTargetOpt))
                    {
                        return default;
                    }
                }
                else
                {
                    _assignmentTargetOpt = node.Left;
                }

                return node.Kind();
            }

            public override SyntaxKind VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return Visit(node.Expression);
            }

            public override SyntaxKind VisitReturnStatement(ReturnStatementSyntax node)
            {
                // A "return" statement's expression will be placed in the switch arm expression.
                return node.Expression is null ? default : SyntaxKind.ReturnStatement;
            }

            public override SyntaxKind VisitThrowStatement(ThrowStatementSyntax node)
            {
                // A "throw" statement can be converted to a throw expression.
                // Gives Failure if Expression is null because a throw expression needs one.
                return node.Expression is null ? default : SyntaxKind.ThrowStatement;
            }

            public override SyntaxKind DefaultVisit(SyntaxNode node)
            {
                // In all other cases we return failure result.
                return default;
            }
        }
    }
}
