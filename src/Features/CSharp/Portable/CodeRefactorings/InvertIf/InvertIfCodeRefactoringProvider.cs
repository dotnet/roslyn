// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal partial class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider
    {
        protected override SyntaxNode GetIfStatement(TextSpan textSpan, SyntaxToken token, CancellationToken cancellationToken)
        {
            var ifStatement = token.GetAncestor<IfStatementSyntax>();
            if (ifStatement == null || ifStatement.Else == null)
            {
                return null;
            }

            var span = TextSpan.FromBounds(ifStatement.IfKeyword.Span.Start, ifStatement.CloseParenToken.Span.End);
            if (!span.IntersectsWith(textSpan))
            {
                return null;
            }

            return ifStatement;
        }

        protected override SyntaxNode GetRootWithInvertIfStatement(
            Document document,
            SemanticModel model,
            SyntaxNode ifStatementSyntax,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = GetSyntaxFactsService();

            var oldIfStatement = (IfStatementSyntax)ifStatementSyntax;

            // For single line statement, we swap the TrailingTrivia to preserve the single line
            StatementSyntax newIfNodeStatement = null;
            ElseClauseSyntax newElseStatement = null;

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

            oldIfStatement = oldIfStatement.WithCondition((ExpressionSyntax)(Negate(oldIfStatement.Condition, generator, syntaxFacts, model, cancellationToken)))
                .WithStatement(newIfStatment)
                .WithElse(newElseStatement);

            if (hasNewLineAfterClosingBrace)
            {
                oldIfStatement = oldIfStatement.WithAdditionalAnnotations(Formatter.Annotation);
            }

            // get new root
            return model.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(ifStatementSyntax, oldIfStatement);
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override string GetTitle()
            => CSharpFeaturesResources.Invert_if;
    }
}
