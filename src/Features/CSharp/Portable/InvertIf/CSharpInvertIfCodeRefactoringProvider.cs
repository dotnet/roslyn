// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InvertIf;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider<IfStatementSyntax, StatementSyntax, StatementSyntax>
    {
        [ImportingConstructor]
        public CSharpInvertIfCodeRefactoringProvider()
        {
        }

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

        protected override bool IsExecutableStatement(SyntaxNode node)
            => node is StatementSyntax;

        protected override StatementSyntax GetNextStatement(StatementSyntax node)
            => node.GetNextStatement();

        protected override StatementSyntax GetIfBody(IfStatementSyntax ifNode)
            => ifNode.Statement;

        protected override StatementSyntax GetEmptyEmbeddedStatement()
            => SyntaxFactory.Block();

        protected override StatementSyntax GetElseBody(IfStatementSyntax ifNode)
            => ifNode.Else.Statement;

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

        protected override SyntaxList<StatementSyntax> GetStatements(SyntaxNode node)
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

        protected override StatementSyntax GetJumpStatement(int rawKind)
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

        protected override StatementSyntax AsEmbeddedStatement(IEnumerable<StatementSyntax> statements, StatementSyntax original)
        {
            var statementArray = statements.ToArray();
            if (statementArray.Length > 0)
            {
                statementArray[0] = statementArray[0].GetNodeWithoutLeadingBlankLines();
            }

            return original is BlockSyntax block
                ? block.WithStatements(SyntaxFactory.List(statementArray))
                : statementArray.Length == 1
                    ? statementArray[0]
                    : SyntaxFactory.Block(statementArray);
        }

        protected override IfStatementSyntax UpdateIf(
            SourceText sourceText,
            IfStatementSyntax ifNode,
            SyntaxNode condition,
            StatementSyntax trueStatement,
            StatementSyntax falseStatementOpt = null)
        {
            var isSingleLine = sourceText.AreOnSameLine(ifNode.GetFirstToken(), ifNode.GetLastToken());
            if (isSingleLine && falseStatementOpt != null)
            {
                // If statement is on a single line, and we're swapping the true/false parts.
                // In that case, try to swap the trailing trivia between the true/false parts.
                // That way the trailing comments/newlines at the end of hte 'if' stay there,
                // and the spaces after the true-part stay where they are.

                (trueStatement, falseStatementOpt) =
                    (trueStatement.WithTrailingTrivia(falseStatementOpt.GetTrailingTrivia()),
                     falseStatementOpt.WithTrailingTrivia(trueStatement.GetTrailingTrivia()));
            }

            var updatedIf = ifNode
                .WithCondition((ExpressionSyntax)condition)
                .WithStatement(trueStatement is IfStatementSyntax
                    ? SyntaxFactory.Block(trueStatement)
                    : trueStatement);

            if (falseStatementOpt != null)
            {
                var elseClause = updatedIf.Else != null
                    ? updatedIf.Else.WithStatement(falseStatementOpt)
                    : SyntaxFactory.ElseClause(falseStatementOpt);

                updatedIf = updatedIf.WithElse(elseClause);
            }

            // If this is multiline, format things after we swap around the if/else.  Because 
            // of all the different types of rewriting, we may need indentation fixed up and
            // whatnot.  Don't do this with single-line because we want to ensure as closely
            // as possible that we've kept things on that single line.
            return isSingleLine
                ? updatedIf
                : updatedIf.WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override SyntaxNode WithStatements(SyntaxNode node, IEnumerable<StatementSyntax> statements)
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

        protected override IEnumerable<StatementSyntax> UnwrapBlock(StatementSyntax ifBody)
        {
            return ifBody is BlockSyntax block
                ? block.Statements
                : SyntaxFactory.SingletonList(ifBody);
        }

        protected override bool IsSingleStatementStatementRange(StatementRange statementRange)
        {
            if (statementRange.IsEmpty)
            {
                return false;
            }

            if ((object)statementRange.FirstStatement != (object)statementRange.LastStatement)
            {
                return false;
            }

            return IsSingleStatement(statementRange.FirstStatement);

            static bool IsSingleStatement(StatementSyntax statement)
            {
                if (statement is BlockSyntax block)
                {
                    return block.Statements.Count == 1 && IsSingleStatement(block.Statements[0]);
                }

                return true;
            }
        }
    }
}
