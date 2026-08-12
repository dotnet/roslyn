// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpToVisualBasicConverter.Cleanup
{
    internal class MissingCurlyCleanup : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree syntaxTree;

        public MissingCurlyCleanup(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            node = (IfStatementSyntax)base.VisitIfStatement(node);
            if (node.Statement.IsKind(SyntaxKind.Block))
            {
                return node;
            }

            BlockSyntax block = SyntaxFactory.Block(statements: SyntaxFactory.SingletonList(node.Statement));
            return SyntaxFactory.IfStatement(
                node.IfKeyword,
                node.OpenParenToken,
                node.Condition,
                node.CloseParenToken,
                block,
                node.Else);
        }

        public override SyntaxNode VisitElseClause(ElseClauseSyntax node)
        {
            node = (ElseClauseSyntax)base.VisitElseClause(node);
            if (node.Statement.IsKind(SyntaxKind.Block) || node.Statement.IsKind(SyntaxKind.IfStatement))
            {
                return node;
            }

            BlockSyntax block = SyntaxFactory.Block(statements: SyntaxFactory.SingletonList(node.Statement));
            return SyntaxFactory.ElseClause(
                node.ElseKeyword,
                block);
        }
    }
}
