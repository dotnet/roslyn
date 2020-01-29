' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class DocumentationCommentTriviaSyntax

        Friend Function GetInteriorXml() As String
            Dim sb As New StringBuilder
            WriteInteriorXml(DirectCast(Me, GreenNode), sb)
            Return sb.ToString
        End Function

        Private Shared Sub WriteInteriorXml(node As GreenNode, sb As StringBuilder)
            If node Is Nothing Then
                Return
            End If

            Dim childCnt = node.SlotCount
            If childCnt > 0 Then
                For i = 0 To childCnt - 1
                    Dim child = node.GetSlot(i)
                    WriteInteriorXml(child, sb)
                Next
            Else
                Dim tk = DirectCast(node, SyntaxToken)
                WriteInteriorXml(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(tk.GetLeadingTrivia), sb)
                WriteInteriorXml(tk, sb)
                WriteInteriorXml(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(tk.GetTrailingTrivia), sb)
            End If
        End Sub

        Private Shared Sub WriteInteriorXml(node As SyntaxToken, sb As StringBuilder)
            If node.Kind <> SyntaxKind.DocumentationCommentLineBreakToken Then
                Dim txt = node.Text
                Debug.Assert(txt <> vbCr AndAlso txt <> vbLf AndAlso txt <> vbCrLf)
                sb.Append(node.Text)
            End If
        End Sub

        Private Shared Sub WriteInteriorXml(node As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), sb As StringBuilder)
            For i = 0 To node.Count - 1
                Dim t = node(i)
                If t.Kind <> SyntaxKind.DocumentationCommentExteriorTrivia Then
                    sb.Append(t.ToString)
                End If
            Next
        End Sub
    End Class
End Namespace
