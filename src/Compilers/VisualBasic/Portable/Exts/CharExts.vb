Imports System.Runtime.CompilerServices

Namespace Global.Roslyn.Exts
  <HideModuleName>
  Public Module CharExts

    '<Extension>
    'Public Function IsNoneOf(c As Char, cs As String) As Boolean
    '  Debug.Assert(Not String.IsNullOrEmpty(cs))
    '  Debug.Assert(cs.Length > 0)
    '  For i = 0 To cs.Length - 1
    '    If c = cs(i) Then Return False
    '  Next
    '  Return True
    'End Function

    <Extension>
    Public Function IsNoneOf(c As Char, c1 As Char, c2 As Char) As Boolean
      Return (c <> c1) AndAlso (c <> c2)
    End Function

    <Extension>
    Public Function IsNoneOf(c As Char, c1 As Char, c2 As Char, c3 As Char, c4 As Char) As Boolean
      Return (c <> c1) AndAlso (c <> c2) AndAlso (c <> c3) AndAlso (c <> c4)
    End Function

    <Extension>
    Public Function IsAnyOf(c As Char, c1 As Char, c2 As Char) As Boolean
      Return (c = c1) OrElse (c = c2)
    End Function

    <Extension>
    Public Function IsAnyOf(c As Char, c1 As Char, c2 As Char, c3 As Char) As Boolean
      Return (c = c1) OrElse (c = c2) OrElse (c = c3)
    End Function

    <Extension>
    Public Function IsAnyOf(c As Char, c1 As Char, c2 As Char, c3 As Char, c4 As Char) As Boolean
      Return (c = c1) OrElse (c = c2) OrElse (c = c3) OrElse (c = c4)
    End Function

    <Extension>
    Public Function IsBetween(c As Char, l As Char, u As Char) As Boolean
      Return (l <= c) AndAlso (c <= u)
    End Function

  End Module

End Namespace


