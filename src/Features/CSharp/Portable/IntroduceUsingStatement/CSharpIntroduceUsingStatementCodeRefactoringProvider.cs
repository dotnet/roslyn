// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceUsingStatement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement;

[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement), Shared]
internal sealed class CSharpIntroduceUsingStatementCodeRefactoringProvider
    : AbstractIntroduceUsingStatementCodeRefactoringProvider<StatementSyntax, LocalDeclarationStatementSyntax, TryStatementSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpIntroduceUsingStatementCodeRefactoringProvider()
    {
    }

    protected override string CodeActionTitle => CSharpFeaturesResources.Introduce_using_statement;

    protected override bool HasCatchBlocks(TryStatementSyntax tryStatement)
        => tryStatement.Catches.Count > 0;

    protected override (SyntaxList<StatementSyntax> tryStatements, SyntaxList<StatementSyntax> finallyStatements) GetTryFinallyStatements(TryStatementSyntax tryStatement)
        => (tryStatement.Block.Statements, tryStatement.Finally?.Block.Statements ?? default);

    protected override bool CanRefactorToContainBlockStatements(SyntaxNode parent)
        => parent is BlockSyntax || parent is SwitchSectionSyntax || parent.IsEmbeddedStatementOwner();

    protected override SyntaxList<StatementSyntax> GetSurroundingStatements(LocalDeclarationStatementSyntax declarationStatement)
        => declarationStatement.GetRequiredParent() switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax switchSection => switchSection.Statements,
            _ => [declarationStatement],
        };

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

    protected override bool TryCreateUsingLocalDeclaration(
        ParseOptions options,
        LocalDeclarationStatementSyntax declarationStatement,
        [NotNullWhen(true)] out LocalDeclarationStatementSyntax? usingDeclarationStatement)
    {
        usingDeclarationStatement = null;

        // using-declarations are not allowed in switch sections (due to craziness with how switch section scoping works).
        if (declarationStatement.Parent is SwitchSectionSyntax ||
            options.LanguageVersion() < LanguageVersion.CSharp8)
        {
            return false;
        }

        usingDeclarationStatement = declarationStatement
            .WithoutLeadingTrivia()
            .WithUsingKeyword(SyntaxFactory.Token(declarationStatement.GetLeadingTrivia(), SyntaxKind.UsingKeyword, [SyntaxFactory.Space]));
        return true;
    }
}
