// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
                Document document,
                SemanticModel semanticModel,
                IfStatementSyntax ifStatement,
                InvertIfStyle invertIfStyle,
                SyntaxNode subsequenceSingleExitPointOpt,
                CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = CSharpSyntaxFactsService.Instance;

            var ifNode = ifStatement;

            var negatedCondition = (ExpressionSyntax)Negator.Negate(ifStatement.Condition, generator, syntaxFacts, semanticModel, cancellationToken);
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

            switch (invertIfStyle)
            {
                case InvertIfStyle.Normal:
                    {
                        // For single line statement, we swap the TrailingTrivia to preserve the single line
                        StatementSyntax newIfNodeStatement = null;
                        ElseClauseSyntax newElseStatement = null;
                        IfStatementSyntax oldIfStatement = ifStatement;

                        var hasNewLineAfterClosingBrace = oldIfStatement.Statement.GetTrailingTrivia().Any(trivia => trivia.Kind() == SyntaxKind.EndOfLineTrivia);
                        if (hasNewLineAfterClosingBrace)
                        {
                            newIfNodeStatement = oldIfStatement.Else.Statement.Kind() != SyntaxKind.Block
                                ? SyntaxFactory.Block(oldIfStatement.Else.Statement)
                                : oldIfStatement.Else.Statement;
                            newElseStatement = oldIfStatement.Else.WithStatement(oldIfStatement.Statement);
                        }
                        else
                        {
                            var elseTrailingTrivia = oldIfStatement.Else.GetTrailingTrivia();
                            var ifTrailingTrivia = oldIfStatement.Statement.GetTrailingTrivia();
                            newIfNodeStatement = oldIfStatement.Else.Statement.WithTrailingTrivia(ifTrailingTrivia);
                            newElseStatement = oldIfStatement.Else.WithStatement(oldIfStatement.Statement).WithTrailingTrivia(elseTrailingTrivia);
                        }

                        var newIfStatment = oldIfStatement.Else.Statement.Kind() == SyntaxKind.IfStatement && newIfNodeStatement.Kind() != SyntaxKind.Block
                            ? SyntaxFactory.Block(newIfNodeStatement)
                            : newIfNodeStatement;

                        oldIfStatement = oldIfStatement.WithCondition(negatedCondition)
                            .WithStatement(newIfStatment)
                            .WithElse(newElseStatement);

                        if (hasNewLineAfterClosingBrace)
                        {
                            oldIfStatement = oldIfStatement.WithAdditionalAnnotations(Formatter.Annotation);
                        }

                        return root.ReplaceNode(ifNode, oldIfStatement);
                    }

                case InvertIfStyle.MoveIfBodyToElseClause:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition)
                            .WithStatement(SyntaxFactory.Block())
                            .WithElse(SyntaxFactory.ElseClause(ifNode.Statement.WithoutLeadingTrivia()));

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.WithNegatedCondition:
                    {
                        var updatedIf = ifNode
                            .WithCondition(negatedCondition);

                        return root.ReplaceNode(ifNode, updatedIf);
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

                        return root.ReplaceNode(currentParent, updatedParent);
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

                        return root.ReplaceNode(currentParent, updatedParent);
                    }

                    StatementSyntax GetNearmostAncestorJumpStatement()
                    {
                        switch ((SyntaxKind)GetNearmostParentJumpStatementRawKind(ifStatement))
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

                case InvertIfStyle.WithSubsequenceExitPoint:
                    {
                        var newIfBody = (StatementSyntax)subsequenceSingleExitPointOpt;
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

                        return root.ReplaceNode(currentParent, updatedParent);
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

                        return root.ReplaceNode(currentParent, updatedParent);
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

        protected override TextSpan GetHeaderSpan(IfStatementSyntax ifStatement)
        {
            return TextSpan.FromBounds(
                ifStatement.IfKeyword.SpanStart,
                ifStatement.CloseParenToken.Span.End);
        }

        protected override bool IsElselessIfStatement(IfStatementSyntax ifStatement)
        {
            return ifStatement.Else == null;
        }

        protected override bool CanInvert(IfStatementSyntax ifStatement)
        {
            return ifStatement.IsParentKind(SyntaxKind.Block, SyntaxKind.SwitchSection);
        }

        protected override int GetNearmostParentJumpStatementRawKind(IfStatementSyntax ifStatement)
        {
            foreach (var node in ifStatement.Ancestors())
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
            }

            return -1;
        }

        protected override (SyntaxNode first, SyntaxNode last) GetIfBodyStatementRange(IfStatementSyntax ifStatement)
        {
            var statement = ifStatement.Statement;
            return (statement, statement);
        }

        protected override IEnumerable<(SyntaxNode first, SyntaxNode last)> GetSubsequentStatementRange(IfStatementSyntax ifStatement)
        {
            StatementSyntax innerStatement = ifStatement;
            foreach (var node in ifStatement.Ancestors())
            {
                var nextStatement = innerStatement.GetNextStatement();
                switch (node.Kind())
                {
                    case SyntaxKind.Block:
                        if (nextStatement != null)
                        {
                            yield return (nextStatement, ((BlockSyntax)node).Statements.Last());
                        }

                        break;
                    case SyntaxKind.SwitchSection:
                        if (nextStatement != null)
                        {
                            yield return (nextStatement, ((SwitchSectionSyntax)node).Statements.Last());
                        }

                        yield break;
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
                        yield break;
                }

                if (node is StatementSyntax statement)
                {
                    innerStatement = statement;
                }
            }
        }

        protected override bool IsEmptyStatementRange((SyntaxNode first, SyntaxNode last) range)
        {
            return range.first == range.last && range.first.IsKind(SyntaxKind.EmptyStatement);
        }
    }
}

