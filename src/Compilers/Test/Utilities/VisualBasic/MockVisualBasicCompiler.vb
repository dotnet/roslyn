' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.Shell.Interop

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
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, Path.GetDirectoryName(GetType(VisualBasicCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Environment.GetEnvironmentVariable("LIB"), New SimpleAnalyzerAssemblyLoader())

        _analyzers = analyzers
    End Sub

    Protected Overrides Function GetSqmAppID() As UInteger
        Return SqmServiceProvider.BASIC_APPID
    End Function

    Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInteger)
        Throw New NotImplementedException
    End Sub

    Protected Overrides Sub ResolveAnalyzersAndGeneratorsFromArguments(
        diagnostics As List(Of DiagnosticInfo),
        messageProvider As CommonMessageProvider,
        ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
        ByRef generators As ImmutableArray(Of SourceGenerator))

        MyBase.ResolveAnalyzersAndGeneratorsFromArguments(diagnostics, messageProvider, analyzers, generators)
        If Not _analyzers.IsDefaultOrEmpty Then
            analyzers = analyzers.InsertRange(0, _analyzers)
        End If
    End Sub

    Public Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger) As Compilation
        Compilation = MyBase.CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger)
        Return Compilation
    End Function
End Class
