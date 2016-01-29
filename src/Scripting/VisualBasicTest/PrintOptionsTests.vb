' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting.UnitTests

    Public Class PrintOptionsTests
        Inherits ObjectFormatterTestBase

        Private Shared ReadOnly Formatter As ObjectFormatter = New TestVisualBasicObjectFormatter()

        <Fact>
        Public Sub NullOptions()
            Assert.Throws(Of ArgumentNullException)(Sub() Formatter.FormatObject("hello", options:=Nothing))
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
            Assert.Equal("10", Formatter.FormatObject(10, options))
            Assert.Equal("Integer(10) { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }", Formatter.FormatObject(array, options))
            Assert.Equal("ChrW(16)", Formatter.FormatObject(ChrW(&H10), options))

            options.NumberRadix = 16
            Assert.Equal("&H0000000A", Formatter.FormatObject(10, options))
            Assert.Equal("Integer(&H0000000A) { &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000, &H00000000 }", Formatter.FormatObject(array, options))
            Assert.Equal("ChrW(&H10)", Formatter.FormatObject(ChrW(&H10), options))
        End Sub

        <Fact>
        Public Sub ValidMemberDisplayFormat()
            Dim options = New PrintOptions()

            options.MemberDisplayFormat = MemberDisplayFormat.Hidden
            Assert.Equal("PrintOptions", Formatter.FormatObject(options, options))

            options.MemberDisplayFormat = MemberDisplayFormat.SingleLine
            Assert.Equal("PrintOptions { Ellipsis=""..."", EscapeNonPrintableCharacters=True, MaximumOutputLength=1024, MemberDisplayFormat=SingleLine, NumberRadix=10 }", Formatter.FormatObject(options, options))

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
", Formatter.FormatObject(options, options))
        End Sub

        <Fact>
        Public Sub ValidEscapeNonPrintableCharacters()
            Dim options = New PrintOptions()

            options.EscapeNonPrintableCharacters = True
            Assert.Equal("vbTab", Formatter.FormatObject(vbTab, options))
            Assert.Equal("vbTab", Formatter.FormatObject(vbTab(0), options))

            options.EscapeNonPrintableCharacters = False
            Assert.Equal("""" + vbTab + """", Formatter.FormatObject(vbTab, options))
            Assert.Equal("""" + vbTab + """c", Formatter.FormatObject(vbTab(0), options))
        End Sub

        <Fact>
        Public Sub ValidMaximumOutputLength()
            Dim options = New PrintOptions()

            options.MaximumOutputLength = 1
            Assert.Equal("1...", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 2
            Assert.Equal("12...", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 3
            Assert.Equal("123...", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 4
            Assert.Equal("1234...", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 5
            Assert.Equal("12345...", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 6
            Assert.Equal("123456", Formatter.FormatObject(123456, options))

            options.MaximumOutputLength = 7
            Assert.Equal("123456", Formatter.FormatObject(123456, options))
        End Sub

        <Fact>
        Public Sub ValidEllipsis()
            Dim options = New PrintOptions()
            options.MaximumOutputLength = 1

            options.Ellipsis = "."
            Assert.Equal("1.", Formatter.FormatObject(123456, options))

            options.Ellipsis = ".."
            Assert.Equal("1..", Formatter.FormatObject(123456, options))

            options.Ellipsis = ""
            Assert.Equal("1", Formatter.FormatObject(123456, options))

            options.Ellipsis = Nothing
            Assert.Equal("1", Formatter.FormatObject(123456, options))
        End Sub

    End Class

End Namespace
