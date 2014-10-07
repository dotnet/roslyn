Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module SyntaxNodeOrTokenListExtensions

        <Extension()>
        Friend Function IndexOf(list As SyntaxNodeOrTokenList, nodeOrToken As SyntaxNodeOrToken) As Integer
            Dim i As Integer = 0
            Dim child As SyntaxNodeOrToken
            For Each child In list
                If (child = nodeOrToken) Then
                    Return i
                End If
                i += 1
            Next
            Return -1
        End Function

    End Module

End Namespace