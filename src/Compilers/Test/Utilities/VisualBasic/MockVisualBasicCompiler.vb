' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities

Friend Class MockVisualBasicCompiler
    Inherits VisualBasicCompiler

    Private ReadOnly _analyzers As ImmutableArray(Of DiagnosticAnalyzer)
    Public Compilation As Compilation
    Public AnalyzerOptions As AnalyzerOptions

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
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), New DefaultAnalyzerAssemblyLoader())

        _analyzers = analyzers
    End Sub

    Private Shared Function CreateBuildPaths(workingDirectory As String, tempDirectory As String) As BuildPaths
        Return RuntimeUtilities.CreateBuildPaths(workingDirectory, tempDirectory:=tempDirectory)
    End Function

    Protected Overrides Sub ResolveAnalyzersFromArguments(
        diagnostics As List(Of DiagnosticInfo),
        messageProvider As CommonMessageProvider,
        skipAnalyzers As Boolean,
        ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
        ByRef generators As ImmutableArray(Of ISourceGenerator))

        MyBase.ResolveAnalyzersFromArguments(diagnostics, messageProvider, skipAnalyzers, analyzers, generators)
        If Not _analyzers.IsDefaultOrEmpty Then
            analyzers = analyzers.InsertRange(0, _analyzers)
        End If
    End Sub

    Public Overloads Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger, syntaxTreeDiagnosticOptionsOpt As ImmutableArray(Of AnalyzerConfigOptionsResult)) As Compilation
        Return Me.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxTreeDiagnosticOptionsOpt, Nothing)
    End Function

    Public Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger, syntaxTreeDiagnosticOptionsOpt As ImmutableArray(Of AnalyzerConfigOptionsResult), globalConfigOptions As AnalyzerConfigOptionsResult) As Compilation
        Compilation = MyBase.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxTreeDiagnosticOptionsOpt, globalConfigOptions)
        Return Compilation
    End Function

    Protected Overrides Function CreateAnalyzerOptions(
        additionalTextFiles As ImmutableArray(Of AdditionalText),
        analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider) As AnalyzerOptions
        AnalyzerOptions = MyBase.CreateAnalyzerOptions(additionalTextFiles, analyzerConfigOptionsProvider)
        Return AnalyzerOptions
    End Function
End Class
