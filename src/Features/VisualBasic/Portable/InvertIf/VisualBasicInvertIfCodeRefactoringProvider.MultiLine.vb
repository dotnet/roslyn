' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertMultiLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        <ImportingConstructor>
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
            End If

            Return updatedIf
        End Function
    End Class
End Namespace

