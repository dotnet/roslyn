// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalysisResult>
        {
            private Analyzer()
            {
            }

            public static bool CanConvertToSwitchExpression(SyntaxNode node)
            {
                return new Analyzer().Visit(node).Success;
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
                // Fail if the switch statement is empty or any of sections have more than one "case" label.
                // Once we have "or" patterns, we can relax this to accept multi-case sections.
                var sections = node.Sections;
                if (sections.Count == 0 || sections.Any(section => section.Labels.Count != 1))
                {
                    return AnalysisResult.Failure;
                }

                return AnalysisResult.Match(
                    // If there's no "default" case, we look at the next statement.
                    // For instance, it could be a "return" statement which we'll use 
                    // as the default case in the switch expression.
                    sections.Any(section => IsDefaultSwitchLabel(section.Labels[0]))
                        ? AnalysisResult.Neutral
                        : AnalyzeNextStatement(node.GetNextStatement()),
                    // Iterate over all sections and match section bodies to
                    // see if they are all convertible to a switch arm's expression.
                    Aggregate(sections, (result, section) => AnalysisResult.Match(result, AnalyzeSwitchSection(section))));
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
                return Aggregate(section.Statements, (result, node) => AnalysisResult.Combine(result, Visit(node)));
            }

            private static AnalysisResult Aggregate<T>(SyntaxList<T> nodes, Func<AnalysisResult, T, AnalysisResult> func)
                where T : SyntaxNode
            {
                var result = AnalysisResult.Neutral;
                foreach (var node in nodes)
                {
                    result = func(result, node);
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
                return node.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    ? AnalysisResult.Assignment(node.Left)
                    : AnalysisResult.Failure;
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
                // Gives Neutral result because it's valid for any kind of switch expression.
                // Gives Failure if Expression is null because a throw expression needs one.
                return node.Expression is null ? AnalysisResult.Failure : AnalysisResult.Neutral;
            }

            public override AnalysisResult DefaultVisit(SyntaxNode node)
            {
                // In all other cases we return failure result.
                return AnalysisResult.Failure;
            }
        }
    }
}
