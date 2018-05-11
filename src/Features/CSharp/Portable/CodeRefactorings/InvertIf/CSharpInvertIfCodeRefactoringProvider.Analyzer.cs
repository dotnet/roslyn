// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    internal sealed partial class CSharpInvertIfCodeRefactoringProvider
    {
        private sealed class Analyzer : Analyzer<IfStatementSyntax>
        {
            protected override string GetTitle()
            {
                return CSharpFeaturesResources.Invert_if;
            }

            protected override SyntaxNode GetRootWithInvertIfStatement(
                Document document,
                SemanticModel semanticModel,
                IfStatementSyntax ifStatement,
                InvertIfStyle invertIfStyle,
                int? generatedJumpStatementRawKindOpt,
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
                            switch ((SyntaxKind)generatedJumpStatementRawKindOpt)
                            {
                                case SyntaxKind.ContinueStatement:
                                    return SyntaxFactory.ContinueStatement();
                                case SyntaxKind.BreakStatement:
                                    return SyntaxFactory.BreakStatement();
                                case SyntaxKind.ReturnStatement:
                                    return SyntaxFactory.ReturnStatement();
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(generatedJumpStatementRawKindOpt);
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

            protected override bool? IsElselessIfStatement(IfStatementSyntax ifStatement)
            {
                switch (ifStatement.Parent?.Kind())
                {
                    case SyntaxKind.Block:
                    case SyntaxKind.SwitchSection:
                        return ifStatement.Else == null;
                    default:
                        return null;
                }
            }

            private static void AnalyzeStatements(
               in SyntaxList<StatementSyntax> statements,
               SemanticModel semanticModel,
               StatementSyntax innerStatement,
               bool isOuterBlock,
               ref bool subsequenceEndPointIsReachable,
               ref SyntaxNode subsequenceSingleExitPointOpt,
               ref int subsequenceStatementCount,
               ref bool subsequenceIsInSameBlock)
            {
                var nextIndex = statements.IndexOf(innerStatement) + 1;

                Debug.Assert(subsequenceSingleExitPointOpt == null);
                Debug.Assert(nextIndex > 0);

                var currentStatementCount = GetStatementCount(statements, nextIndex);
                subsequenceStatementCount += currentStatementCount;

                if (nextIndex >= statements.Count || subsequenceStatementCount == 0)
                {
                    subsequenceSingleExitPointOpt = null;
                    subsequenceEndPointIsReachable = true;
                }
                else
                {
                    var controlFlow = semanticModel.AnalyzeControlFlow(statements[nextIndex], statements.Last());
                    if (controlFlow.EndPointIsReachable)
                    {
                        subsequenceSingleExitPointOpt = null;
                        subsequenceEndPointIsReachable = true;
                    }
                    else
                    {
                        var exitPoints = controlFlow.ExitPoints;
                        subsequenceSingleExitPointOpt = exitPoints.Length == 1 ? exitPoints[0] : null;
                        subsequenceEndPointIsReachable = false;
                    }
                }

                if (currentStatementCount != 0 && isOuterBlock)
                {
                    subsequenceIsInSameBlock = false;
                }
            }

            protected override void AnalyzeSubsequence(
                SemanticModel semanticModel,
                IfStatementSyntax ifStatement,
                out int subsequenceStatementCount,
                out bool subsequenceEndPontIsReachable,
                out bool subsequenceIsInSameBlock,
                out SyntaxNode subsequenceSingleExitPointOpt,
                out int? jumpStatementRawKindOpt)
            {
                subsequenceStatementCount = 0;
                subsequenceEndPontIsReachable = true;
                subsequenceSingleExitPointOpt = null;
                subsequenceIsInSameBlock = true;

                var innerStatement = (StatementSyntax)ifStatement;
                var isOuterBlock = false;

                foreach (var node in ifStatement.Ancestors())
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.Block:
                            if (subsequenceEndPontIsReachable)
                            {
                                AnalyzeStatements(((BlockSyntax)node).Statements,
                                    semanticModel, innerStatement, isOuterBlock,
                                    ref subsequenceEndPontIsReachable, 
                                    ref subsequenceSingleExitPointOpt,
                                    ref subsequenceStatementCount,
                                    ref subsequenceIsInSameBlock);
                            }

                            break;

                        case SyntaxKind.SwitchSection:
                            if (subsequenceEndPontIsReachable)
                            {
                                AnalyzeStatements(((SwitchSectionSyntax)node).Statements,
                                    semanticModel, innerStatement, isOuterBlock,
                                    ref subsequenceEndPontIsReachable,
                                    ref subsequenceSingleExitPointOpt,
                                    ref subsequenceStatementCount,
                                    ref subsequenceIsInSameBlock);
                            }

                            jumpStatementRawKindOpt = (int)SyntaxKind.BreakStatement;
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
                            jumpStatementRawKindOpt = (int)SyntaxKind.ReturnStatement;
                            return;

                        case SyntaxKind.DoStatement:
                        case SyntaxKind.WhileStatement:
                        case SyntaxKind.ForStatement:
                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.ForEachVariableStatement:
                            jumpStatementRawKindOpt = (int)SyntaxKind.ContinueStatement;
                            return;
                    }

                    isOuterBlock = true;

                    if (node is StatementSyntax statement)
                    {
                        innerStatement = statement;
                    }
                }

                jumpStatementRawKindOpt = null;
            }

            protected override ControlFlowAnalysis AnalyzeIfBodyControlFlow(SemanticModel semanticModel, IfStatementSyntax ifStatement)
            {
                return semanticModel.AnalyzeControlFlow(ifStatement.Statement);
            }

            protected override int GetIfBodyStatementCount(IfStatementSyntax ifStatement)
            {
                return GetStatementCount(ifStatement.Statement);
            }

            private static int GetStatementCount(StatementSyntax statement)
            {
                switch (statement.Kind())
                {
                    case SyntaxKind.EmptyStatement:
                        return 0;
                    case SyntaxKind.Block:
                        return GetStatementCount(((BlockSyntax)statement).Statements);
                    default:
                        return 1;
                }
            }

            private static int GetStatementCount(in SyntaxList<StatementSyntax> statements, int startIndex = 0)
            {
                bool sawOneStatement = false;
                for (int i = startIndex, n = statements.Count; i < n; ++i)
                {
                    var statement = statements[i];
                    switch (statement.Kind())
                    {
                        case SyntaxKind.EmptyStatement:
                            continue;
                        case SyntaxKind.Block:
                            switch (GetStatementCount(((BlockSyntax)statement).Statements))
                            {
                                case 0:
                                    continue;
                                case 1:
                                    break;
                                default:
                                    return 2;
                            }

                            break;
                    }

                    if (sawOneStatement)
                    {
                        return 2;
                    }

                    sawOneStatement = true;
                }

                return sawOneStatement ? 1 : 0;
            }
        }
    }
}
