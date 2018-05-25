// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider<IfStatementSyntax, StatementSyntax>
    {
        protected override string GetTitle() => CSharpFeaturesResources.Invert_if;

#if false
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
#endif

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

        protected override bool IsStatementContainer(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block, SyntaxKind.SwitchSection);

        protected override bool IsNoOpSyntaxNode(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block, SyntaxKind.EmptyStatement);

        protected override bool IsStatement(SyntaxNode node)
            => node is StatementSyntax;

        protected override SyntaxNode GetNextExecutableStatement(SyntaxNode node)
            => CSharpSyntaxFactsService.Instance.GetNextExecutableStatement(node);

        protected override bool CanControlFlowOut(SyntaxNode node)
        {
            switch (node.Kind())
            {
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
                    return false;
            }

            return true;
        }

        protected override SyntaxList<SyntaxNode> GetStatements(SyntaxNode node)
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

        protected override int GetJumpStatementRawKind(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SwitchSection:
                    return (int)SyntaxKind.BreakStatement;

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
                    return (int)SyntaxKind.ReturnStatement;

                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    return (int)SyntaxKind.ContinueStatement;
            }

            return -1;
        }

        protected override SyntaxNode GetJumpStatement(int rawKind)
        {
            switch ((SyntaxKind)rawKind)
            {
                case SyntaxKind.ContinueStatement:
                    return SyntaxFactory.ContinueStatement();
                case SyntaxKind.BreakStatement:
                    return SyntaxFactory.BreakStatement();
                case SyntaxKind.ReturnStatement:
                    return SyntaxFactory.ReturnStatement();
                default:
                    throw ExceptionUtilities.UnexpectedValue(rawKind);
            }
        }

        protected override StatementSyntax GetIfBody(IfStatementSyntax ifNode)
        {
            return ifNode.Statement;
        }

        protected override StatementSyntax AsEmbeddedStatement(
            StatementSyntax originalStatement,
            IEnumerable<SyntaxNode> newStatements)
        {
            var statementsArray = newStatements.ToArray();

            if (statementsArray.Length > 0)
            {
                // FIXME preserve comments
                statementsArray[0] = statementsArray[0].WithoutLeadingTrivia();
            }

            // FIXME remove leading trivia except for comments
            return originalStatement is BlockSyntax block
                ? block.WithStatements(SyntaxFactory.List(statementsArray))
                : statementsArray.Length == 1
                    ? (StatementSyntax)statementsArray.Single()
                    : SyntaxFactory.Block(statementsArray.Cast<StatementSyntax>());
        }

        protected override StatementSyntax GetEmptyEmbeddedStatement()
        {
            return SyntaxFactory.Block();
        }

        protected override SyntaxNode UpdateIf(
            IfStatementSyntax ifNode,
            SyntaxNode condition,
            StatementSyntax trueStatement = null,
            StatementSyntax falseStatement = null)
        {
            var updatedIf = ifNode.WithCondition((ExpressionSyntax)condition);

            if (trueStatement != null)
            {
                updatedIf = updatedIf.WithStatement(
                    trueStatement is IfStatementSyntax
                        ? SyntaxFactory.Block(trueStatement)
                        : trueStatement);
            }

            if (falseStatement != null)
            {
                updatedIf = updatedIf.WithElse(SyntaxFactory.ElseClause(falseStatement));
            }

            return updatedIf;
        }

        protected override SyntaxNode WithStatements(SyntaxNode node, IEnumerable<SyntaxNode> statements)
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

        protected override StatementSyntax GetElseBody(IfStatementSyntax ifNode)
        {
            Debug.Assert(ifNode.Else != null);
            return ifNode.Else.Statement;
        }

        protected override IEnumerable<SyntaxNode> UnwrapBlock(StatementSyntax ifBody)
        {
            return ifBody is BlockSyntax block
                ? block.Statements
                : SyntaxFactory.SingletonList(ifBody);
        }
    }
}

