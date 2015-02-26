Imports System.Runtime.CompilerServices

Namespace Global.Exts

    Friend Module [Char]

        <Extension>
        Public Function IsAnyOf(c As Char, c0 As Char, c1 As Char) As Boolean
            Return (c = c0) OrElse (c = c1)
        End Function

        <Extension>
        Public Function IsAnyOf(c As Char, c0 As Char, c1 As Char, c2 As Char) As Boolean
            Return (c = c0) OrElse (c = c1) OrElse (c = c2)
        End Function

        <Extension>
        Public Function IsAnyOf(c As Char, c0 As Char, c1 As Char, c2 As Char, c3 As Char) As Boolean
            Return (c = c0) OrElse (c = c1) OrElse (c = c2) OrElse (c = c3)
        End Function

        <Extension>
        Public Function IsAnyOf(c As Char, c0 As Char, c1 As Char, c2 As Char, c3 As Char, c4 As Char) As Boolean
            Return (c = c0) OrElse (c = c1) OrElse (c = c2) OrElse (c = c3) OrElse (c = c4)
        End Function
        <Extension>
        Public Function IsAnyOf(c As Char, c0 As Char, c1 As Char, c2 As Char, c3 As Char, c4 As Char, c5 As Char) As Boolean
            Return (c = c0) OrElse (c = c1) OrElse (c = c2) OrElse (c = c3) OrElse (c = c4) OrElse (c = c5)
        End Function

    End Module

  End Namespace