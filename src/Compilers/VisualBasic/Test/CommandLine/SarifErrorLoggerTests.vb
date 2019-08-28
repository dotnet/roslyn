' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Roslyn.Test.Utilities.SharedResourceHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Public MustInherit Class SarifErrorLoggerTests
        Inherits BasicTestBase

        Private ReadOnly _baseDirectory As String = TempRoot.Root

        Protected Readonly MustOverride Property VersionSpecificArguments() As String()

        Friend MustOverride Function GetExpectedOutputForNoDiagnostics(
            cmd As CommonCompiler) As String

        Friend MustOverride Function GetExpectedOutputForSimpleCompilerDiagnostics(
            cmd As CommonCompiler,
            sourceFilePath As String) As String

        Friend MustOverride Function GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(
            cmd As CommonCompiler,
            sourceFilePath As String) As String

        Friend MustOverride Function GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(
            cmd As MockVisualBasicCompiler) As String

        Protected Sub NoDiagnosticsImpl()
            Dim helloWorldVB As String = <text>
Imports System
Class C
    Shared Sub Main(args As String())
        Console.WriteLine("Hello, world")
    End Sub
End Class
</text>.Value

            Dim hello = Temp.CreateFile().WriteAllText(helloWorldVB).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim arguments = {
                "/nologo",
                $"/errorlog:{errorLogFile}",
                hello
            }.Concat(VersionSpecificArguments).ToArray()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, arguments)
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Assert.Equal("", outWriter.ToString().Trim())
            Assert.Equal(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()
            Dim expectedOutput = GetExpectedOutputForNoDiagnostics(cmd)

            Assert.Equal(expectedOutput, actualOutput)

            CleanupAllGeneratedFiles(hello)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        Protected Sub SimpleCompilerDiagnosticsImpl()
            Dim source As String = <text>
Public Class C
    Public Sub Method()
        Dim x As Integer
    End Sub
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim arguments = {
                "/nologo",
                "/preferreduilang:en",
                $"/errorlog:{errorLogFile}",
                sourceFilePath
            }.Concat(VersionSpecificArguments).ToArray()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, arguments)
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()
            Dim expectedOutput = GetExpectedOutputForSimpleCompilerDiagnostics(cmd, sourceFilePath)

            Assert.Equal(expectedOutput, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        Protected Sub SimpleCompilerDiagnosticsSuppressedImpl()
            Dim source As String = <text>
Public Class C
    Public Sub Method()
#Disable Warning BC42024
        Dim x As Integer
#Enable Warning BC42024
    End Sub
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim errorLogDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(errorLogDir.Path, "ErrorLog.txt")

            Dim arguments = {
                "/nologo",
                "/preferreduilang:en",
                 $"/errorlog:{errorLogFile}",
                 sourceFilePath
            }.Concat(VersionSpecificArguments).ToArray()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory, arguments)
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            ' Suppressed diagnostics are only reported in the error log, not the console output.
            Assert.DoesNotContain("BC42024", actualConsoleOutput)
            Assert.Contains("BC30420", actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()
            Dim expectedOutput = GetExpectedOutputForSimpleCompilerDiagnosticsSuppressed(cmd, sourceFilePath)

            Assert.Equal(expectedOutput, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

        Protected Sub AnalyzerDiagnosticsWithAndWithoutLocationImpl()
            Dim source As String = <text>
Imports System
Class C
End Class
</text>.Value

            Dim sourceFilePath = Temp.CreateFile().WriteAllText(source).Path
            Dim outputDir = Temp.CreateDirectory()
            Dim errorLogFile = Path.Combine(outputDir.Path, "ErrorLog.txt")
            Dim outputFilePath = Path.Combine(outputDir.Path, "test.dll")

            Dim arguments = {
                "/nologo",
                 "/preferreduilang:en",
                 "/t:library",
                 $"/out:{outputFilePath}",
                 $"/errorlog:{errorLogFile}",
                 sourceFilePath
            }.Concat(VersionSpecificArguments).ToArray()

            Dim cmd = New MockVisualBasicCompiler(Nothing, _baseDirectory,
                arguments,
                analyzer:=New AnalyzerForErrorLogTest())
            Dim outWriter = New StringWriter(CultureInfo.InvariantCulture)

            Dim exitCode = cmd.Run(outWriter, Nothing)
            Dim actualConsoleOutput = outWriter.ToString().Trim()

            Assert.Contains(AnalyzerForErrorLogTest.Descriptor1.Id, actualConsoleOutput)
            Assert.Contains(AnalyzerForErrorLogTest.Descriptor2.Id, actualConsoleOutput)
            Assert.NotEqual(0, exitCode)

            Dim actualOutput = File.ReadAllText(errorLogFile).Trim()
            Dim expectedOutput = GetExpectedOutputForAnalyzerDiagnosticsWithAndWithoutLocation(cmd)

            Assert.Equal(expectedOutput, actualOutput)

            CleanupAllGeneratedFiles(sourceFilePath)
            CleanupAllGeneratedFiles(outputFilePath)
            CleanupAllGeneratedFiles(errorLogFile)
        End Sub

    End Class
End Namespace