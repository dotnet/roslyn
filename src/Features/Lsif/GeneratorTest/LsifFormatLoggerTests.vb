' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Logging
Imports Microsoft.Extensions.Logging

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    Public Class LsifFormatLoggerTests
        <Theory>
        <InlineData(LogLevel.Information, "Info")>
        <InlineData(LogLevel.Warning, "Warning")>
        <InlineData(LogLevel.Error, "Error")>
        <InlineData(LogLevel.Critical, "Critical")>
        Public Sub TestSimpleMessage(logLevel As LogLevel, expectedSeverity As String)
            Dim writer = New StringWriter
            Dim logger = New LsifFormatLogger(writer)

            logger.Log(logLevel, "Test message")

            Assert.Equal("{""command"":""log"",""parameters"":{""severity"":""" + expectedSeverity + """,""message"":""Test message""}}", writer.ToString())
        End Sub

        <Fact>
        Public Sub TestException()
            Dim writer = New StringWriter
            Dim logger = New LsifFormatLogger(writer)

            logger.LogError(New Exception("Exception!"), "An exception was thrown")

            Assert.Equal("{""command"":""log"",""parameters"":{""severity"":""Error"",""message"":""An exception was thrown"",""exceptionMessage"":""Exception!"",""exceptionType"":""System.Exception""}}", writer.ToString())
        End Sub
    End Class
End Namespace
