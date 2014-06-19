Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend NotInheritable Class BinaryExpressionSyntax
        Inherits ExpressionSyntax

        Friend Overrides Sub WriteTo(writer As IO.TextWriter)
            Debug.Assert(SlotCount() = 3)

            Dim nodes = ArrayBuilder(Of SyntaxNode).GetInstance()
            Dim leftmost As SyntaxNode = Me

            Do
                Dim binary = TryCast(leftmost, BinaryExpressionSyntax)
                If binary Is Nothing Then
                    Exit Do
                End If

                nodes.Push(leftmost.GetSlot(2))
                nodes.Push(leftmost.GetSlot(1))
                leftmost = leftmost.GetSlot(0)
            Loop

            leftmost.WriteTo(writer)

            While nodes.Count > 0
                nodes.Pop().WriteTo(writer)
            End While

            nodes.Free()
        End Sub

    End Class
End Namespace