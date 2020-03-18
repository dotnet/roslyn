﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

            public static (SyntaxKind nodeToGenerate, VariableDeclaratorSyntax declaratorToRemoveOpt) Analyze(
                SwitchStatementSyntax node,
                SemanticModel semanticModel,
                out bool shouldRemoveNextStatement)
            {
                var analyzer = new Analyzer();
                var nodeToGenerate = analyzer.AnalyzeSwitchStatement(node, out shouldRemoveNextStatement);

                if (nodeToGenerate == SyntaxKind.SimpleAssignmentExpression &&
                    analyzer.TryGetVariableDeclaratorAndSymbol(semanticModel) is var (declarator, symbol))
                {
                    if (shouldRemoveNextStatement &&
                        semanticModel.AnalyzeDataFlow(node.GetNextStatement()).DataFlowsIn.Contains(symbol))
                    {
                        // Bail out if data flows into the next statement that we want to move
                        // For example:
                        //
                        //      string name = "";
                        //      switch (index)
                        //      {
                        //          case 0: name = "0"; break;
                        //          case 1: name = "1"; break;
                        //      }
                        //      throw new Exception(name);
                        //
                        return default;
                    }

                    var declaration = declarator.GetAncestor<StatementSyntax>();
                    if (declaration.Parent == node.Parent && declarator.Initializer is null)
                    {
                        var beforeSwitch = node.GetPreviousStatement() is StatementSyntax previousStatement
                            ? semanticModel.AnalyzeDataFlow(declaration, previousStatement)
                            : semanticModel.AnalyzeDataFlow(declaration);
                        if (!beforeSwitch.WrittenInside.Contains(symbol))
                        {
                            // Move declarator only if it has no initializer and it's not used before switch
                            return (nodeToGenerate, declaratorToRemoveOpt: declarator);
                        }
                    }
                }

                return (nodeToGenerate, declaratorToRemoveOpt: null);
            }

            private (VariableDeclaratorSyntax, ISymbol)? TryGetVariableDeclaratorAndSymbol(SemanticModel semanticModel)
            {
                if (!_assignmentTargetOpt.IsKind(SyntaxKind.IdentifierName))
                {
                    return null;
                }

                var symbol = semanticModel.GetSymbolInfo(_assignmentTargetOpt).Symbol;
                if (!(symbol is { Kind: SymbolKind.Local, DeclaringSyntaxReferences: { Length: 1 } syntaxRefs }))
                {
                    return null;
                }

                if (!(syntaxRefs[0].GetSyntax() is VariableDeclaratorSyntax declarator))
                {
                    return null;
                }

                return (declarator, symbol);
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
                // Fail if the switch statement is empty or any of sections have more than one "case" label.
                // Once we have "or" patterns, we can relax this to accept multi-case sections.
                var sections = switchStatement.Sections;
                if (sections.Count == 0 || !sections.All(s => s.Labels.Count == 1 || s.Labels.Any(x => x.IsKind(SyntaxKind.DefaultSwitchLabel))))
                {
                    shouldRemoveNextStatement = false;
                    return default;
                }

                // If there's no "default" case, we look at the next statement.
                // For instance, it could be a "return" statement which we'll use
                // as the default case in the switch expression.
                var nextStatement = AnalyzeNextStatement(switchStatement, out shouldRemoveNextStatement);

                // We do need to intersect the next statement analysis result to catch possible
                // arm kind mismatch, e.g. a "return" after a non-exhaustive assignment switch.
                return Aggregate(nextStatement, sections, (result, section) => Intersect(result, AnalyzeSwitchSection(section)));
            }

            private SyntaxKind AnalyzeNextStatement(SwitchStatementSyntax switchStatement, out bool shouldRemoveNextStatement)
            {
                if (switchStatement.Sections.Any(section => section.Labels.Count > 1 || IsDefaultSwitchLabel(section.Labels[0])))
                {
                    // Throw can be overridden by other section bodies, therefore it has no effect on the result.
                    shouldRemoveNextStatement = false;
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
                    case 2 when section.Statements[1].IsKind(SyntaxKind.BreakStatement) || section.Statements[0].IsKind(SyntaxKind.SwitchStatement):
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
                if (node.Right is RefExpressionSyntax)
                    return default;

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
                => Visit(node.Expression);

            public override SyntaxKind VisitReturnStatement(ReturnStatementSyntax node)
            {
                // A "return" statement's expression will be placed in the switch arm expression. We
                // also can't convert a switch statement with ref-returns to a switch-expression
                // (currently). Until the language supports ref-switch-expressions, we just disable
                // things.
                return node.Expression is null || node.Expression is RefExpressionSyntax
                    ? default
                    : SyntaxKind.ReturnStatement;
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
