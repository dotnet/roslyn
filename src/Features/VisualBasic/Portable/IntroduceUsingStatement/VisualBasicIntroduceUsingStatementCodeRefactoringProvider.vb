' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceUsingStatement
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceUsingStatement

    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement), [Shared]>
    Friend NotInheritable Class VisualBasicIntroduceUsingStatementCodeRefactoringProvider
        Inherits AbstractIntroduceUsingStatementCodeRefactoringProvider(Of StatementSyntax, LocalDeclarationStatementSyntax, TryBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property CodeActionTitle As String = VBFeaturesResources.Introduce_Using_statement

        Protected Overrides Function HasCatchBlocks(tryStatement As TryBlockSyntax) As Boolean
            Return tryStatement.CatchBlocks.Count > 0
        End Function

        Protected Overrides Function GetTryFinallyStatements(tryStatement As TryBlockSyntax) As (SyntaxList(Of StatementSyntax), SyntaxList(Of StatementSyntax))
            Return (tryStatement.Statements, If(tryStatement.FinallyBlock IsNot Nothing, tryStatement.FinallyBlock.Statements, Nothing))
        End Function

        Protected Overrides Function CanRefactorToContainBlockStatements(parent As SyntaxNode) As Boolean
            ' We don’t care enough about declarations in single-line If, Else, lambdas, etc, to support them.
            Return parent.IsMultiLineExecutableBlock()
        End Function

        Protected Overrides Function GetSurroundingStatements(declarationStatement As LocalDeclarationStatementSyntax) As SyntaxList(Of StatementSyntax)
            Return declarationStatement.GetRequiredParent().GetExecutableBlockStatements()
        End Function

        Protected Overrides Function WithStatements(parentOfStatementsToSurround As SyntaxNode, statements As SyntaxList(Of StatementSyntax)) As SyntaxNode
            Return parentOfStatementsToSurround.ReplaceStatements(statements)
        End Function

        Protected Overrides Function CreateUsingStatement(
                declarationStatement As LocalDeclarationStatementSyntax,
                statementsToSurround As SyntaxList(Of StatementSyntax)) As StatementSyntax
            Dim usingStatement = SyntaxFactory.UsingStatement(
                expression:=Nothing,
                variables:=declarationStatement.Declarators).WithTriviaFrom(declarationStatement)
            Return SyntaxFactory.UsingBlock(usingStatement, statementsToSurround)
        End Function

        Protected Overrides Function TryCreateUsingLocalDeclaration(options As ParseOptions, declarationStatement As LocalDeclarationStatementSyntax, ByRef usingDeclarationStatement As LocalDeclarationStatementSyntax) As Boolean
            Return False
        End Function
    End Class
End Namespace
