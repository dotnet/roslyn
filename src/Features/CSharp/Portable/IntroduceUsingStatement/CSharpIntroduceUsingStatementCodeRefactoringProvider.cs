// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceUsingStatement;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement), Shared]
    internal sealed class CSharpIntroduceUsingStatementCodeRefactoringProvider
        : AbstractIntroduceUsingStatementCodeRefactoringProvider<StatementSyntax, LocalDeclarationStatementSyntax>
    {
        protected override string CodeActionTitle => CSharpFeaturesResources.Introduce_using_statement;

        protected override bool IsBlockLike(SyntaxNode node)
        {
            return node is BlockSyntax || node is SwitchSectionSyntax;
        }

        protected override SyntaxList<StatementSyntax> GetStatements(SyntaxNode blockLike)
        {
            return
                blockLike is BlockSyntax block ? block.Statements :
                blockLike is SwitchSectionSyntax switchSection ? switchSection.Statements :
                throw ExceptionUtilities.UnexpectedValue(blockLike);
        }

        protected override SyntaxNode WithStatements(SyntaxNode blockLike, SyntaxList<StatementSyntax> statements)
        {
            return
                blockLike is BlockSyntax block ? block.WithStatements(statements) as SyntaxNode :
                blockLike is SwitchSectionSyntax switchSection ? switchSection.WithStatements(statements) :
                throw ExceptionUtilities.UnexpectedValue(blockLike);
        }

        protected override StatementSyntax CreateUsingStatement(LocalDeclarationStatementSyntax declarationStatement, SyntaxTriviaList sameLineTrivia, SyntaxList<StatementSyntax> statementsToSurround)
        {
            var usingStatement = SyntaxFactory.UsingStatement(
                declaration: declarationStatement.Declaration.WithoutTrivia(),
                expression: null, // Declaration already has equals token and expression
                statement: SyntaxFactory.Block(statementsToSurround));

            return usingStatement
                .WithCloseParenToken(usingStatement.CloseParenToken
                    .WithTrailingTrivia(sameLineTrivia));
        }
    }
}
