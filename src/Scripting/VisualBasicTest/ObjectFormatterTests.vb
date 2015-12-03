' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
Imports ObjectFormatterFixtures
Imports Xunit
Imports Formatter = Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests.TestVisualBasicObjectFormatter

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.UnitTests

    Public Class ObjectFormatterTests
        Inherits ObjectFormatterTestBase

        <Fact()>
        Public Sub InlineCharacters()
            Assert.Equal("ChrW(20)", Formatter.SingleLine.FormatObject(ChrW(20)))
            Assert.Equal("vbBack", Formatter.SingleLine.FormatObject(ChrW(&H8)))
        End Sub

        <Fact(Skip:="IDK")>
        Public Sub QuotedStrings()
            Dim s = "a" & ChrW(&HFFFE) & ChrW(&HFFFF) & vbCrLf & "b"

            Dim withQuotes = New Formatter(useHexadecimalNumbers:=True, omitStringQuotes:=False)
            Dim withoutQuotes = New Formatter(useHexadecimalNumbers:=True, omitStringQuotes:=True)

            ' ObjectFormatter should substitute spaces for non-printable characters
            Assert.Equal("""a"" & ChrW(&HABCF) & ChrW(&HABCD) & vbCrLf & ""b""", withQuotes.FormatObject(s))
            Assert.Equal("a    b", withoutQuotes.FormatObject(s))
        End Sub

        <Fact>
        Public Sub Objects()
            Dim str As String
            Dim nested As Object = New Outer.Nested(Of Integer)()

            str = Formatter.SingleLine.FormatObject(nested)
            Assert.Equal("Outer.Nested(Of Integer) { A=1, B=2 }", str)

            str = Formatter.Hidden.FormatObject(nested)
            Assert.Equal("Outer.Nested(Of Integer)", str)

            str = Formatter.Hidden.FormatObject(A(Of Integer).X)
            Assert.Equal("A(Of Integer).B(Of Integer)", str)

            Dim obj As Object = New A(Of Integer).B(Of Boolean).C.D(Of String, Double).E()
            str = Formatter.Hidden.FormatObject(obj)
            Assert.Equal("A(Of Integer).B(Of Boolean).C.D(Of String, Double).E", str)

            Dim sort = New Sort()
            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=51).FormatObject(sort)
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, a ...", str)
            Assert.Equal(51, str.Length)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=5).FormatObject(sort)
            Assert.Equal("S ...", str)
            Assert.Equal(5, str.Length)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=4).FormatObject(sort)
            Assert.Equal("...", str)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=3).FormatObject(sort)
            Assert.Equal("...", str)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=2).FormatObject(sort)
            Assert.Equal("...", str)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=1).FormatObject(sort)
            Assert.Equal("...", str)

            str = New Formatter(MemberDisplayFormat.SingleLine, lineLengthLimit:=80).FormatObject(sort)
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str)
        End Sub

        ' TODO: port tests from C#
    End Class

End Namespace
