// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using static SyntaxFactory;

    internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider
    {
        private sealed class Rewriter : CSharpSyntaxVisitor<ExpressionSyntax>
        {
            private ExpressionSyntax _assignmentTargetOpt;
            private readonly bool _isAllThrowStatements;

            private Rewriter(bool isAllThrowStatements)
            {
                _isAllThrowStatements = isAllThrowStatements;
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
                return rewriter.GetFinalStatement(switchExpression,
                    switchStatement.SwitchKeyword.LeadingTrivia, nodeToGenerate, generateDeclaration);
            }

            private bool TryRemoveVariableDeclarators(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, SyntaxEditor editor)
            {
                Debug.Assert(_assignmentTargetOpt != null);

                // Try to remove variable declarator only if it's a simple identifier.
                if (!_assignmentTargetOpt.IsKind(SyntaxKind.IdentifierName))
                {
                    return false;
                }

                var symbol = semanticModel.GetSymbolInfo(_assignmentTargetOpt).Symbol;
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

                // Safe to remove declarator node.
                editor.RemoveNode(symbol.DeclaringSyntaxReferences[0].GetSyntax());
                return true;
            }

            private StatementSyntax GetFinalStatement(
                ExpressionSyntax switchExpression,
                SyntaxTriviaList leadingTrivia,
                SyntaxKind nodeToGenerate,
                bool generateDeclaration)
            {
                switch (nodeToGenerate)
                {
                    case SyntaxKind.ReturnStatement:
                        return ReturnStatement(
                            Token(leadingTrivia, SyntaxKind.ReturnKeyword, trailing: default),
                            switchExpression,
                            Token(SyntaxKind.SemicolonToken));
                    case SyntaxKind.ThrowStatement:
                        return ThrowStatement(
                            Token(leadingTrivia, SyntaxKind.ThrowKeyword, trailing: default),
                            switchExpression,
                            Token(SyntaxKind.SemicolonToken));
                }

                Debug.Assert(SyntaxFacts.IsAssignmentExpression(nodeToGenerate));
                Debug.Assert(_assignmentTargetOpt != null);

                return generateDeclaration
                    ? GenerateVariableDeclaration(switchExpression, leadingTrivia)
                    : GenerateAssignment(switchExpression, nodeToGenerate, leadingTrivia);
            }

            private ExpressionStatementSyntax GenerateAssignment(ExpressionSyntax switchExpression, SyntaxKind assignmentKind, SyntaxTriviaList leadingTrivia)
            {
                Debug.Assert(_assignmentTargetOpt != null);

                return ExpressionStatement(
                    AssignmentExpression(assignmentKind,
                        left: _assignmentTargetOpt,
                        right: switchExpression))
                    .WithLeadingTrivia(leadingTrivia);
            }

            private StatementSyntax GenerateVariableDeclaration(ExpressionSyntax switchExpression, SyntaxTriviaList leadingTrivia)
            {
                Debug.Assert(_assignmentTargetOpt is IdentifierNameSyntax);

                return LocalDeclarationStatement(
                        VariableDeclaration(
                            type: IdentifierName(Identifier(leadingTrivia, "var", trailing: default)),
                            variables: SingletonSeparatedList(
                                        VariableDeclarator(
                                            identifier: ((IdentifierNameSyntax)_assignmentTargetOpt).Identifier,
                                            argumentList: null,
                                            initializer: EqualsValueClause(switchExpression)))));
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
                if (_assignmentTargetOpt == null)
                {
                    _assignmentTargetOpt = node.Left;
                }

                return node.Right;
            }

            private ExpressionSyntax RewriteStatements(SyntaxList<StatementSyntax> statements)
            {
                Debug.Assert(statements.Count == 1 || statements.Count == 2);
                Debug.Assert(!statements[0].IsKind(SyntaxKind.BreakStatement));
                return Visit(statements[0]);
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
                    .Select(s =>
                        (leadingTrivia: s.Labels[0].GetFirstToken().LeadingTrivia,
                         trailingTrivia: s.Statements[0].GetLastToken().TrailingTrivia,
                         armExpression: GetSwitchExpressionArm(s)))
                    .ToList();

                // This is possibly false only on the top-level switch statement. 
                // On nested nodes, if there's a subsequent statement, it is most definitely a
                // "return" or "throw" which is already validated in the analysis phase.
                if (allowMoveNextStatementToSwitchExpression)
                {
                    var nextStatement = node.GetNextStatement();
                    if (nextStatement != null)
                    {
                        Debug.Assert(nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement));
                        switchArms.Add(
                            (nextStatement.GetFirstToken().LeadingTrivia,
                             nextStatement.GetLastToken().TrailingTrivia,
                             SwitchExpressionArm(DiscardPattern(), Visit(nextStatement))));
                    }
                }

                return SwitchExpression(
                    node.Expression.Parenthesize(),
                    Token(leading: default, SyntaxKind.SwitchKeyword, node.CloseParenToken.TrailingTrivia),
                    Token(SyntaxKind.OpenBraceToken),
                    SeparatedList(
                        switchArms.Select(t => t.armExpression.WithLeadingTrivia(t.leadingTrivia)),
                        switchArms.Select(t => Token(leading: default, SyntaxKind.CommaToken, t.trailingTrivia))),
                    Token(SyntaxKind.CloseBraceToken));
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
