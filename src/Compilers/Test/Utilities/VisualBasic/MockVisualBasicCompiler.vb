' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics

Friend Class MockVisualBasicCompiler
    Inherits VisualBasicCompiler

    Private ReadOnly _analyzers As ImmutableArray(Of DiagnosticAnalyzer)
    Public Compilation As Compilation

    Public Sub New(baseDirectory As String, args As String(), Optional analyzer As DiagnosticAnalyzer = Nothing)
        MyClass.New(Nothing, baseDirectory, args, analyzer)
    End Sub

    Public Sub New(responseFile As String, baseDirectory As String, args As String(), Optional analyzer As DiagnosticAnalyzer = Nothing)
        MyClass.New(responseFile, baseDirectory, args, If(analyzer Is Nothing, ImmutableArray(Of DiagnosticAnalyzer).Empty, ImmutableArray.Create(analyzer)))
    End Sub

    Public Sub New(responseFile As String, baseDirectory As String, args As String(), analyzers As ImmutableArray(Of DiagnosticAnalyzer))
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, Path.GetDirectoryName(GetType(VisualBasicCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Environment.GetEnvironmentVariable("LIB"), New DesktopAnalyzerAssemblyLoader())

        _analyzers = analyzers
    End Sub

    Protected Overrides Function ResolveAnalyzersFromArguments(
        diagnostics As List(Of DiagnosticInfo),
        messageProvider As CommonMessageProvider) As ImmutableArray(Of DiagnosticAnalyzer)

        Dim analyzers = MyBase.ResolveAnalyzersFromArguments(diagnostics, messageProvider)
        If Not _analyzers.IsDefaultOrEmpty Then
            analyzers = analyzers.InsertRange(0, _analyzers)
        End If
        Return analyzers
    End Function

    Public Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger) As Compilation
        Compilation = MyBase.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger)
        Return Compilation
    End Function
End Class
