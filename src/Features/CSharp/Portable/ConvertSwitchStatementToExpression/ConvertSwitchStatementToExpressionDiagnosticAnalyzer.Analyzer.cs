// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalysisResult>
        {
            private readonly ArrayBuilder<SyntaxNode> _originalTargets;
            private readonly ArrayBuilder<SyntaxNode> _currentTargets;

            private bool _firstSwitchSection = true;

            private Analyzer()
            {
                _originalTargets = ArrayBuilder<SyntaxNode>.GetInstance();
                _currentTargets = ArrayBuilder<SyntaxNode>.GetInstance();
            }

            private void Free()
            {
                _originalTargets.Free();
                _currentTargets.Free();
            }

            public static AnalysisResult Analyze(SwitchStatementSyntax node, out bool shouldRemoveNextStatement)
            {
                var analyzer = new Analyzer();
                var result = analyzer.AnalyzeSwitchStatement(node, out shouldRemoveNextStatement);
                analyzer.Free();
                return result;
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

            public override AnalysisResult VisitSwitchStatement(SwitchStatementSyntax node)
            {
                return AnalyzeSwitchStatement(node, out _);
            }

            private AnalysisResult AnalyzeSwitchStatement(SwitchStatementSyntax switchStatement, out bool shouldRemoveNextStatement)
            {
                shouldRemoveNextStatement = false;

                // Fail if the switch statement is empty or any of sections have more than one "case" label.
                // Once we have "or" patterns, we can relax this to accept multi-case sections.
                var sections = switchStatement.Sections;
                if (sections.Count == 0 || sections.Any(section => section.Labels.Count != 1))
                {
                    return AnalysisResult.Failure;
                }

                // If there's no "default" case, we look at the next statement.
                // For instance, it could be a "return" statement which we'll use
                // as the default case in the switch expression.
                var nextStatementAnalysis = sections.Any(section => IsDefaultSwitchLabel(section.Labels[0]))
                    ? AnalysisResult.Neutral
                    : AnalyzeNextStatement(switchStatement.GetNextStatement());
                if (nextStatementAnalysis.IsFailure)
                {
                    return AnalysisResult.Failure;
                }

                // Using "Success" which considers both "return" and "throw" but excludes neutral result.
                shouldRemoveNextStatement = nextStatementAnalysis.Success;

                // We do need to intersect the next statement analysis result to catch possible
                // arm kind mismatch, e.g. a "return" after a non-exhaustive assignment switch.
                var result = nextStatementAnalysis;
                foreach (var section in sections)
                {
                    result = result.Intersect(AnalyzeSwitchSection(section));
                    if (result.IsFailure)
                    {
                        break;
                    }

                    if (_firstSwitchSection)
                    {
                        _firstSwitchSection = false;
                        continue;
                    }

                    // Assignments only match if they have the same set of targets
                    if (!_currentTargets.SequenceEqual(_originalTargets,
                            (current, source) => SyntaxFactory.AreEquivalent(current, source)))
                    {
                        return AnalysisResult.Failure;
                    }

                    _currentTargets.Clear();
                }

                return result;
            }

            private AnalysisResult AnalyzeNextStatement(StatementSyntax nextStatement)
            {
                // Only the following "throw" and "return" can be moved into the switch expression.
                return nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement)
                    ? Visit(nextStatement)
                    : AnalysisResult.Failure;
            }

            private AnalysisResult AnalyzeSwitchSection(SwitchSectionSyntax section)
            {
                // This is a switch section body. Here we "combine" the result, since there could be
                // compatible statements like the ending `break;` with some other assignments.
                var result = AnalysisResult.Neutral;
                foreach (var statement in section.Statements)
                {
                    result = result.Union(Visit(statement));
                    if (result.IsFailure)
                    {
                        // No point to continue if any node was not
                        // convertible to a switch arm's expression
                        break;
                    }
                }

                return result;
            }

            public override AnalysisResult VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (DependsOnPreviousAssignments(node))
                {
                    return AnalysisResult.Failure;
                }

                var targets = _firstSwitchSection ? _originalTargets : _currentTargets;
                targets.Add(node.Left);
                return AnalysisResult.Assignment(node.Kind());
            }

            private bool DependsOnPreviousAssignments(AssignmentExpressionSyntax assignment)
            {
                return _originalTargets.Any(
                    (target, right) => right.DescendantNodesAndSelf().Any(n => SyntaxFactory.AreEquivalent(target, n)),
                    assignment.Right);
            }

            public override AnalysisResult VisitBreakStatement(BreakStatementSyntax node)
            {
                // Only `break;` is allowed which could appear after some assignments.
                return AnalysisResult.Break;
            }

            public override AnalysisResult VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return Visit(node.Expression);
            }

            public override AnalysisResult VisitReturnStatement(ReturnStatementSyntax node)
            {
                // A "return" statement's expression will be placed in the switch arm expression.
                return node.Expression is null ? AnalysisResult.Failure : AnalysisResult.Return;
            }

            public override AnalysisResult VisitThrowStatement(ThrowStatementSyntax node)
            {
                // A "throw" statement can be converted to a throw expression.
                // Gives Failure if Expression is null because a throw expression needs one.
                return node.Expression is null ? AnalysisResult.Failure : AnalysisResult.Throw;
            }

            public override AnalysisResult DefaultVisit(SyntaxNode node)
            {
                // In all other cases we return failure result.
                return AnalysisResult.Failure;
            }
        }
    }
}
