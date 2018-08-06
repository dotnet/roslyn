' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Public Class TestOptions
    Public Shared ReadOnly Script As New VisualBasicParseOptions(kind:=SourceCodeKind.Script)
    Public Shared ReadOnly Regular As New VisualBasicParseOptions(kind:=SourceCodeKind.Regular)
    Public Shared ReadOnly RegularWithFlowAnalysisFeature As VisualBasicParseOptions = Regular.WithFlowAnalysisFeature()

    Public Shared ReadOnly ReleaseDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release)

    Public Shared ReadOnly ReleaseDebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release).
        WithDebugPlusMode(True)

    Public Shared ReadOnly ReleaseDebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Release).
        WithDebugPlusMode(True)

    Public Shared ReadOnly DebugDll As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Debug)
    Public Shared ReadOnly DebugExe As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=OptimizationLevel.Debug)

    Public Shared ReadOnly ReleaseModule As New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly ReleaseWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Release)
    Public Shared ReadOnly DebugWinMD As New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeMetadata, optimizationLevel:=OptimizationLevel.Debug)
End Class

Friend Module TestOptionExtensions
    <Extension()>
    Public Function WithStrictFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat(New KeyValuePair(Of String, String)() {New KeyValuePair(Of String, String)("Strict", "true")}))
    End Function

    <Extension()>
    Public Function WithFlowAnalysisFeature(options As VisualBasicParseOptions) As VisualBasicParseOptions
        Return options.WithFeatures(options.Features.Concat(New KeyValuePair(Of String, String)() {New KeyValuePair(Of String, String)("flow-analysis", "true")}))
    End Function
End Module
