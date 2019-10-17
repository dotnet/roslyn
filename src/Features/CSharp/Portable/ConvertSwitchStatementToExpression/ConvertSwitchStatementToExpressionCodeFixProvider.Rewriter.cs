// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
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
                SwitchStatementSyntax switchStatement,
                ITypeSymbol declaratorToRemoveType,
                SemanticModel semanticModel,
                SyntaxKind nodeToGenerate, bool shouldMoveNextStatementToSwitchExpression, bool generateDeclaration)
            {
                var rewriter = new Rewriter(isAllThrowStatements: nodeToGenerate == SyntaxKind.ThrowStatement);

                // Rewrite the switch statement as a switch expression.
                var switchExpression = rewriter.RewriteSwitchStatement(switchStatement, declaratorToRemoveType, semanticModel,
                    allowMoveNextStatementToSwitchExpression: shouldMoveNextStatementToSwitchExpression);

                // Generate the final statement to wrap the switch expression, e.g. a "return" or an assignment.
                return rewriter.GetFinalStatement(switchExpression,
                    switchStatement.SwitchKeyword.LeadingTrivia, nodeToGenerate, generateDeclaration);
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

            private SwitchExpressionArmSyntax GetSwitchExpressionArm(SwitchSectionSyntax node, ITypeSymbol declaratorToRemoveType, SemanticModel semanticModel)
            {
                return SwitchExpressionArm(
                    pattern: GetPattern(SingleOrDefaultSwitchLabel(node.Labels), out var whenClauseOpt),
                    whenClause: whenClauseOpt,
                    expression: RewriteStatements(node.Statements, declaratorToRemoveType, semanticModel));
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
                _assignmentTargetOpt ??= node.Left;
                return node.Right;
            }

            private ExpressionSyntax RewriteStatements(SyntaxList<StatementSyntax> statements, ITypeSymbol declaratorToRemoveType, SemanticModel semanticModel)
            {
                Debug.Assert(statements.Count == 1 || statements.Count == 2);
                Debug.Assert(!statements[0].IsKind(SyntaxKind.BreakStatement));
                var rewrittenExpression = Visit(statements[0]);

                if (statements[0].Kind() == SyntaxKind.ExpressionStatement)
                {
                    var expressionStatement = (ExpressionStatementSyntax)statements[0];
                    SyntaxNode type = null;
                    if (expressionStatement.Expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                    {
                        var objCreationNode = expressionStatement.Expression.ChildNodes().Where(s => s.Kind() == SyntaxKind.ObjectCreationExpression).FirstOrDefault();
                        if (objCreationNode != default(SyntaxNode))
                        {
                            type = objCreationNode.ChildNodes().Where(s => s.Kind() == SyntaxKind.IdentifierName).FirstOrDefault();
                        }
                        else
                        {
                            type = expressionStatement.Expression.ChildNodes().Where(s => s.Kind() == SyntaxKind.IdentifierName).FirstOrDefault();
                        }
                    }

                    if (declaratorToRemoveType != null && type != null)
                    {
                        rewrittenExpression = ExpressionSyntaxExtensions.CastIfPossible(rewrittenExpression, declaratorToRemoveType, type.GetLocation().SourceSpan.Start, semanticModel);
                    }
                }

                return rewrittenExpression;
            }

            public override ExpressionSyntax VisitSwitchStatement(SwitchStatementSyntax node)
            {
                return RewriteSwitchStatement(node);
            }

            private static SwitchLabelSyntax SingleOrDefaultSwitchLabel(SyntaxList<SwitchLabelSyntax> labels)
            {
                return labels.Count == 1
                    ? labels[0]
                    : labels.First(x => x.IsKind(SyntaxKind.DefaultSwitchLabel));
            }

            private ExpressionSyntax RewriteSwitchStatement(SwitchStatementSyntax node, ITypeSymbol declaratorToRemoveType = null, SemanticModel semanticModel = null, bool allowMoveNextStatementToSwitchExpression = true)
            {
                var switchArms = node.Sections
                    // The default label must come last in the switch expression.
                    .OrderBy(section => SingleOrDefaultSwitchLabel(section.Labels).IsKind(SyntaxKind.DefaultSwitchLabel))
                    .Select(s =>
                        (tokensForLeadingTrivia: new[] { s.Labels[0].GetFirstToken(), s.Labels[0].GetLastToken() },
                         tokensForTrailingTrivia: new[] { s.Statements[0].GetFirstToken(), s.Statements[0].GetLastToken() },
                         armExpression: GetSwitchExpressionArm(s, declaratorToRemoveType, semanticModel)))
                    .ToList();

                if (allowMoveNextStatementToSwitchExpression)
                {
                    var nextStatement = node.GetNextStatement();
                    if (nextStatement.IsKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement))
                    {
                        switchArms.Add(
                            (tokensForLeadingTrivia: new[] { nextStatement.GetFirstToken() },
                             tokensForTrailingTrivia: new[] { nextStatement.GetLastToken() },
                             SwitchExpressionArm(DiscardPattern(), Visit(nextStatement))));
                    }
                }

                return SwitchExpression(
                    node.Expression.Parenthesize(),
                    Token(leading: default, SyntaxKind.SwitchKeyword, node.CloseParenToken.TrailingTrivia),
                    Token(SyntaxKind.OpenBraceToken),
                    SeparatedList(
                        switchArms.Select(t => t.armExpression.WithLeadingTrivia(t.tokensForLeadingTrivia.GetTrivia().FilterComments(addElasticMarker: false))),
                        switchArms.Select(t => Token(SyntaxKind.CommaToken).WithTrailingTrivia(t.tokensForTrailingTrivia.GetTrivia().FilterComments(addElasticMarker: true)))),
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
