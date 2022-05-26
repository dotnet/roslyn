' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertSingleLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of SingleLineIfStatementSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsElseless(ifNode As SingleLineIfStatementSyntax) As Boolean
            Return ifNode.ElseClause Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As SingleLineIfStatementSyntax) As Boolean
            Return TypeOf ifNode.Parent IsNot SingleLineLambdaExpressionSyntax AndAlso
                Not ifNode.Statements.Any(Function(n) n.IsKind(SyntaxKind.LocalDeclarationStatement)) AndAlso
                Not If(ifNode.ElseClause?.Statements.Any(Function(n) n.IsKind(SyntaxKind.LocalDeclarationStatement)), False)
        End Function

        Protected Overrides Function GetCondition(ifNode As SingleLineIfStatementSyntax) As SyntaxNode
            Return ifNode.Condition
        End Function

        Protected Overrides Function GetIfBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.Statements
        End Function

        Protected Overrides Function GetElseBody(ifNode As SingleLineIfStatementSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.ElseClause.Statements
        End Function

        Protected Overrides Function UpdateIf(
                sourceText As SourceText,
                ifNode As SingleLineIfStatementSyntax,
                condition As SyntaxNode,
                trueStatements As SyntaxList(Of StatementSyntax),
                Optional falseStatements As SyntaxList(Of StatementSyntax) = Nothing) As SingleLineIfStatementSyntax

            Dim isSingleLine = sourceText.AreOnSameLine(ifNode.GetFirstToken(), ifNode.GetLastToken())
            If isSingleLine AndAlso falseStatements.Count > 0 Then
                ' If statement Is on a single line, And we're swapping the true/false parts.
                ' In that case, try to swap the trailing trivia between the true/false parts.
                ' That way the trailing comments/newlines at the end of the 'if' stay there,
                ' And the spaces after the true-part stay where they are.

                Dim lastTrue = trueStatements.LastOrDefault()
                Dim lastFalse = falseStatements.LastOrDefault()

                If lastTrue IsNot Nothing AndAlso lastFalse IsNot Nothing Then
                    Dim newLastTrue = lastTrue.WithTrailingTrivia(lastFalse.GetTrailingTrivia())
                    Dim newLastFalse = lastFalse.WithTrailingTrivia(lastTrue.GetTrailingTrivia())

                    trueStatements = trueStatements.Replace(lastTrue, newLastTrue)
                    falseStatements = falseStatements.Replace(lastFalse, newLastFalse)
                End If
            End If

            Dim updatedIf = ifNode _
                .WithCondition(DirectCast(condition, ExpressionSyntax)) _
                .WithStatements(trueStatements)

            If falseStatements.Count <> 0 Then
                Dim elseClause =
                    If(updatedIf.ElseClause IsNot Nothing,
                       updatedIf.ElseClause.WithStatements(falseStatements),
                       SyntaxFactory.SingleLineElseClause(falseStatements))

                updatedIf = updatedIf.WithElseClause(elseClause)
            End If

            Return updatedIf
        End Function
    End Class
End Namespace

