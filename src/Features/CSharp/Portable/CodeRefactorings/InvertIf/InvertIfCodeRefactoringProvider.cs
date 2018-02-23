// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed partial class InvertIfCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly Dictionary<SyntaxKind, (SyntaxKind, SyntaxKind)> s_binaryMap =
            new Dictionary<SyntaxKind, (SyntaxKind, SyntaxKind)>(SyntaxFacts.EqualityComparer)
                {
                    { SyntaxKind.EqualsExpression, (SyntaxKind.NotEqualsExpression, SyntaxKind.ExclamationEqualsToken) },
                    { SyntaxKind.NotEqualsExpression, (SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken) },
                    { SyntaxKind.LessThanExpression, (SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken) },
                    { SyntaxKind.LessThanOrEqualExpression, (SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken) },
                    { SyntaxKind.GreaterThanExpression, (SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken) },
                    { SyntaxKind.GreaterThanOrEqualExpression, (SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken) },
                };

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ifStatement = root.FindToken(textSpan.Start).GetAncestor<IfStatementSyntax>();
            if (ifStatement == null)
            {
                return;
            }

            if (!ifStatement.IfKeyword.Span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var invertIfStyle = GetInvertIfStyleAsync(semanticModel, ifStatement,
                out var subsequenceExitPoint,
                out var jumpStatementKind);
            if (invertIfStyle == InvertIfStyle.None)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Invert_if_statement,
                    c => InvertIfAsync(document, ifStatement, invertIfStyle, jumpStatementKind, subsequenceExitPoint, c)));
        }

        private static InvertIfStyle GetInvertIfStyleAsync(
            SemanticModel semanticModel, IfStatementSyntax ifStatement, out SyntaxNode subsequenceExitPoint, out SyntaxKind jumpStatementKind)
        {
            subsequenceExitPoint = null;
            jumpStatementKind = default;

            if (ifStatement.Else != null)
            {
                return InvertIfStyle.Normal;
            }

            switch (ifStatement.Parent?.Kind())
            {
                case SyntaxKind.Block:
                case SyntaxKind.SwitchSection:
                    break;
                default:
                    return InvertIfStyle.None;
            }

            AnalyzeIfStatement(semanticModel, ifStatement.Statement,
                out BlockStyle ifStatementBlockStyle,
                out SyntaxNode ifStatementExitPoint,
                out StatementStyle ifStatementStyle);

            if (ifStatementBlockStyle == BlockStyle.Empty)
            {
                return InvertIfStyle.WithNegatedCondition;
            }

            AnalyzeSubsequence(semanticModel, ifStatement,
                out subsequenceExitPoint,
                out jumpStatementKind,
                out BlockStyle subsequenceBlockStyle,
                out StatementStyle subsequenceStatementStyle,
                out bool hasOuterBlockStatements);

            switch (subsequenceBlockStyle)
            {
                case BlockStyle.Empty:
                    return InvertIfStyle.WithNearmostJump;

                case BlockStyle.Reachable:
                    if (!hasOuterBlockStatements && 
                        ifStatementExitPoint.IsKind(jumpStatementKind) &&
                        ifStatementStyle == StatementStyle.SingleStatement)
                    {
                        return InvertIfStyle.MoveSubsequenceToElseBody;
                    }
                    else
                    {
                        return InvertIfStyle.WithElseClause;
                    }

                case BlockStyle.Unreachable:
                    if (ifStatementBlockStyle == BlockStyle.Reachable)
                    {
                        if (subsequenceExitPoint != null &&
                            subsequenceStatementStyle == StatementStyle.SingleStatement)
                        {
                            return InvertIfStyle.WithSubsequenceExitPoint;
                        }
                        else
                        {
                            return InvertIfStyle.MoveIfBodyToElseClause;
                        }
                    }

                    if (!hasOuterBlockStatements)
                    {
                        return InvertIfStyle.SwapIfBodyWithSubsequence;
                    }
                    else
                    {
                        return InvertIfStyle.MoveIfBodyToElseClause;
                    }
            }

            return InvertIfStyle.None;
        }

        private static void AnalyzeIfStatement(
            SemanticModel semanticModel,
            StatementSyntax statement,
            out BlockStyle blockStyle,
            out SyntaxNode ifStatementExitPoint,
            out StatementStyle statementStyle)
        {
            statementStyle = GetStatementStyle(statement);
            if (statementStyle == StatementStyle.Empty)
            {
                ifStatementExitPoint = null;
                blockStyle = BlockStyle.Empty;
                return;
            }

            var controlFlow = semanticModel.AnalyzeControlFlow(statement);
            if (controlFlow.EndPointIsReachable)
            {
                ifStatementExitPoint = null;
                blockStyle = BlockStyle.Reachable;
            }
            else
            {
                var exitPoints = controlFlow.ExitPoints;
                ifStatementExitPoint = exitPoints.Length == 1 ? exitPoints[0] : null;
                blockStyle = BlockStyle.Unreachable;
            }
        }

        private static void AnalyzeStatements(
            SemanticModel semanticModel,
            in SyntaxList<StatementSyntax> statements,
            StatementSyntax innerStatement,
            out BlockStyle blockStyle,
            ref SyntaxNode subsequenceExitPoint,
            ref StatementStyle statementStyle)
        {
            Debug.Assert(subsequenceExitPoint is null);

            var nextIndex = statements.IndexOf(innerStatement) + 1;
            Debug.Assert(nextIndex > 0);
            statementStyle |= GetStatementStyle(statements, nextIndex);
            if (nextIndex >= statements.Count || statementStyle == StatementStyle.Empty)
            {
                subsequenceExitPoint = null;
                blockStyle = BlockStyle.Empty;
                return;
            }

            var controlFlow = semanticModel.AnalyzeControlFlow(statements[nextIndex], statements.Last());
            if (controlFlow.EndPointIsReachable)
            {
                subsequenceExitPoint = null;
                blockStyle = BlockStyle.Reachable;
            }
            else
            {
                var exitPoints = controlFlow.ExitPoints;
                subsequenceExitPoint = exitPoints.Length == 1 ? exitPoints[0] : null;
                blockStyle = BlockStyle.Unreachable;
            }
        }

        private static StatementStyle GetStatementStyle(in SyntaxList<StatementSyntax> statements, int startIndex = 0)
        {
            bool sawOneStatement = false;
            for (int i = startIndex, n = statements.Count; i < n; ++i)
            {
                var statement = statements[i];
                switch (statement.Kind())
                {
                    case SyntaxKind.LocalFunctionStatement:
                    case SyntaxKind.EmptyStatement:
                        continue;
                    case SyntaxKind.Block:
                        switch (GetStatementStyle(((BlockSyntax)statement).Statements))
                        {
                            case StatementStyle.Empty:
                                continue;
                            case StatementStyle.MultiStatement:
                                return StatementStyle.MultiStatement;
                        }

                        break;
                }

                if (sawOneStatement)
                {
                    return StatementStyle.MultiStatement;
                }

                sawOneStatement = true;
            }

            return sawOneStatement
                ? StatementStyle.SingleStatement
                : StatementStyle.Empty;
        }

        private static StatementStyle GetStatementStyle(StatementSyntax embeddedStatement)
        {
            switch (embeddedStatement.Kind())
            {
                case SyntaxKind.EmptyStatement:
                    return StatementStyle.Empty;
                case SyntaxKind.Block:
                    return GetStatementStyle(((BlockSyntax)embeddedStatement).Statements);
                default:
                    return StatementStyle.SingleStatement;
            }
        }

        private static void AnalyzeSubsequence(
            SemanticModel semanticModel,
            IfStatementSyntax ifStatement,
            out SyntaxNode subsequenceExitPoint,
            out SyntaxKind jumpStatementKind,
            out BlockStyle blockStyle,
            out StatementStyle statementStyle,
            out bool hasOuterBlockStatements)
        {
            StatementSyntax innerStatement = ifStatement;

            blockStyle = BlockStyle.Empty;
            statementStyle = StatementStyle.Empty;

            subsequenceExitPoint = null;
            hasOuterBlockStatements = false;

            foreach (var node in ifStatement.Ancestors())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.Block:
                        if (blockStyle != BlockStyle.Unreachable)
                        {
                            var block = (BlockSyntax)node;
                            AnalyzeStatements(semanticModel, block.Statements, innerStatement,
                                out var currentBlockStyle,
                                ref subsequenceExitPoint,
                                ref statementStyle);
                            if (currentBlockStyle != BlockStyle.Empty && innerStatement != ifStatement)
                            {
                                hasOuterBlockStatements = true;
                            }

                            blockStyle |= currentBlockStyle;
                        }

                        break;

                    case SyntaxKind.SwitchSection:
                        if (blockStyle != BlockStyle.Unreachable)
                        {
                            var section = (SwitchSectionSyntax)node;
                            AnalyzeStatements(semanticModel, section.Statements, innerStatement,
                                out var currentBlockStyle,
                                ref subsequenceExitPoint,
                                ref statementStyle);
                            if (currentBlockStyle != BlockStyle.Empty && innerStatement != ifStatement)
                            {
                                hasOuterBlockStatements = true;
                            }

                            blockStyle |= currentBlockStyle;
                        }

                        jumpStatementKind = SyntaxKind.BreakStatement;
                        return;

                    case SyntaxKind.LocalFunctionStatement:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        jumpStatementKind = SyntaxKind.ReturnStatement;
                        return;

                    case SyntaxKind.DoStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.ForEachVariableStatement:
                        jumpStatementKind = SyntaxKind.ContinueStatement;
                        return;
                }

                innerStatement = node as StatementSyntax;
            }

            jumpStatementKind = SyntaxKind.None;
        }

        private static async Task<Document> InvertIfAsync(Document document, IfStatementSyntax ifNode, InvertIfStyle invertIfStyle, SyntaxKind jumpStatementKind, SyntaxNode subsequenceExitPoint, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var negatedCondition = Negate(ifNode.Condition, semanticModel, cancellationToken);

            switch (invertIfStyle)
            {
                case InvertIfStyle.Normal:
                    {
                        // In the case that the else clause is actually an else if clause, place the if
                        // statement to be moved in a new block in order to make sure that the else
                        // statement matches the right if statement after the edit.
                        var newIfNodeStatement = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement ?
                            SyntaxFactory.Block(ifNode.Else.Statement) :
                            ifNode.Else.Statement;

                        var invertedIf = ifNode.WithCondition(negatedCondition)
                                    .WithStatement(newIfNodeStatement)
                                    .WithElse(ifNode.Else.WithStatement(ifNode.Statement))
                                    .WithAdditionalAnnotations(Formatter.Annotation);

                        var result = root.ReplaceNode(ifNode, invertedIf);
                        return document.WithSyntaxRoot(result);
                    }

                case InvertIfStyle.MoveIfBodyToElseClause:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block())
                            .WithElse(SyntaxFactory.ElseClause(ifNode.Statement.WithoutLeadingTrivia()));

                        return document.WithSyntaxRoot(root.ReplaceNode(ifNode, updatedIf));
                    }

                case InvertIfStyle.WithNegatedCondition:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition);

                        return document.WithSyntaxRoot(root.ReplaceNode(ifNode, updatedIf));
                    }

                case InvertIfStyle.SwapIfBodyWithSubsequence:
                    {
                        var ifBody = ifNode.Statement;

                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1).ToArray();

                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(ReplaceEmbeddedStatement(ifNode.Statement, statementsAfterIf));

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifBody)))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return document.WithSyntaxRoot(root.ReplaceNode(currentParent, updatedParent));
                    }

                case InvertIfStyle.WithNearmostJump:
                    {
                        var newIfBody = GetNearmostAncestorJumpStatement();
                        var updatedIf = ifNode.WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block(newIfBody));

                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifNode.Statement)))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return document.WithSyntaxRoot(root.ReplaceNode(currentParent, updatedParent));
                    }

                    StatementSyntax GetNearmostAncestorJumpStatement()
                    {
                        switch (jumpStatementKind)
                        {
                            case SyntaxKind.ContinueStatement:
                                return SyntaxFactory.ContinueStatement();
                            case SyntaxKind.BreakStatement:
                                return SyntaxFactory.BreakStatement();
                            case SyntaxKind.ReturnStatement:
                                return SyntaxFactory.ReturnStatement();
                            case var syntaxKind:
                                throw ExceptionUtilities.UnexpectedValue(syntaxKind);
                        }
                    }

                case InvertIfStyle.WithSubsequenceExitPoint:
                    {
                        var newIfBody = (StatementSyntax)subsequenceExitPoint;
                        var updatedIf = ifNode.WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block(newIfBody));

                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifNode.Statement)).Concat(newIfBody))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return document.WithSyntaxRoot(root.ReplaceNode(currentParent, updatedParent));
                    }

                case InvertIfStyle.MoveSubsequenceToElseBody:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1).ToArray();

                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(ReplaceEmbeddedStatement(ifNode.Statement, statementsAfterIf));

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return document.WithSyntaxRoot(root.ReplaceNode(currentParent, updatedParent));
                    }

                case InvertIfStyle.WithElseClause:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1).ToArray();

                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(ReplaceEmbeddedStatement(ifNode.Statement, statementsAfterIf))
                            .WithElse(SyntaxFactory.ElseClause(ifNode.Statement));

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return document.WithSyntaxRoot(root.ReplaceNode(currentParent, updatedParent));
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(invertIfStyle);
            }
        }

        private static IEnumerable<StatementSyntax> UnwrapBlock(StatementSyntax ifBody)
        {
            return ifBody is BlockSyntax block ? block.Statements : (IEnumerable<StatementSyntax>)new[] { ifBody };
        }

        private static StatementSyntax ReplaceEmbeddedStatement(StatementSyntax statement, StatementSyntax[] statements)
        {
            if (statements.Length > 0)
            {
                statements[0] = statements[0].WithoutLeadingTrivia();
            }

            return statement is BlockSyntax block
                ? block.WithStatements(SyntaxFactory.List(statements))
                : statements.Length == 1 ? statements[0] : SyntaxFactory.Block(statements);
        }

        private static SyntaxList<StatementSyntax> GetStatements(SyntaxNode node)
        {
            switch (node)
            {
                case BlockSyntax n:
                    return n.Statements;
                case SwitchSectionSyntax n:
                    return n.Statements;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }

        private static SyntaxNode WithStatements(SyntaxNode node, IEnumerable<SyntaxNode> statements)
        {
            switch (node)
            {
                case BlockSyntax n:
                    return n.WithStatements(SyntaxFactory.List(statements));
                case SwitchSectionSyntax n:
                    return n.WithStatements(SyntaxFactory.List(statements));
                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }

        private static bool IsComparisonOfZeroAndSomethingNeverLessThanZero(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var canSimplify = false;

            if (binaryExpression.Kind() == SyntaxKind.GreaterThanExpression &&
                binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Left,
                    (LiteralExpressionSyntax)binaryExpression.Right,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.LessThanExpression &&
                     binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Right,
                    (LiteralExpressionSyntax)binaryExpression.Left,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                     binaryExpression.Right.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Left,
                    (LiteralExpressionSyntax)binaryExpression.Right,
                    semanticModel,
                    cancellationToken);
            }
            else if (binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                     binaryExpression.Left.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                canSimplify = CanSimplifyToLengthEqualsZeroExpression(
                    binaryExpression.Right,
                    (LiteralExpressionSyntax)binaryExpression.Left,
                    semanticModel,
                    cancellationToken);
            }

            return canSimplify;
        }

        private static bool CanSimplifyToLengthEqualsZeroExpression(
            ExpressionSyntax variableExpression,
            LiteralExpressionSyntax numericLiteralExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var numericValue = semanticModel.GetConstantValue(numericLiteralExpression, cancellationToken);
            if (numericValue.HasValue && numericValue.Value is int && (int)numericValue.Value == 0)
            {
                var symbol = semanticModel.GetSymbolInfo(variableExpression, cancellationToken).Symbol;

                if (symbol != null && (symbol.Name == "Length" || symbol.Name == "LongLength"))
                {
                    var containingType = symbol.ContainingType;
                    if (containingType != null &&
                        (containingType.SpecialType == SpecialType.System_Array ||
                         containingType.SpecialType == SpecialType.System_String))
                    {
                        return true;
                    }
                }

                var typeInfo = semanticModel.GetTypeInfo(variableExpression, cancellationToken);
                if (typeInfo.Type != null)
                {
                    switch (typeInfo.Type.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool TryNegateBinaryComparisonExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ExpressionSyntax result)
        {
            if (s_binaryMap.TryGetValue(expression.Kind(), out var tuple))
            {
                var binaryExpression = (BinaryExpressionSyntax)expression;
                var expressionType = tuple.Item1;
                var operatorType = tuple.Item2;

                // Special case negating Length > 0 to Length == 0 and 0 < Length to 0 == Length
                // for arrays and strings. We can do this because we know that Length cannot be
                // less than 0. Additionally, if we find Length == 0 or 0 == Length, we'll invert
                // it to Length > 0 or 0 < Length, respectively.
                if (IsComparisonOfZeroAndSomethingNeverLessThanZero(binaryExpression, semanticModel, cancellationToken))
                {
                    operatorType = binaryExpression.OperatorToken.Kind() == SyntaxKind.EqualsEqualsToken
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanToken : SyntaxKind.LessThanToken
                        : SyntaxKind.EqualsEqualsToken;
                    expressionType = binaryExpression.Kind() == SyntaxKind.EqualsExpression
                        ? binaryExpression.Right is LiteralExpressionSyntax ? SyntaxKind.GreaterThanExpression : SyntaxKind.LessThanExpression
                        : SyntaxKind.EqualsExpression;
                }

                result = SyntaxFactory.BinaryExpression(
                    expressionType,
                    binaryExpression.Left,
                    SyntaxFactory.Token(
                        binaryExpression.OperatorToken.LeadingTrivia,
                        operatorType,
                        binaryExpression.OperatorToken.TrailingTrivia),
                    binaryExpression.Right);

                return true;
            }

            result = null;
            return false;
        }

        private static ExpressionSyntax Negate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryNegateBinaryComparisonExpression(expression, semanticModel, cancellationToken, out var result))
            {
                return result;
            }

            switch (expression.Kind())
            {
                case SyntaxKind.ParenthesizedExpression:
                    {
                        var parenthesizedExpression = (ParenthesizedExpressionSyntax)expression;
                        return parenthesizedExpression
                            .WithExpression(Negate(parenthesizedExpression.Expression, semanticModel, cancellationToken))
                            .WithAdditionalAnnotations(Simplifier.Annotation);
                    }

                case SyntaxKind.LogicalNotExpression:
                    {
                        var logicalNotExpression = (PrefixUnaryExpressionSyntax)expression;

                        var notToken = logicalNotExpression.OperatorToken;
                        var nextToken = logicalNotExpression.Operand.GetFirstToken(
                            includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

                        var existingTrivia = SyntaxFactory.TriviaList(
                            notToken.LeadingTrivia.Concat(
                                notToken.TrailingTrivia.Concat(
                                    nextToken.LeadingTrivia)));

                        var updatedNextToken = nextToken.WithLeadingTrivia(existingTrivia);

                        return logicalNotExpression.Operand.ReplaceToken(
                            nextToken,
                            updatedNextToken);
                    }

                case SyntaxKind.LogicalOrExpression:
                    {
                        var binaryExpression = (BinaryExpressionSyntax)expression;
                        result = SyntaxFactory.BinaryExpression(
                            SyntaxKind.LogicalAndExpression,
                            Negate(binaryExpression.Left, semanticModel, cancellationToken),
                            SyntaxFactory.Token(
                                binaryExpression.OperatorToken.LeadingTrivia,
                                SyntaxKind.AmpersandAmpersandToken,
                                binaryExpression.OperatorToken.TrailingTrivia),
                            Negate(binaryExpression.Right, semanticModel, cancellationToken));

                        return result
                            .Parenthesize()
                            .WithLeadingTrivia(binaryExpression.GetLeadingTrivia())
                            .WithTrailingTrivia(binaryExpression.GetTrailingTrivia());
                    }

                case SyntaxKind.LogicalAndExpression:
                    {
                        var binaryExpression = (BinaryExpressionSyntax)expression;
                        result = SyntaxFactory.BinaryExpression(
                            SyntaxKind.LogicalOrExpression,
                            Negate(binaryExpression.Left, semanticModel, cancellationToken),
                            SyntaxFactory.Token(
                                binaryExpression.OperatorToken.LeadingTrivia,
                                SyntaxKind.BarBarToken,
                                binaryExpression.OperatorToken.TrailingTrivia),
                            Negate(binaryExpression.Right, semanticModel, cancellationToken));

                        return result
                            .Parenthesize()
                            .WithLeadingTrivia(binaryExpression.GetLeadingTrivia())
                            .WithTrailingTrivia(binaryExpression.GetTrailingTrivia());
                    }

                case SyntaxKind.TrueLiteralExpression:
                    {
                        var literalExpression = (LiteralExpressionSyntax)expression;
                        return SyntaxFactory.LiteralExpression(
                            SyntaxKind.FalseLiteralExpression,
                            SyntaxFactory.Token(
                                literalExpression.Token.LeadingTrivia,
                                SyntaxKind.FalseKeyword,
                                literalExpression.Token.TrailingTrivia));
                    }

                case SyntaxKind.FalseLiteralExpression:
                    {
                        var literalExpression = (LiteralExpressionSyntax)expression;
                        return SyntaxFactory.LiteralExpression(
                            SyntaxKind.TrueLiteralExpression,
                            SyntaxFactory.Token(
                                literalExpression.Token.LeadingTrivia,
                                SyntaxKind.TrueKeyword,
                                literalExpression.Token.TrailingTrivia));
                    }
            }

            // Anything else we can just negate by adding a ! in front of the parenthesized expression.
            // Unnecessary parentheses will get removed by the simplification service.
            return SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                expression.Parenthesize());
        }

        enum StatementStyle
        {
            Empty = 0b1,
            SingleStatement = 0b11,
            MultiStatement = 0b111
        }

        private enum BlockStyle
        {
            Empty = 0b1,
            Reachable = 0b11,
            Unreachable = 0b111,
        }

        private enum InvertIfStyle
        {
            None,
            // swap if and else
            Normal,
            // swap subsequent statements and if body
            SwapIfBodyWithSubsequence,
            // move subsequent statements to if body
            MoveSubsequenceToElseBody,
            // invert and generete else
            WithElseClause,
            // invert and generate else, keep if-body empty
            MoveIfBodyToElseClause,
            // invert and copy the exit point statement
            WithSubsequenceExitPoint,
            // invert and generate return, break, continue
            WithNearmostJump,
            // just invert the condition
            WithNegatedCondition,
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
