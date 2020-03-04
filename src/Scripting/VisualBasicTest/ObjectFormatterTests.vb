﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
Imports ObjectFormatterFixtures
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.UnitTests

    Public Class ObjectFormatterTests
        Inherits ObjectFormatterTestBase

        Private Shared ReadOnly s_formatter As ObjectFormatter = New TestVisualBasicObjectFormatter()

        <Fact()>
        Public Sub InlineCharacters()
            Assert.Equal("ChrW(20)", s_formatter.FormatObject(ChrW(20), SingleLineOptions))
            Assert.Equal("vbBack", s_formatter.FormatObject(ChrW(&H8), SingleLineOptions))
        End Sub

        <Fact(Skip:="IDK")>
        Public Sub QuotedStrings()
            Dim s = "a" & ChrW(&HFFFE) & ChrW(&HFFFF) & vbCrLf & "b"

            Dim options = New PrintOptions With {.NumberRadix = ObjectFormatterHelpers.NumberRadixHexadecimal}
            Dim withQuotes = New TestVisualBasicObjectFormatter(quoteStringsAndCharacters:=True)
            Dim withoutQuotes = New TestVisualBasicObjectFormatter(quoteStringsAndCharacters:=False)

            ' ObjectFormatter should substitute spaces for non-printable characters
            Assert.Equal("""a"" & ChrW(&HABCF) & ChrW(&HABCD) & vbCrLf & ""b""", withQuotes.FormatObject(s, options))
            Assert.Equal("a    b", withoutQuotes.FormatObject(s, options))
        End Sub

        <Fact>
        Public Sub Objects()
            Dim str As String
            Dim nested As Object = New Outer.Nested(Of Integer)()

            str = s_formatter.FormatObject(nested, SingleLineOptions)
            Assert.Equal("Outer.Nested(Of Integer) { A=1, B=2 }", str)

            str = s_formatter.FormatObject(nested, HiddenOptions)
            Assert.Equal("Outer.Nested(Of Integer)", str)

            str = s_formatter.FormatObject(A(Of Integer).X, HiddenOptions)
            Assert.Equal("A(Of Integer).B(Of Integer)", str)

            Dim obj As Object = New A(Of Integer).B(Of Boolean).C.D(Of String, Double).E()
            str = s_formatter.FormatObject(obj, HiddenOptions)
            Assert.Equal("A(Of Integer).B(Of Boolean).C.D(Of String, Double).E", str)

            Dim sort = New Sort()
            str = New TestVisualBasicObjectFormatter(maximumLineLength:=51).FormatObject(sort, SingleLineOptions)
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1...", str)
            Assert.Equal(51 + 3, str.Length)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=5).FormatObject(sort, SingleLineOptions)
            Assert.Equal("Sort ...", str)
            Assert.Equal(5 + 3, str.Length)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=4).FormatObject(sort, SingleLineOptions)
            Assert.Equal("Sort...", str)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=3).FormatObject(sort, SingleLineOptions)
            Assert.Equal("Sor...", str)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=2).FormatObject(sort, SingleLineOptions)
            Assert.Equal("So...", str)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=1).FormatObject(sort, SingleLineOptions)
            Assert.Equal("S...", str)

            str = New TestVisualBasicObjectFormatter(maximumLineLength:=80).FormatObject(sort, SingleLineOptions)
            Assert.Equal("Sort { aB=-1, ab=1, Ac=-1, Ad=1, ad=-1, aE=1, aF=-1, AG=1 }", str)
        End Sub

        <Fact>
        Public Sub EscapeWithoutQuotes()
            Dim primitiveFormatter As New TestPrimitiveObjectFormatter()
            Assert.Throws(Of ArgumentException)(Sub() primitiveFormatter.TestEscapeStringWithoutQuotes())
            Assert.Throws(Of ArgumentException)(Sub() primitiveFormatter.TestEscapeCharWithoutQuotes())
        End Sub

        Private Class TestPrimitiveObjectFormatter
            Inherits VisualBasicPrimitiveFormatter

            Public Sub TestEscapeStringWithoutQuotes()
                FormatLiteral("a", useQuotes:=False, escapeNonPrintable:=True)
            End Sub

            Public Sub TestEscapeCharWithoutQuotes()
                FormatLiteral("a"c, useQuotes:=False, escapeNonPrintable:=True)
            End Sub
        End Class

        ' TODO: port tests from C#
    End Class

End Namespace
