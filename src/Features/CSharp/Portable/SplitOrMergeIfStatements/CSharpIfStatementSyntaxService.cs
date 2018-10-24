// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportLanguageService(typeof(IIfStatementSyntaxService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpIfStatementSyntaxService : IIfStatementSyntaxService
    {
        public int IfKeywordKind => (int)SyntaxKind.IfKeyword;

        public int LogicalAndExpressionKind => (int)SyntaxKind.LogicalAndExpression;

        public int LogicalOrExpressionKind => (int)SyntaxKind.LogicalOrExpression;

        public bool IsIfLikeStatement(SyntaxNode node) => node is IfStatementSyntax;

        public bool IsConditionOfIfLikeStatement(SyntaxNode expression, out SyntaxNode ifLikeStatement)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifLikeStatement = ifStatement;
                return true;
            }

            ifLikeStatement = null;
            return false;
        }

        public SyntaxNode GetConditionOfIfLikeStatement(SyntaxNode ifLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;
            return ifStatement.Condition;
        }

        public ImmutableArray<SyntaxNode> GetElseLikeClauses(SyntaxNode ifLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;

            var builder = ImmutableArray.CreateBuilder<SyntaxNode>();

            while (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
            {
                builder.Add(elseIfStatement);
                ifStatement = elseIfStatement;
            }

            if (ifStatement.Else != null)
            {
                builder.Add(ifStatement.Else);
            }

            return builder.ToImmutable();
        }

        public SyntaxNode WithCondition(SyntaxNode ifOrElseIfNode, SyntaxNode condition)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIfNode;
            return ifStatement.WithCondition((ExpressionSyntax)condition);
        }

        public SyntaxNode WithStatement(SyntaxNode ifOrElseIfNode, SyntaxNode statement)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIfNode;
            return ifStatement.WithStatement(SyntaxFactory.Block((StatementSyntax)statement));
        }

        public SyntaxNode WithStatementsOf(SyntaxNode ifOrElseIfNode, SyntaxNode otherIfOrElseIfNode)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIfNode;
            var otherIfStatement = (IfStatementSyntax)otherIfOrElseIfNode;
            return ifStatement.WithStatement(otherIfStatement.Statement);
        }

        public SyntaxNode ToIfStatement(SyntaxNode ifOrElseIfNode)
            => ifOrElseIfNode;

        public SyntaxNode ToElseIfClause(SyntaxNode ifOrElseIfNode)
            => ((IfStatementSyntax)ifOrElseIfNode).WithElse(null);

        public void InsertElseIfClause(SyntaxEditor editor, SyntaxNode ifOrElseIfNode, SyntaxNode elseIfClause)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIfNode;
            var elseIfStatement = (IfStatementSyntax)elseIfClause;

            var newElseIfStatement = elseIfStatement.WithElse(ifStatement.Else);
            var newIfStatement = ifStatement.WithElse(SyntaxFactory.ElseClause(newElseIfStatement));

            if (ifStatement.Else == null && ContainsEmbeddedIfStatement(ifStatement))
            {
                newIfStatement = newIfStatement.WithStatement(SyntaxFactory.Block(newIfStatement.Statement));
            }

            editor.ReplaceNode(ifStatement, newIfStatement);
        }

        public void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause)
        {
            var elseIfStatement = (IfStatementSyntax)elseIfClause;
            var elseClause = (ElseClauseSyntax)elseIfStatement.Parent;
            var parentIfStatement = (IfStatementSyntax)elseClause.Parent;

            editor.ReplaceNode(parentIfStatement, parentIfStatement.WithElse(elseIfStatement.Else));
        }

        private static bool ContainsEmbeddedIfStatement(IfStatementSyntax ifStatement)
        {
            for (var statement = ifStatement.Statement; statement.IsEmbeddedStatementOwner(); statement = statement.GetEmbeddedStatement())
            {
                if (statement.IsKind(SyntaxKind.IfStatement))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
