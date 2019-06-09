' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceUsingStatement
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceUsingStatement

    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement), [Shared]>
    Friend NotInheritable Class VisualBasicIntroduceUsingStatementCodeRefactoringProvider
        Inherits AbstractIntroduceUsingStatementCodeRefactoringProvider(Of StatementSyntax, LocalDeclarationStatementSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property CodeActionTitle As String = VBFeaturesResources.Introduce_Using_statement

        Protected Overrides Function CanRefactorToContainBlockStatements(parent As SyntaxNode) As Boolean
            ' We don’t care enough about declarations in single-line If, Else, lambdas, etc, to support them.
            Return parent.IsMultiLineExecutableBlock()
        End Function

        Protected Overrides Function GetStatements(parentOfStatementsToSurround As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Return parentOfStatementsToSurround.GetExecutableBlockStatements()
        End Function

        Protected Overrides Function WithStatements(parentOfStatementsToSurround As SyntaxNode, statements As SyntaxList(Of StatementSyntax)) As SyntaxNode
            Return parentOfStatementsToSurround.ReplaceStatements(statements)
        End Function

        Protected Overrides Function CreateUsingStatement(declarationStatement As LocalDeclarationStatementSyntax, sameLineTrivia As SyntaxTriviaList, statementsToSurround As SyntaxList(Of StatementSyntax)) As StatementSyntax
            Dim usingStatement =
                SyntaxFactory.UsingStatement(
                    expression:=Nothing,
                    variables:=declarationStatement.Declarators)

            If sameLineTrivia.Any Then
                usingStatement = usingStatement.WithTrailingTrivia(sameLineTrivia)
            End If

            Return SyntaxFactory.UsingBlock(usingStatement, statementsToSurround)
        End Function
    End Class
End Namespace
