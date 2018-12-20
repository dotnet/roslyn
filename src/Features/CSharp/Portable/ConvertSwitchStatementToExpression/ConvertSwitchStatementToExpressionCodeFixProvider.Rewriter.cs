// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using static SyntaxFactory;

    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider
    {
        private sealed class Rewriter : CSharpSyntaxVisitor<ExpressionSyntax>
        {
            private readonly ArrayBuilder<ExpressionSyntax> _assignmentTargets;
            private readonly bool _isAllThrowStatements;

            private Rewriter(bool isAllThrowStatements)
            {
                _assignmentTargets = ArrayBuilder<ExpressionSyntax>.GetInstance();
                _isAllThrowStatements = isAllThrowStatements;
            }

            private void Free()
            {
                _assignmentTargets.Free();
            }

            public static StatementSyntax Rewrite(
                SwitchStatementSyntax switchStatement, SemanticModel semanticModel, SyntaxEditor editor,
                SyntaxKind nodeToGenerate, bool shouldMoveNextStatementToSwitchExpression)
            {
                var rewriter = new Rewriter(isAllThrowStatements: nodeToGenerate == SyntaxKind.ThrowStatement);

                // Rewrite the switch statement as a switch expression.
                var switchExpression = rewriter.RewriteSwitchStatement(switchStatement,
                    allowMoveNextStatementToSwitchExpression: shouldMoveNextStatementToSwitchExpression);

                // Only on simple assignments we attempt to remove variable declarators.
                var isSimpleAssignment = nodeToGenerate == SyntaxKind.SimpleAssignmentExpression;
                var generateDeclaration = isSimpleAssignment && rewriter.TryRemoveVariableDeclarators(switchStatement, semanticModel, editor);

                // Generate the final statement to wrap the switch expression, e.g. a "return" or an assignment.
                var finalStatement = rewriter.GetFinalStatement(switchExpression, nodeToGenerate, generateDeclaration);
                rewriter.Free();
                return finalStatement;
            }

            private bool TryRemoveVariableDeclarators(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, SyntaxEditor editor)
            {
                Debug.Assert(_assignmentTargets.Count > 0);

                // Try to remove variable declarator only if these are simple identifiers.
                if (!_assignmentTargets.All(target => target.IsKind(SyntaxKind.IdentifierName)))
                {
                    return false;
                }

                var symbols = _assignmentTargets.SelectAsArray((target, model) => model.GetSymbolInfo(target).Symbol, semanticModel);
                foreach (var symbol in symbols)
                {
                    if (symbol == null)
                    {
                        return false;
                    }

                    if (symbol.Kind != SymbolKind.Local)
                    {
                        return false;
                    }

                    var syntaxReferences = symbol.DeclaringSyntaxReferences;
                    if (syntaxReferences.Length != 1)
                    {
                        return false;
                    }

                    if (!(syntaxReferences[0].GetSyntax() is VariableDeclaratorSyntax declarator))
                    {
                        return false;
                    }

                    if (declarator.Initializer != null)
                    {
                        return false;
                    }

                    var symbolName = symbol.Name;
                    var declaratorSpanStart = declarator.SpanStart;
                    var switchStatementSpanStart = switchStatement.SpanStart;

                    // Check for uses before the switch expression.
                    foreach (var descendentNode in declarator.GetAncestor<BlockSyntax>().DescendantNodes())
                    {
                        var nodeSpanStart = descendentNode.SpanStart;
                        if (nodeSpanStart <= declaratorSpanStart)
                        {
                            // We haven't yet reached the declarator node.
                            continue;
                        }

                        if (nodeSpanStart >= switchStatementSpanStart)
                        {
                            // We've reached the switch statement.
                            break;
                        }

                        if (descendentNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifierName) &&
                            identifierName.Identifier.ValueText == symbolName &&
                            symbol.Equals(semanticModel.GetSymbolInfo(identifierName).Symbol))
                        {
                            // The variable is being used outside the switch statement.
                            return false;
                        }
                    }
                }

                foreach (var symbol in symbols)
                {
                    // Safe to remove all declarator nodes.
                    editor.RemoveNode(symbol.DeclaringSyntaxReferences[0].GetSyntax());
                }

                return true;
            }

            private StatementSyntax GetFinalStatement(ExpressionSyntax switchExpression, SyntaxKind nodeToGenerate, bool generateDeclaration)
            {
                switch (nodeToGenerate)
                {
                    case SyntaxKind.ReturnStatement:
                        return ReturnStatement(switchExpression);
                    case SyntaxKind.ThrowStatement:
                        return ThrowStatement(switchExpression);
                }

                Debug.Assert(SyntaxFacts.IsAssignmentExpression(nodeToGenerate));
                Debug.Assert(_assignmentTargets.Count > 0);

                return generateDeclaration
                    ? GenerateVariableDeclaration(switchExpression)
                    : GenerateAssignment(switchExpression, nodeToGenerate);
            }

            private ExpressionStatementSyntax GenerateAssignment(ExpressionSyntax switchExpression, SyntaxKind assignmentKind)
            {
                var assignmentLeft = _assignmentTargets.Count == 1
                    ? _assignmentTargets[0]
                    : TupleExpression(SeparatedList(_assignmentTargets.Select(Argument)));

                return ExpressionStatement(
                    AssignmentExpression(assignmentKind,
                        left: assignmentLeft,
                        right: switchExpression));
            }

            private ExpressionStatementSyntax GenerateVariableDeclaration(ExpressionSyntax switchExpression)
            {
                var designations = _assignmentTargets
                    .Select(id => (VariableDesignationSyntax)SingleVariableDesignation(((IdentifierNameSyntax)id).Identifier))
                    .ToArray();

                var designation = designations.Length == 1
                    ? designations[0]
                    : ParenthesizedVariableDesignation(SeparatedList(designations));

                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        left: DeclarationExpression(IdentifierName("var"), designation),
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
                // to avoid adding duplicated nodes.
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
                return RewriteSwitchStatement(node);
            }

            private ExpressionSyntax RewriteSwitchStatement(SwitchStatementSyntax node, bool allowMoveNextStatementToSwitchExpression = true)
            {
                var switchArms = node.Sections
                    // The default label must come last in the switch expression.
                    .OrderBy(section => section.Labels[0].IsKind(SyntaxKind.DefaultSwitchLabel))
                    .SelectAsArray(GetSwitchExpressionArm);

                // This is possibly false only on the top-level switch statement. 
                // On nested nodes, if there's a subsequent statement, it is most definitely a
                // "return" or "throw" which is already validated in the analysis phase.
                if (allowMoveNextStatementToSwitchExpression)
                {
                    var nextStatement = node.GetNextStatement();
                    if (nextStatement != null)
                    {
                        Debug.Assert(nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement));
                        switchArms = switchArms.Add(SwitchExpressionArm(DiscardPattern(), Visit(nextStatement)));
                    }
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
                // If this is an all-throw switch statement, we return the expression rather than
                // creating a throw expression so we can wrap the switch expression inside a throw expression.
                return _isAllThrowStatements ? node.Expression : ThrowExpression(node.Expression);
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
