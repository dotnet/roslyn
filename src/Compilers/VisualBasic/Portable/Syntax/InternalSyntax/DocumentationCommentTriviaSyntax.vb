' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class DocumentationCommentTriviaSyntax

        Friend Function GetInteriorXml() As String
            Dim sb As New StringBuilder
            WriteInteriorXml(Me, sb)
            Return sb.ToString
        End Function

        Private Shared Sub WriteInteriorXml(node As VisualBasicSyntaxNode, sb As StringBuilder)
            If node Is Nothing Then
                Return
            End If

            Dim childCnt = node.SlotCount
            If childCnt > 0 Then
                For i = 0 To childCnt - 1
                    Dim child = DirectCast(node.GetSlot(i), VisualBasicSyntaxNode)
                    WriteInteriorXml(child, sb)
                Next
            Else
                Dim tk = DirectCast(node, SyntaxToken)
                WriteInteriorXml(New SyntaxList(Of VisualBasicSyntaxNode)(tk.GetLeadingTrivia), sb)
                WriteInteriorXml(tk, sb)
                WriteInteriorXml(New SyntaxList(Of VisualBasicSyntaxNode)(tk.GetTrailingTrivia), sb)
            End If
        End Sub

        Private Shared Sub WriteInteriorXml(node As SyntaxToken, sb As StringBuilder)
            If node.Kind <> SyntaxKind.DocumentationCommentLineBreakToken Then
                Dim txt = node.Text
                Debug.Assert(txt <> vbCr AndAlso txt <> vbLf AndAlso txt <> vbCrLf)
                sb.Append(node.Text)
            End If
        End Sub

        Private Shared Sub WriteInteriorXml(node As SyntaxList(Of VisualBasicSyntaxNode), sb As StringBuilder)
            For i = 0 To node.Count - 1
                Dim t = node(i)
                If t.Kind <> SyntaxKind.DocumentationCommentExteriorTrivia Then
                    sb.Append(t.ToString)
                End If
            Next
        End Sub

    End Class

End Namespace
