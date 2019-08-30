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

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueWhileStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            If node.IsIncorrectExitStatement(SyntaxKind.ExitWhileStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim whileBlock = node.GetAncestor(Of WhileBlockSyntax)()
            If whileBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)

            With whileBlock
                highlights.Add(.WhileStatement.WhileKeyword.Span)

                highlights.AddRange(
                    whileBlock.GetRelatedStatementHighlights(
                        blockKind:=SyntaxKind.WhileKeyword))

                highlights.Add(.EndWhileStatement.Span)
            End With

            Return highlights
        End Function

    End Class
End Namespace
