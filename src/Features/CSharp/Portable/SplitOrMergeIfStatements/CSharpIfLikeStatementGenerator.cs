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
        [ImportingConstructor]
        public CSharpIfLikeStatementGenerator()
        {
        }

        public bool IsIfOrElseIf(SyntaxNode node) => node is IfStatementSyntax;

        public bool IsCondition(SyntaxNode expression, out SyntaxNode ifOrElseIf)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifOrElseIf = ifStatement;
                return true;
            }

            ifOrElseIf = null;
            return false;
        }

        public bool IsElseIfClause(SyntaxNode node, out SyntaxNode parentIfOrElseIf)
        {
            if (node is IfStatementSyntax { Parent: ElseClauseSyntax _ })
            {
                parentIfOrElseIf = (IfStatementSyntax)node.Parent.Parent;
                return true;
            }

            parentIfOrElseIf = null;
            return false;
        }

        public bool HasElseIfClause(SyntaxNode ifOrElseIf, out SyntaxNode elseIfClause)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;
            if (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
            {
                elseIfClause = elseIfStatement;
                return true;
            }

            elseIfClause = null;
            return false;
        }

        public SyntaxNode GetCondition(SyntaxNode ifOrElseIf)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;
            return ifStatement.Condition;
        }

        public SyntaxNode GetRootIfStatement(SyntaxNode ifOrElseIf)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;

            while (ifStatement.Parent is ElseClauseSyntax elseClause)
            {
                ifStatement = (IfStatementSyntax)elseClause.Parent;
            }

            return ifStatement;
        }

        public ImmutableArray<SyntaxNode> GetElseIfAndElseClauses(SyntaxNode ifOrElseIf)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;

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

        public SyntaxNode WithCondition(SyntaxNode ifOrElseIf, SyntaxNode condition)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;
            return ifStatement.WithCondition((ExpressionSyntax)condition);
        }

        public SyntaxNode WithStatementInBlock(SyntaxNode ifOrElseIf, SyntaxNode statement)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;
            return ifStatement.WithStatement(SyntaxFactory.Block((StatementSyntax)statement));
        }

        public SyntaxNode WithStatementsOf(SyntaxNode ifOrElseIf, SyntaxNode otherIfOrElseIf)
        {
            var ifStatement = (IfStatementSyntax)ifOrElseIf;
            var otherIfStatement = (IfStatementSyntax)otherIfOrElseIf;
            return ifStatement.WithStatement(otherIfStatement.Statement);
        }

        public SyntaxNode WithElseIfAndElseClausesOf(SyntaxNode ifStatement, SyntaxNode otherIfStatement)
        {
            return ((IfStatementSyntax)ifStatement).WithElse(((IfStatementSyntax)otherIfStatement).Else);
        }

        public SyntaxNode ToIfStatement(SyntaxNode ifOrElseIf)
            => ifOrElseIf;

        public SyntaxNode ToElseIfClause(SyntaxNode ifOrElseIf)
            => ((IfStatementSyntax)ifOrElseIf).WithElse(null);

        public void InsertElseIfClause(SyntaxEditor editor, SyntaxNode afterIfOrElseIf, SyntaxNode elseIfClause)
        {
            editor.ReplaceNode(afterIfOrElseIf, (currentNode, _) =>
            {
                var ifStatement = (IfStatementSyntax)currentNode;
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

                return newIfStatement;
            });
        }

        public void RemoveElseIfClause(SyntaxEditor editor, SyntaxNode elseIfClause)
        {
            editor.ReplaceNode(elseIfClause.Parent.Parent, (currentNode, _) =>
            {
                var parentIfStatement = (IfStatementSyntax)currentNode;
                var elseClause = parentIfStatement.Else;
                var elseIfStatement = (IfStatementSyntax)elseClause.Statement;
                return parentIfStatement.WithElse(elseIfStatement.Else);
            });
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
