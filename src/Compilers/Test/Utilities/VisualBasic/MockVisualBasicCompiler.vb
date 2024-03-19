' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities

Friend Class MockVisualBasicCompiler
    Inherits VisualBasicCompiler

    Private ReadOnly _analyzers As ImmutableArray(Of DiagnosticAnalyzer)
    Private ReadOnly _generators As ImmutableArray(Of ISourceGenerator)
    Private ReadOnly _additionalReferences As ImmutableArray(Of MetadataReference)
    Public Compilation As Compilation
    Public AnalyzerOptions As AnalyzerOptions
    Public DescriptorsWithInfo As ImmutableArray(Of (Descriptor As DiagnosticDescriptor, Info As DiagnosticDescriptorErrorLoggerInfo))
    Public TotalAnalyzerExecutionTime As Double

    Public Sub New(baseDirectory As String, args As String(), Optional analyzer As DiagnosticAnalyzer = Nothing)
        MyClass.New(Nothing, baseDirectory, args, analyzer)
    End Sub

    Public Sub New(responseFile As String, baseDirectory As String, args As String(), analyzer As DiagnosticAnalyzer)
        MyClass.New(responseFile, CreateBuildPaths(baseDirectory, Path.GetTempPath()), args, analyzer)
    End Sub

    Public Sub New(responseFile As String, buildPaths As BuildPaths, args As String(), analyzer As DiagnosticAnalyzer)
        MyClass.New(responseFile, buildPaths, args, If(analyzer Is Nothing, Nothing, {analyzer}))
    End Sub

    Public Sub New(responseFile As String, workingDirectory As String, args As String(), Optional analyzers As DiagnosticAnalyzer() = Nothing, Optional generators As ISourceGenerator() = Nothing, Optional additionalReferences As MetadataReference() = Nothing)
        MyClass.New(responseFile, CreateBuildPaths(workingDirectory, Path.GetTempPath()), args, analyzers, generators, additionalReferences)
    End Sub

    Public Sub New(responseFile As String, buildPaths As BuildPaths, args As String(), Optional analyzers As DiagnosticAnalyzer() = Nothing, Optional generators As ISourceGenerator() = Nothing, Optional additionalReferences As MetadataReference() = Nothing)
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), New DefaultAnalyzerAssemblyLoader())

        _analyzers = analyzers.AsImmutableOrEmpty()
        _generators = generators.AsImmutableOrEmpty()
        _additionalReferences = additionalReferences.AsImmutableOrEmpty()
    End Sub

    Private Shared Function CreateBuildPaths(workingDirectory As String, tempDirectory As String) As BuildPaths
        Return RuntimeUtilities.CreateBuildPaths(workingDirectory, tempDirectory:=tempDirectory)
    End Function

    Protected Overrides Sub ResolveAnalyzersFromArguments(
        diagnostics As List(Of DiagnosticInfo),
        messageProvider As CommonMessageProvider,
        compilationOptions As CompilationOptions,
        skipAnalyzers As Boolean,
        ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
        ByRef generators As ImmutableArray(Of ISourceGenerator))

        MyBase.ResolveAnalyzersFromArguments(diagnostics, messageProvider, compilationOptions, skipAnalyzers, analyzers, generators)
        If Not _analyzers.IsDefaultOrEmpty Then
            analyzers = analyzers.InsertRange(0, _analyzers)
        End If

        If Not _generators.IsDefaultOrEmpty Then
            generators = generators.InsertRange(0, _generators)
        End If
    End Sub

    Public Overloads Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger, syntaxTreeDiagnosticOptionsOpt As ImmutableArray(Of AnalyzerConfigOptionsResult)) As Compilation
        Return Me.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxTreeDiagnosticOptionsOpt, Nothing)
    End Function

    Public Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger, syntaxTreeDiagnosticOptionsOpt As ImmutableArray(Of AnalyzerConfigOptionsResult), globalConfigOptions As AnalyzerConfigOptionsResult) As Compilation
        Compilation = MyBase.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, syntaxTreeDiagnosticOptionsOpt, globalConfigOptions)

        If Not _additionalReferences.IsEmpty Then
            Compilation = Compilation.AddReferences(_additionalReferences)
        End If

        Return Compilation
    End Function

    Protected Overrides Function CreateAnalyzerOptions(
        additionalTextFiles As ImmutableArray(Of AdditionalText),
        analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider) As AnalyzerOptions
        AnalyzerOptions = MyBase.CreateAnalyzerOptions(additionalTextFiles, analyzerConfigOptionsProvider)
        Return AnalyzerOptions
    End Function

    Protected Overrides Sub AddAnalyzerDescriptorsAndExecutionTime(errorLogger As ErrorLogger, descriptorsWithInfo As ImmutableArray(Of (Descriptor As DiagnosticDescriptor, Info As DiagnosticDescriptorErrorLoggerInfo)), totalAnalyzerExecutionTime As Double)
        Me.DescriptorsWithInfo = descriptorsWithInfo
        Me.TotalAnalyzerExecutionTime = totalAnalyzerExecutionTime

        MyBase.AddAnalyzerDescriptorsAndExecutionTime(errorLogger, descriptorsWithInfo, totalAnalyzerExecutionTime)
    End Sub

    Public Function GetAnalyzerExecutionTimeFormattedString() As String
        Return ReportAnalyzerUtil.GetFormattedAnalyzerExecutionTime(TotalAnalyzerExecutionTime, Culture).Trim()
    End Function
End Class
