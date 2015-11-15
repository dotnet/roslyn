' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
Imports ObjectFormatterFixtures
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.UnitTests

    Public Class ObjectFormatterTests
        Inherits ObjectFormatterTestBase

        <Fact()>
        Public Sub InlineCharacters()
            Assert.Equal("ChrW(20)", VisualBasicObjectFormatter.Instance.FormatObject(ChrW(20), s_inline))
            Assert.Equal("vbBack", VisualBasicObjectFormatter.Instance.FormatObject(ChrW(&H8), s_inline))
        End Sub

        <Fact(Skip:="IDK")>
        Public Sub QuotedStrings()
            Dim s = "a" & ChrW(&HFFFE) & ChrW(&HFFFF) & vbCrLf & "b"

            ' ObjectFormatter should substitute spaces for non-printable characters
            Assert.Equal("""a"" & ChrW(&HABCF) & ChrW(&HABCD) & vbCrLf & ""b""", VisualBasicObjectFormatter.Instance.FormatObject(s, s_hexa.Copy(quoteStrings:=True)))
            Assert.Equal("a    b", VisualBasicObjectFormatter.Instance.FormatObject(s, s_hexa.Copy(quoteStrings:=False)))
        End Sub

        <Fact>
        Public Sub Objects()
            Dim str As String
            Dim nested As Object = New Outer.Nested(Of Integer)()

            str = VisualBasicObjectFormatter.Instance.FormatObject(nested, s_inline)
            Assert.Equal("Outer.Nested(Of Integer) { A=1, B=2 }", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(nested, New ObjectFormattingOptions(memberFormat:=MemberDisplayFormat.NoMembers))
            Assert.Equal("Outer.Nested(Of Integer)", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(A(Of Integer).X, New ObjectFormattingOptions(memberFormat:=MemberDisplayFormat.NoMembers))
            Assert.Equal("A(Of Integer).B(Of Integer)", str)

            Dim obj As Object = New A(Of Integer).B(Of Boolean).C.D(Of String, Double).E()
            str = VisualBasicObjectFormatter.Instance.FormatObject(obj, New ObjectFormattingOptions(memberFormat:=MemberDisplayFormat.NoMembers))
            Assert.Equal("A(Of Integer).B(Of Boolean).C.D(Of String, Double).E", str)

            Dim sort = New Sort()
            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=51, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, a ...", str)
            Assert.Equal(51, str.Length)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=5, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("S ...", str)
            Assert.Equal(5, str.Length)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=4, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("...", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=3, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("...", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=2, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("...", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=1, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("...", str)

            str = VisualBasicObjectFormatter.Instance.FormatObject(sort, New ObjectFormattingOptions(maxLineLength:=80, memberFormat:=MemberDisplayFormat.Inline))
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str)
        End Sub

        ' TODO: port tests from C#
    End Class

End Namespace
