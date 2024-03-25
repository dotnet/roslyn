// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InvertIf;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvertIf;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
internal sealed class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider<
    SyntaxKind, StatementSyntax, IfStatementSyntax, StatementSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpInvertIfCodeRefactoringProvider()
    {
    }

    protected override string GetTitle()
        => CSharpFeaturesResources.Invert_if;

    protected override bool IsElseless(IfStatementSyntax ifNode)
        => ifNode.Else == null;

    protected override bool CanInvert(IfStatementSyntax ifNode)
        => ifNode?.Parent is (kind: SyntaxKind.Block or SyntaxKind.SwitchSection);

    protected override SyntaxNode GetCondition(IfStatementSyntax ifNode)
        => ifNode.Condition;

    protected override StatementRange GetIfBodyStatementRange(IfStatementSyntax ifNode)
        => new(ifNode.Statement, ifNode.Statement);

    protected override bool IsStatementContainer(SyntaxNode node)
        => node.Kind() is SyntaxKind.Block or SyntaxKind.SwitchSection;

    protected override bool IsNoOpSyntaxNode(SyntaxNode node)
        => node.Kind() is SyntaxKind.Block or SyntaxKind.EmptyStatement;

    protected override bool IsExecutableStatement(SyntaxNode node)
        => node is StatementSyntax;

    protected override StatementSyntax? GetNextStatement(StatementSyntax node)
        => node.GetNextStatement();

    protected override StatementSyntax GetIfBody(IfStatementSyntax ifNode)
        => ifNode.Statement;

    protected override StatementSyntax GetEmptyEmbeddedStatement()
        => SyntaxFactory.Block();

    protected override StatementSyntax GetElseBody(IfStatementSyntax ifNode)
        => ifNode.Else?.Statement ?? throw new InvalidOperationException();

    protected override bool CanControlFlowOut(SyntaxNode node)
    {
        switch (node)
        {
            case SwitchSectionSyntax:
            case LocalFunctionStatementSyntax:
            case AccessorDeclarationSyntax:
            case MemberDeclarationSyntax:
            case AnonymousFunctionExpressionSyntax:
            case CommonForEachStatementSyntax:
            case DoStatementSyntax:
            case WhileStatementSyntax:
            case ForStatementSyntax:
                return false;
        }

        return true;
    }

    protected override SyntaxList<StatementSyntax> GetStatements(SyntaxNode node)
        => node switch
        {
            BlockSyntax n => n.Statements,
            SwitchSectionSyntax n => n.Statements,
            _ => throw ExceptionUtilities.UnexpectedValue(node),
        };

    protected override SyntaxKind? GetJumpStatementKind(SyntaxNode node)
        => node switch
        {
            SwitchSectionSyntax
                => SyntaxKind.BreakStatement,
            LocalFunctionStatementSyntax or AccessorDeclarationSyntax or MemberDeclarationSyntax
                => node.ContainsYield() ? SyntaxKind.YieldBreakStatement : SyntaxKind.ReturnStatement,
            AnonymousFunctionExpressionSyntax
                => SyntaxKind.ReturnStatement,
            CommonForEachStatementSyntax or DoStatementSyntax or WhileStatementSyntax or ForStatementSyntax
                => SyntaxKind.ContinueStatement,
            _ => null,
        };

    protected override StatementSyntax GetJumpStatement(SyntaxKind kind)
        => kind switch
        {
            SyntaxKind.ContinueStatement => SyntaxFactory.ContinueStatement(),
            SyntaxKind.BreakStatement => SyntaxFactory.BreakStatement(),
            SyntaxKind.ReturnStatement => SyntaxFactory.ReturnStatement(),
            SyntaxKind.YieldBreakStatement => SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };

    protected override StatementSyntax AsEmbeddedStatement(IEnumerable<StatementSyntax> statements, StatementSyntax original)
    {
        var statementArray = statements.ToArray();
        if (statementArray.Length > 0)
        {
            statementArray[0] = statementArray[0].GetNodeWithoutLeadingBlankLines();
        }

        return original is BlockSyntax block
            ? block.WithStatements([.. statementArray])
            : statementArray.Length == 1
                ? statementArray[0]
                : SyntaxFactory.Block(statementArray);
    }

    protected override IfStatementSyntax UpdateIf(
        SourceText sourceText,
        IfStatementSyntax ifNode,
        SyntaxNode condition,
        StatementSyntax trueStatement,
        StatementSyntax? falseStatementOpt = null)
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

        if (ShouldKeepFalse(ifNode, falseStatementOpt))
        {
            var elseClause = updatedIf.Else != null
                ? updatedIf.Else.WithStatement(falseStatementOpt)
                : SyntaxFactory.ElseClause(falseStatementOpt);

            updatedIf = updatedIf.WithElse(elseClause);
        }
        else
        {
            updatedIf = updatedIf.WithElse(null);
        }

        // If this is multiline, format things after we swap around the if/else.  Because 
        // of all the different types of rewriting, we may need indentation fixed up and
        // whatnot.  Don't do this with single-line because we want to ensure as closely
        // as possible that we've kept things on that single line.
        return isSingleLine
            ? updatedIf
            : updatedIf.WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static bool ShouldKeepFalse(IfStatementSyntax originalIfStatement, [NotNullWhen(returnValue: true)] StatementSyntax? falseStatement)
    {
        // The original false statement doesn't exist at all
        // then no need to consider keeping it around
        if (falseStatement is null)
        {
            return false;
        }

        if (falseStatement is BlockSyntax falseBlock)
        {
            // Block false syntax with some statements included.
            // If there are no statements, it's an empty
            // block and we can get rid of it.
            if (falseBlock.Statements.Any())
            {
                return true;
            }

            // If the stateements for the else don't pass, we still need to check
            // if there are comments from the original if that should be included.
            // If so, pass them along to be copied to the new else block
            return originalIfStatement.Statement is BlockSyntax block
                && BlockHasComment(block);
        }

        // The statement is not expected to have children, so we know it's fine
        // to consider this something that needs to be included. Such as 
        // a return statement, or other similar things for single line if/else.
        return true;

        static bool BlockHasComment(BlockSyntax block)
        {
            return block.CloseBraceToken.LeadingTrivia.Any(HasCommentTrivia)
                || block.OpenBraceToken.TrailingTrivia.Any(HasCommentTrivia);
        }

        static bool HasCommentTrivia(SyntaxTrivia trivia)
        {
            return trivia.Kind() is SyntaxKind.MultiLineCommentTrivia or SyntaxKind.SingleLineCommentTrivia;
        }
    }

    protected override SyntaxNode WithStatements(SyntaxNode node, IEnumerable<StatementSyntax> statements)
        => node switch
        {
            BlockSyntax n => n.WithStatements([.. statements]),
            SwitchSectionSyntax n => n.WithStatements([.. statements]),
            _ => throw ExceptionUtilities.UnexpectedValue(node),
        };

    protected override IEnumerable<StatementSyntax> UnwrapBlock(StatementSyntax ifBody)
    {
        return ifBody is BlockSyntax block
            ? block.Statements
            : [ifBody];
    }

    protected override bool IsSingleStatementStatementRange(StatementRange statementRange)
    {
        if (statementRange.IsEmpty)
        {
            return false;
        }

        if (statementRange.FirstStatement != statementRange.LastStatement)
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
