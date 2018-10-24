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
    [ExportLanguageService(typeof(IIfLikeStatementGenerator), LanguageNames.CSharp), Shared]
    internal sealed class CSharpIfLikeStatementGenerator : IIfLikeStatementGenerator
    {
        public bool IsIfLikeStatement(SyntaxNode node) => node is IfStatementSyntax;

        public bool IsCondition(SyntaxNode expression, out SyntaxNode ifLikeStatement)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifLikeStatement = ifStatement;
                return true;
            }

            ifLikeStatement = null;
            return false;
        }

        public bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfLikeStatement)
        {
            if (node is IfStatementSyntax && node.Parent is ElseClauseSyntax)
            {
                parentIfLikeStatement = (IfStatementSyntax)node.Parent.Parent;
                return true;
            }

            parentIfLikeStatement = null;
            return false;
        }

        public SyntaxNode GetCondition(SyntaxNode ifLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;
            return ifStatement.Condition;
        }

        public SyntaxNode GetRootIfStatement(SyntaxNode ifLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;

            while (ifStatement.Parent is ElseClauseSyntax elseClause)
            {
                ifStatement = (IfStatementSyntax)elseClause.Parent;
            }

            return ifStatement;
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

        public SyntaxNode WithCondition(SyntaxNode ifLikeStatement, SyntaxNode condition)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;
            return ifStatement.WithCondition((ExpressionSyntax)condition);
        }

        public SyntaxNode WithStatement(SyntaxNode ifLikeStatement, SyntaxNode statement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;
            return ifStatement.WithStatement(SyntaxFactory.Block((StatementSyntax)statement));
        }

        public SyntaxNode WithStatementsOf(SyntaxNode ifLikeStatement, SyntaxNode otherIfLikeStatement)
        {
            var ifStatement = (IfStatementSyntax)ifLikeStatement;
            var otherIfStatement = (IfStatementSyntax)otherIfLikeStatement;
            return ifStatement.WithStatement(otherIfStatement.Statement);
        }

        public SyntaxNode ToIfStatement(SyntaxNode ifLikeStatement)
            => ifLikeStatement;

        public SyntaxNode ToElseIfClause(SyntaxNode ifLikeStatement)
            => ((IfStatementSyntax)ifLikeStatement).WithElse(null);

        public void InsertElseIfClause(SyntaxEditor editor, SyntaxNode afterIfLikeStatement, SyntaxNode elseIfClause)
        {
            var ifStatement = (IfStatementSyntax)afterIfLikeStatement;
            var elseIfStatement = (IfStatementSyntax)elseIfClause;

            var newElseIfStatement = elseIfStatement.WithElse(ifStatement.Else);
            var newIfStatement = ifStatement.WithElse(SyntaxFactory.ElseClause(newElseIfStatement));

            if (ifStatement.Else == null && ContainsEmbeddedIfStatement(ifStatement))
            {
                // If the if statement contains an embedded if statement (not wrapped inside a block), adding an else
                // clause might introduce a dangling else problem (the 'else' would bind to the inner if statement),
                // so if there used to be no else clause, we'll insert a new block to prevent that.
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
