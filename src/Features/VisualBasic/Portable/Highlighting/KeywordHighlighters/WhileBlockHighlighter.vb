' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic), [Shared]>
    Friend Class WhileBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
