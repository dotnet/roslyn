﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        MyClass.New(responseFile, CreateBuildPaths(baseDirectory, Path.GetTempPath()), args, analyzer)
    End Sub

    Public Sub New(responseFile As String, buildPaths As BuildPaths, args As String(), Optional analyzer As DiagnosticAnalyzer = Nothing)
        MyClass.New(responseFile, buildPaths, args, If(analyzer Is Nothing, ImmutableArray(Of DiagnosticAnalyzer).Empty, ImmutableArray.Create(analyzer)))
    End Sub

    Public Sub New(responseFile As String, workingDirectory As String, args As String(), analyzers As ImmutableArray(Of DiagnosticAnalyzer))
        MyClass.New(responseFile, CreateBuildPaths(workingDirectory, Path.GetTempPath()), args, analyzers)
    End Sub

    Public Sub New(responseFile As String, buildPaths As BuildPaths, args As String(), analyzers As ImmutableArray(Of DiagnosticAnalyzer))
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), New DesktopAnalyzerAssemblyLoader())

        _analyzers = analyzers
    End Sub

    Private Shared Function CreateBuildPaths(workingDirectory As String, tempDirectory As String) As BuildPaths
        Return New BuildPaths(
            clientDir:=Path.GetDirectoryName(GetType(VisualBasicCompiler).Assembly.Location),
            workingDir:=workingDirectory,
            sdkDir:=RuntimeEnvironment.GetRuntimeDirectory(),
            tempDir:=tempDirectory)
    End Function

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
