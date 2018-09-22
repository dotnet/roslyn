// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceUsingStatement;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement), Shared]
    internal sealed class CSharpIntroduceUsingStatementCodeRefactoringProvider
        : AbstractIntroduceUsingStatementCodeRefactoringProvider<StatementSyntax, LocalDeclarationStatementSyntax, BlockSyntax>
    {
        protected override string CodeActionTitle => CSharpFeaturesResources.Introduce_using_statement;

        protected override SyntaxList<StatementSyntax> GetStatements(BlockSyntax blockSyntax)
        {
            return blockSyntax.Statements;
        }

        protected override BlockSyntax WithStatements(BlockSyntax blockSyntax, SyntaxList<StatementSyntax> statements)
        {
            return blockSyntax.WithStatements(statements);
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
