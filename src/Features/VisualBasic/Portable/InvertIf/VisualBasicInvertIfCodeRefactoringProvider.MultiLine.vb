' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertMultiLineIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertMultiLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsElseless(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseBlock Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseIfBlocks.IsEmpty
        End Function

        Protected Overrides Function GetCondition(ifNode As MultiLineIfBlockSyntax) As SyntaxNode
            Return ifNode.IfStatement.Condition
        End Function

        Protected Overrides Function GetIfBody(ifNode As MultiLineIfBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.Statements
        End Function

        Protected Overrides Function GetElseBody(ifNode As MultiLineIfBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.ElseBlock.Statements
        End Function

        Protected Overrides Function UpdateIf(
                sourceText As SourceText,
                ifNode As MultiLineIfBlockSyntax,
                condition As SyntaxNode,
                trueStatement As SyntaxList(Of StatementSyntax),
                Optional falseStatementOpt As SyntaxList(Of StatementSyntax) = Nothing) As MultiLineIfBlockSyntax

            Dim updatedIf = ifNode _
                .WithIfStatement(ifNode.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax))) _
                .WithStatements(trueStatement)

            If falseStatementOpt.Count > 0 Then
                updatedIf = updatedIf.WithElseBlock(SyntaxFactory.ElseBlock(falseStatementOpt))
            ElseIf HasComment(ifNode.ElseBlock) Then
                ' If the original else block has leading trivia
                ' then we need to mantain that trivia for the new statement.
                ' This should be attached to the "EndIf" statement now
                ' because that will make it show up in the else block
                Dim newEndIfStatement = SyntaxFactory.EndIfStatement().WithLeadingTrivia(ifNode.ElseBlock.GetLeadingTrivia())
                updatedIf = updatedIf.WithEndIfStatement(newEndIfStatement)
                updatedIf = updatedIf.WithElseBlock(SyntaxFactory.ElseBlock())
            Else
                updatedIf = updatedIf.WithElseBlock(Nothing)
            End If

            Return updatedIf
        End Function

        Private Shared Function HasComment(elseBlock As ElseBlockSyntax) As Boolean
            Return elseBlock IsNot Nothing AndAlso elseBlock.GetLeadingTrivia().Any(Function(trivia) trivia.IsKind(SyntaxKind.CommentTrivia))
        End Function
    End Class
End Namespace

