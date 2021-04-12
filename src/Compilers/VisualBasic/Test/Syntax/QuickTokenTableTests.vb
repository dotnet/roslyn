' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports InternalSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Public Class QuickTokenTableTests

    Private Shared Sub ShouldBeGoodQuickToken(s As String)
        Dim txt = SourceText.From(s)
        Using acc As New InternalSyntax.Scanner(txt, TestOptions.Regular)

            Dim qt = acc.QuickScanToken(False)

            Assert.True(qt.Succeeded)
            Assert.Equal(s.Length - 1, qt.Length)

            Dim ca = qt.Chars
            For i = 0 To qt.Length - 1
                Assert.Equal(ca(i), s(i))
            Next
        End Using
    End Sub

    Private Shared Sub ShouldBeBadQuickToken(s As String)
        Dim txt = SourceText.From(s)
        Using acc As New InternalSyntax.Scanner(txt, TestOptions.Regular)
            Dim qt = acc.QuickScanToken(False)

            Assert.False(qt.Succeeded)
        End Using
    End Sub

    <Fact>
    Public Sub GoodQuickTokens()
        ShouldBeGoodQuickToken("a4 A")
        ShouldBeGoodQuickToken(" xyzzy 9")
        ShouldBeGoodQuickToken(" xyzzy@ 9")
        ShouldBeGoodQuickToken("    xyzzy A")
        ShouldBeGoodQuickToken(",a")
        ShouldBeGoodQuickToken("  ,a")
        ShouldBeGoodQuickToken("  ( 8")
        ShouldBeGoodQuickToken("  ,  a")
        ShouldBeGoodQuickToken("  ( 8")
        ShouldBeGoodQuickToken("a,")
        ShouldBeGoodQuickToken("ab$,")
        ShouldBeGoodQuickToken("  a  ,")
        ShouldBeGoodQuickToken("  ,a")

        ShouldBeGoodQuickToken("} x")
        ShouldBeGoodQuickToken("{ A")
        ShouldBeGoodQuickToken(". F")
        ShouldBeGoodQuickToken("+ a")
    End Sub

    <Fact>
    Public Sub BadQuickTokens()
        ShouldBeBadQuickToken("a" + vbCr)
        ShouldBeBadQuickToken("ab$" + vbCr)
        ShouldBeBadQuickToken("  a  " + vbCr)
        ShouldBeBadQuickToken("+ " & vbCr)
        ShouldBeBadQuickToken(">    =")
        ShouldBeBadQuickToken("<    <")
        ShouldBeBadQuickToken(" .9")
        ShouldBeBadQuickToken("  A" + ChrW(156))
        ShouldBeBadQuickToken("   aa  '")
        ShouldBeBadQuickToken("+ =")
    End Sub

    Private Shared Function RandomLetter(rand As Random) As Char
        Return ChrW(rand.Next(AscW("A"), AscW("K")))
    End Function

    Private Shared Function CreateRandomEntry(rand As Random, table As TextKeyedCache(Of InternalSyntax.SyntaxToken)) As Tuple(Of String, InternalSyntax.SyntaxToken)
        Dim buf(0 To 40) As Char

        Dim count = rand.Next(10, 20)
        For i = 0 To count - 1
            buf(i) = RandomLetter(rand)
        Next
        buf(count) = " "c
        buf(count + 1) = "Z"c

        Using scanner As New InternalSyntax.Scanner(SourceText.From(New String(buf)), TestOptions.Regular)
            Dim qt = scanner.QuickScanToken(False)
            Assert.True(qt.Succeeded)

            Dim text = New String(qt.Chars, 0, qt.Length)
            Dim token = InternalSyntax.SyntaxFactory.Identifier(text)

            Assert.Null(table.FindItem(qt.Chars, qt.Start, qt.Length, qt.HashCode))
            table.AddItem(qt.Chars, qt.Start, qt.Length, qt.HashCode, DirectCast(token, InternalSyntax.SyntaxToken))
            Return New Tuple(Of String, InternalSyntax.SyntaxToken)(text, token)
        End Using
    End Function

    Private Shared Sub CheckEntry(table As TextKeyedCache(Of InternalSyntax.SyntaxToken), e As Tuple(Of String, InternalSyntax.SyntaxToken))
        Dim buf(0 To 40) As Char

        For i = 0 To e.Item1.Length - 1
            buf(i) = e.Item1(i)
        Next
        buf(e.Item1.Length) = "Z"c

        Using scanner As New InternalSyntax.Scanner(SourceText.From(New String(buf)), TestOptions.Regular)
            Dim qt = scanner.QuickScanToken(False)
            Assert.True(qt.Succeeded)

            Dim tokFound = table.FindItem(qt.Chars, qt.Start, qt.Length, qt.HashCode)
            Assert.Same(e.Item2, tokFound)
        End Using
    End Sub

    <Fact>
    Public Sub QuickTokenTable()
        Const ITER = 1000

        Dim rand As New Random(123)
        Dim table As New TextKeyedCache(Of InternalSyntax.SyntaxToken)

        For i = 0 To ITER - 1
            Dim e = CreateRandomEntry(rand, table)
            CheckEntry(table, e)
        Next
    End Sub

End Class
