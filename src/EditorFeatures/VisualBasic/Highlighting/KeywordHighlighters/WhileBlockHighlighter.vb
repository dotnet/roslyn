' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class WhileBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueWhileStatement) Then
                Return
            End If

            If node.IsIncorrectExitStatement(SyntaxKind.ExitWhileStatement) Then
                Return
            End If

            Dim whileBlock = node.GetAncestor(Of WhileBlockSyntax)()
            If whileBlock Is Nothing Then
                Return
            End If

            With whileBlock
                highlights.Add(.WhileStatement.WhileKeyword.Span)

                highlights.AddRange(
                    whileBlock.GetRelatedStatementHighlights(
                        blockKind:=SyntaxKind.WhileKeyword))

                highlights.Add(.EndWhileStatement.Span)
            End With
        End Sub

    End Class
End Namespace
