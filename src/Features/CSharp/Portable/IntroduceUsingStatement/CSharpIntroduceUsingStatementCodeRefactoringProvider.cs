// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
        [ImportingConstructor]
        public CSharpIntroduceUsingStatementCodeRefactoringProvider()
        {
        }

        protected override string CodeActionTitle => CSharpFeaturesResources.Introduce_using_statement;

        protected override bool CanRefactorToContainBlockStatements(SyntaxNode parent)
        {
            return parent is BlockSyntax || parent is SwitchSectionSyntax || parent.IsEmbeddedStatementOwner();
        }

        protected override SyntaxList<StatementSyntax> GetStatements(SyntaxNode parentOfStatementsToSurround)
        {
            return
                parentOfStatementsToSurround is BlockSyntax block ? block.Statements :
                parentOfStatementsToSurround is SwitchSectionSyntax switchSection ? switchSection.Statements :
                throw ExceptionUtilities.UnexpectedValue(parentOfStatementsToSurround);
        }

        protected override SyntaxNode WithStatements(SyntaxNode parentOfStatementsToSurround, SyntaxList<StatementSyntax> statements)
        {
            return
                parentOfStatementsToSurround is BlockSyntax block ? block.WithStatements(statements) as SyntaxNode :
                parentOfStatementsToSurround is SwitchSectionSyntax switchSection ? switchSection.WithStatements(statements) :
                throw ExceptionUtilities.UnexpectedValue(parentOfStatementsToSurround);
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
