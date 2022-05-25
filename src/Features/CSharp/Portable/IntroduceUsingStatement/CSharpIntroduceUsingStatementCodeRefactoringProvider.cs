// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceUsingStatementCodeRefactoringProvider()
        {
        }

        protected override string CodeActionTitle => CSharpFeaturesResources.Introduce_using_statement;

        protected override bool CanRefactorToContainBlockStatements(SyntaxNode parent)
            => parent is BlockSyntax || parent is SwitchSectionSyntax || parent.IsEmbeddedStatementOwner();

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

        protected override StatementSyntax CreateUsingStatement(LocalDeclarationStatementSyntax declarationStatement, SyntaxList<StatementSyntax> statementsToSurround)
            => SyntaxFactory.UsingStatement(
                SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithLeadingTrivia(declarationStatement.GetLeadingTrivia()),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                declaration: declarationStatement.Declaration.WithoutTrivia(),
                expression: null, // Declaration already has equals token and expression
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(declarationStatement.GetTrailingTrivia()),
                statement: SyntaxFactory.Block(statementsToSurround));
    }
}
