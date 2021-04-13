' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.UnitTests

    Public Class PrintOptionsTests
        Inherits ObjectFormatterTestBase

        Private Shared ReadOnly s_formatter As ObjectFormatter = New TestVisualBasicObjectFormatter()

        <Fact>
        Public Sub NullOptions()
            Assert.Throws(Of ArgumentNullException)(Sub() s_formatter.FormatObject("hello", options:=Nothing))
        End Sub

        <Fact>
        Public Sub InvalidNumberRadix()
            Assert.Throws(Of ArgumentOutOfRangeException)(
                Sub()
                    Dim options As New PrintOptions()
                    options.NumberRadix = 3
                End Sub)
        End Sub

        <Fact>
        Public Sub InvalidMemberDisplayFormat()
            Assert.Throws(Of ArgumentOutOfRangeException)(
                Sub()
                    Dim options As New PrintOptions()
                    options.MemberDisplayFormat = CType(-1, MemberDisplayFormat)
                End Sub)
        End Sub

        <Fact>
        Public Sub InvalidMaximumOutputLength()
            Assert.Throws(Of ArgumentOutOfRangeException)(
                Sub()
                    Dim options As New PrintOptions()
                    options.MaximumOutputLength = -1
                End Sub)
            Assert.Throws(Of ArgumentOutOfRangeException)(
                Sub()
                    Dim options As New PrintOptions()
                    options.MaximumOutputLength = 0
                End Sub)
        End Sub

        <Fact>
        Public Sub ValidNumberRadix()
            Dim options = New PrintOptions()
            Dim array(9) As Integer

            options.NumberRadix = 10
            Assert.Equal("10", s_formatter.FormatObject(10, options))
            Assert.Equal("Integer(10) { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }", s_formatter.FormatObject(array, options))
            Assert.Equal("ChrW(16)", s_formatter.FormatObject(ChrW(&H10), options))

            options.NumberRadix = 16
            Assert.Equal("&H0000000A", s_formatter.FormatObject(10, options))
            Assert.Equal("Integer(&H0000000A) { &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000 }", s_formatter.FormatObject(array, options))
            Assert.Equal("ChrW(&H10)", s_formatter.FormatObject(ChrW(&H10), options))
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/8241")>
        Public Sub ValidMemberDisplayFormat()
            Dim options = New PrintOptions()

            options.MemberDisplayFormat = MemberDisplayFormat.Hidden
            Assert.Equal("PrintOptions", s_formatter.FormatObject(options, options))

            options.MemberDisplayFormat = MemberDisplayFormat.SingleLine
            Assert.Equal("PrintOptions { Ellipsis=""..."", EscapeNonPrintableCharacters=True, MaximumOutputLength=1024, MemberDisplayFormat=SingleLine, NumberRadix=10 }", s_formatter.FormatObject(options, options))

            options.MemberDisplayFormat = MemberDisplayFormat.SeparateLines
            Assert.Equal("PrintOptions {
  Ellipsis: ""..."",
  EscapeNonPrintableCharacters: True,
  MaximumOutputLength: 1024,
  MemberDisplayFormat: SeparateLines,
  NumberRadix: 10,
  _maximumOutputLength: 1024,
  _memberDisplayFormat: SeparateLines,
  _numberRadix: 10
}
", s_formatter.FormatObject(options, options))
        End Sub

        <Fact>
        Public Sub ValidEscapeNonPrintableCharacters()
            Dim options = New PrintOptions()

            options.EscapeNonPrintableCharacters = True
            Assert.Equal("vbTab", s_formatter.FormatObject(vbTab, options))
            Assert.Equal("vbTab", s_formatter.FormatObject(vbTab(0), options))

            options.EscapeNonPrintableCharacters = False
            Assert.Equal("""" + vbTab + """", s_formatter.FormatObject(vbTab, options))
            Assert.Equal("""" + vbTab + """c", s_formatter.FormatObject(vbTab(0), options))
        End Sub

        <Fact>
        Public Sub ValidMaximumOutputLength()
            Dim options = New PrintOptions()

            options.MaximumOutputLength = 1
            Assert.Equal("1...", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 2
            Assert.Equal("12...", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 3
            Assert.Equal("123...", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 4
            Assert.Equal("1234...", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 5
            Assert.Equal("12345...", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 6
            Assert.Equal("123456", s_formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 7
            Assert.Equal("123456", s_formatter.FormatObject(123456, options))
        End Sub

        <Fact>
        Public Sub ValidEllipsis()
            Dim options = New PrintOptions()
            options.MaximumOutputLength = 1

            options.Ellipsis = "."
            Assert.Equal("1.", s_formatter.FormatObject(123456, options))

            options.Ellipsis = ".."
            Assert.Equal("1..", s_formatter.FormatObject(123456, options))

            options.Ellipsis = ""
            Assert.Equal("1", s_formatter.FormatObject(123456, options))

            options.Ellipsis = Nothing
            Assert.Equal("1", s_formatter.FormatObject(123456, options))
        End Sub

    End Class

End Namespace
