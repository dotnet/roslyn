// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider<IfStatementSyntax>
    {
        protected override string GetTitle() => CSharpFeaturesResources.Invert_if;

        protected override SyntaxNode GetRootWithInvertIfStatement(
            SyntaxNode root,
            IfStatementSyntax ifNode,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            SyntaxNode negatedExpression)
        {
            var negatedCondition = (ExpressionSyntax)negatedExpression;

            switch (invertIfStyle)
            {
                case InvertIfStyle.IfWithElse_SwapIfBodyWithElseBody:
                    {
                        // For single line statement, we swap the TrailingTrivia to preserve the single line
                        StatementSyntax newIfNodeStatement;
                        ElseClauseSyntax newElseStatement;

                        var hasNewLineAfterClosingBrace = ifNode.Statement.GetTrailingTrivia().Any(trivia => trivia.Kind() == SyntaxKind.EndOfLineTrivia);
                        if (hasNewLineAfterClosingBrace)
                        {
                            newIfNodeStatement = ifNode.Else.Statement.Kind() != SyntaxKind.Block
                                ? SyntaxFactory.Block(ifNode.Else.Statement)
                                : ifNode.Else.Statement;
                            newElseStatement = ifNode.Else.WithStatement(ifNode.Statement);
                        }
                        else
                        {
                            var elseTrailingTrivia = ifNode.Else.GetTrailingTrivia();
                            var ifTrailingTrivia = ifNode.Statement.GetTrailingTrivia();
                            newIfNodeStatement = ifNode.Else.Statement.WithTrailingTrivia(ifTrailingTrivia);
                            newElseStatement = ifNode.Else.WithStatement(ifNode.Statement).WithTrailingTrivia(elseTrailingTrivia);
                        }

                        var newIfStatment = ifNode.Else.Statement.Kind() == SyntaxKind.IfStatement && newIfNodeStatement.Kind() != SyntaxKind.Block
                            ? SyntaxFactory.Block(newIfNodeStatement)
                            : newIfNodeStatement;

                        var updatedIfStatement = ifNode.WithCondition(negatedCondition)
                            .WithStatement(newIfStatment)
                            .WithElse(newElseStatement);

                        if (hasNewLineAfterClosingBrace)
                        {
                            updatedIfStatement = updatedIfStatement.WithAdditionalAnnotations(Formatter.Annotation);
                        }

                        return root.ReplaceNode(ifNode, updatedIfStatement);
                    }

                case InvertIfStyle.IfWithoutElse_MoveIfBodyToElseClause:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block())
                            .WithElse(SyntaxFactory.ElseClause(ifNode.Statement.WithoutLeadingTrivia()));

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.IfWithoutElse_WithNegatedCondition:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition);

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.IfWithoutElse_SwapIfBodyWithSubsequentStatements:
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

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                case InvertIfStyle.IfWithoutElse_WithNearmostJumpStatement:
                    {
                        var newIfBody = GetNearmostParentJumpStatement();
                        var updatedIf = ifNode.WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block(newIfBody));

                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifNode.Statement)))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                    StatementSyntax GetNearmostParentJumpStatement()
                    {
                        switch ((SyntaxKind)GetNearmostParentJumpStatementRawKind(ifNode))
                        {
                            case SyntaxKind.ContinueStatement:
                                return SyntaxFactory.ContinueStatement();
                            case SyntaxKind.BreakStatement:
                                return SyntaxFactory.BreakStatement();
                            case SyntaxKind.ReturnStatement:
                                return SyntaxFactory.ReturnStatement();
                            default:
                                throw ExceptionUtilities.Unreachable;
                        }
                    }

                case InvertIfStyle.IfWithoutElse_WithSubsequentExitPointStatement:
                    {
                        var newIfBody = (StatementSyntax)subsequentSingleExitPointOpt;
                        var updatedIf = ifNode.WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block(newIfBody));

                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(currentParent, statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifNode.Statement)).Concat(newIfBody))
                            .WithAdditionalAnnotations(Formatter.Annotation);

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                case InvertIfStyle.IfWithoutElse_MoveSubsequentStatementsToIfBody:
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

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                case InvertIfStyle.IfWithoutElse_WithElseClause:
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

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(invertIfStyle);
            }
        }

        private static SyntaxList<StatementSyntax> UnwrapBlock(StatementSyntax ifBody)
        {
            return ifBody is BlockSyntax block
                ? block.Statements
                : SyntaxFactory.SingletonList(ifBody);
        }

        private static StatementSyntax ReplaceEmbeddedStatement(StatementSyntax statement, StatementSyntax[] statements)
        {
            if (statements.Length > 0)
            {
                // FIXME preserve comments
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

        protected override TextSpan GetHeaderSpan(IfStatementSyntax ifNode)
        {
            return TextSpan.FromBounds(
                ifNode.IfKeyword.SpanStart,
                ifNode.CloseParenToken.Span.End);
        }

        protected override bool IsElseless(IfStatementSyntax ifNode)
            => ifNode.Else == null;

        protected override bool CanInvert(IfStatementSyntax ifNode)
            => ifNode.IsParentKind(SyntaxKind.Block, SyntaxKind.SwitchSection);

        protected override SyntaxNode GetCondition(IfStatementSyntax ifNode)
            => ifNode.Condition;

        protected override StatementRange GetIfBodyStatementRange(IfStatementSyntax ifNode)
            => new StatementRange(ifNode.Statement, ifNode.Statement);

        protected override int GetNearmostParentJumpStatementRawKind(IfStatementSyntax ifNode)
            => (int)GetNearmostParentJumpStatementKind(ifNode);

        private static SyntaxKind GetNearmostParentJumpStatementKind(IfStatementSyntax ifNode)
        {
            foreach (var node in ifNode.Ancestors())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.SwitchSection:
                        return SyntaxKind.BreakStatement;

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
                        return SyntaxKind.ReturnStatement;

                    case SyntaxKind.DoStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.ForEachVariableStatement:
                        return SyntaxKind.ContinueStatement;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        protected override IEnumerable<StatementRange> GetSubsequentStatementRanges(IfStatementSyntax ifNode)
        {
            StatementSyntax innerStatement = ifNode;
            foreach (var node in ifNode.Ancestors())
            {
                var nextStatement = innerStatement.GetNextStatement();
                if (nextStatement != null && node.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection))
                {
                    yield return new StatementRange(nextStatement, GetStatements(node).Last());
                }

                switch (node.Kind())
                {
                    case SyntaxKind.Block:
                        // Continue walking up to visit possible outer blocks
                        break;

                    case SyntaxKind.SwitchSection:
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
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.ForEachVariableStatement:
                        // We no longer need to continue since other statements
                        // are out of reach, as far as this analysis concerned.
                        yield break;
                }

                if (node is StatementSyntax statement)
                {
                    innerStatement = statement;
                }
            }
        }

        protected override bool IsEmptyStatementRange(StatementRange statementRange)
        {
            // FIXME check for empty blocks
            return statementRange.IsSingleStatement && statementRange.FirstStatement.IsKind(SyntaxKind.EmptyStatement);
        }
    }
}

