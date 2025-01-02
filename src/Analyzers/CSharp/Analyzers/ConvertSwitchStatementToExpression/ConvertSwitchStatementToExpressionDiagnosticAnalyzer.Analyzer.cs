// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;

using static ConvertSwitchStatementToExpressionHelpers;

internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
{
    private sealed class Analyzer : CSharpSyntaxVisitor<SyntaxKind>
    {
        private readonly bool _supportsOrPatterns;

        private ExpressionSyntax? _assignmentTargetOpt;

        private Analyzer(bool supportsOrPatterns)
        {
            _supportsOrPatterns = supportsOrPatterns;
        }

        public static (SyntaxKind nodeToGenerate, VariableDeclaratorSyntax? declaratorToRemoveOpt) Analyze(
            SwitchStatementSyntax node,
            SemanticModel semanticModel,
            out bool shouldRemoveNextStatement)
        {
            var analyzer = new Analyzer(supportsOrPatterns: semanticModel.SyntaxTree.Options.LanguageVersion() >= LanguageVersion.CSharp9);
            var nodeToGenerate = analyzer.AnalyzeSwitchStatement(node, out shouldRemoveNextStatement);

            if (nodeToGenerate == SyntaxKind.SimpleAssignmentExpression &&
                analyzer.TryGetVariableDeclaratorAndSymbol(semanticModel) is var (declarator, symbol))
            {
                if (shouldRemoveNextStatement && node.GetNextStatement() is StatementSyntax nextStatement)
                {
                    var dataFlow = semanticModel.AnalyzeDataFlow(nextStatement);
                    Contract.ThrowIfNull(dataFlow);
                    if (dataFlow.DataFlowsIn.Contains(symbol))
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
                }

                var declaration = declarator.GetAncestor<StatementSyntax>();
                Contract.ThrowIfNull(declaration);
                if (declaration.Parent == node.Parent && declarator.Initializer is null)
                {
                    var beforeSwitch = node.GetPreviousStatement() is StatementSyntax previousStatement
                        ? semanticModel.AnalyzeDataFlow(declaration, previousStatement)
                        : semanticModel.AnalyzeDataFlow(declaration);
                    Contract.ThrowIfNull(beforeSwitch);
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
            if (symbol is not
                { Kind: SymbolKind.Local, DeclaringSyntaxReferences: { Length: 1 } syntaxRefs })
            {
                return null;
            }

            if (syntaxRefs[0].GetSyntax() is not VariableDeclaratorSyntax declarator)
            {
                return null;
            }

            return (declarator, symbol);
        }

        public override SyntaxKind VisitSwitchStatement(SwitchStatementSyntax node)
            => AnalyzeSwitchStatement(node, out _);

        private SyntaxKind AnalyzeSwitchStatement(SwitchStatementSyntax switchStatement, out bool shouldRemoveNextStatement)
        {
            // Fail if the switch statement is empty.
            var sections = switchStatement.Sections;
            if (sections.Count == 0)
            {
                shouldRemoveNextStatement = false;
                return default;
            }

            if (!sections.All(s => CanConvertLabelsToArms(s.Labels)))
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

        private bool CanConvertLabelsToArms(SyntaxList<SwitchLabelSyntax> labels)
        {
            Debug.Assert(labels.Count >= 1);
            if (labels.Count == 1)
            {
                // Single label can always be converted to a single arm.
                return true;
            }

            if (labels.Any(label => IsDefaultSwitchLabel(label)))
            {
                // if any of the  labels are a default/_/var (catch-all) then we can convert this set of labels into
                // a single `_` arm.
                return true;
            }

            // We have multiple labels and none of them are a 'catch-all'.  

            if (!_supportsOrPatterns)
            {
                // We don't support 'or' patterns, so no way to convert this to arms.
                return false;
            }

            // If any of the cases have when-clauses, like so:
            //
            //  case ... when Goo():
            //  case ... when Bar():
            //
            // Then we can't convert into a single arm.
            foreach (var label in labels)
            {
                if (label is CasePatternSwitchLabelSyntax casePattern &&
                    casePattern.WhenClause != null)
                {
                    return false;
                }
            }

            // We have multiple labels that can be combined together using an 'or' pattern.
            return true;
        }

        private SyntaxKind AnalyzeNextStatement(SwitchStatementSyntax switchStatement, out bool shouldRemoveNextStatement)
        {
            // Check if we have a catch-all label anywhere.  If so we don't need to pull in the next statements.
            if (switchStatement.Sections.Any(section => section.Labels.Any(label => IsDefaultSwitchLabel(label))))
            {
                // Throw can be overridden by other section bodies, therefore it has no effect on the result.
                shouldRemoveNextStatement = false;
                return SyntaxKind.ThrowStatement;
            }

            // Didn't have a default case, see if we can pull in the statement following the switch to become our default.
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

        private SyntaxKind AnalyzeNextStatement(StatementSyntax? nextStatement)
        {
            // Only the following "throw" and "return" can be moved into the switch expression.
            return nextStatement is (kind: SyntaxKind.ThrowStatement or SyntaxKind.ReturnStatement)
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
            return node.Expression is null or RefExpressionSyntax
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
