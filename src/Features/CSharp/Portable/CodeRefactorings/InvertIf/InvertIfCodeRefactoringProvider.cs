// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider<IfStatementSyntax, StatementSyntax>
    {
        protected override string GetTitle()
            => CSharpFeaturesResources.Invert_if;

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

        protected override StatementSyntax GetIfBody(IfStatementSyntax ifNode)
            => ifNode.Statement;

        protected override StatementSyntax GetEmptyEmbeddedStatement()
            => SyntaxFactory.Block();

        protected override StatementSyntax GetElseBody(IfStatementSyntax ifNode)
            => ifNode.Else.Statement;

        protected override TextSpan GetHeaderSpan(IfStatementSyntax ifNode)
        {
            return TextSpan.FromBounds(
                ifNode.IfKeyword.SpanStart,
                ifNode.CloseParenToken.Span.End);
        }

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

        protected override StatementSyntax AsEmbeddedStatement(
            StatementSyntax originalStatement,
            IEnumerable<SyntaxNode> newStatements)
        {
            var statements = newStatements.ToArray();
            if (statements.Length > 0)
            {
                // FIXME preserve comments
                statements[0] = statements[0].WithoutLeadingTrivia();
            }

            return originalStatement is BlockSyntax block
                ? block.WithStatements(SyntaxFactory.List(statements))
                : statements.Length == 1
                    ? (StatementSyntax)statements[0]
                    : SyntaxFactory.Block(statements.Cast<StatementSyntax>());
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

        protected override IEnumerable<SyntaxNode> UnwrapBlock(StatementSyntax ifBody)
        {
            return ifBody is BlockSyntax block
                ? block.Statements
                : SyntaxFactory.SingletonList(ifBody);
        }
    }
}
